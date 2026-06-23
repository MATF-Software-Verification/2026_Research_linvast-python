using System;
using System.Collections.Generic;
using System.Linq;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    public sealed partial class PythonASTBuilder
    {
        /// <summary>
        /// Attempts to promote a tuple unpacking assignment to declarations.
        /// Example: a, b = 1, 2 -> creates VarDeclNode for both a and b.
        /// Returns true only when every promoted pair is a new declaration.
        /// </summary>
        private bool TryPromoteTupleUnpacking(ExprNode lhs, ExprNode rhs, int line, List<ASTNode> nodes, HashSet<string> declared)
        {
            // Check if LHS is a list of identifiers (tuple unpacking target)
            var identifiers = this.ExtractIdentifiers(lhs);
            if (identifiers == null || identifiers.Count == 0)
                return false;

            // Check if RHS is an expression list or single expression
            var rhsExpressions = new List<ExprNode>();
            if (rhs is ExprListNode exprList)
                rhsExpressions.AddRange(exprList.Expressions);
            else if (rhs is ArrInitExprNode arrInit)
                rhsExpressions.AddRange(arrInit.Expressions);
            else
                // Single RHS value - cannot safely unpack without runtime information
                return false;

            // Promote only on exact arity match; otherwise keep original assignment.
            if (identifiers.Count == 0 || identifiers.Count != rhsExpressions.Count)
                return false;
            int count = identifiers.Count;

            for (int i = 0; i < count; i++) {
                if (declared.Contains(identifiers[i]))
                    return false;
            }

            var declSpecs = new DeclSpecsNode(line);
            var varDecls = new List<VarDeclNode>();
            for (int i = 0; i < count; i++) {
                string identifier = identifiers[i];
                ExprNode value = rhsExpressions[i];
                var idNode = new IdNode(line, identifier);
                varDecls.Add(new VarDeclNode(line, idNode, value));
                declared.Add(identifier);
            }

            nodes.Add(new DeclStatNode(line, declSpecs, new DeclListNode(line, varDecls)));
            return true;
        }

        /// <summary>
        /// Extracts identifier names from a tuple unpacking target (e.g., a, b, c)
        /// Returns null if the expression is not a valid tuple of identifiers.
        /// </summary>
        private List<string>? ExtractIdentifiers(ExprNode expr)
        {
            if (expr is IdNode id)
                return new List<string> { id.Identifier };

            if (expr is ExprListNode exprList) {
                if (exprList.Expressions.All(e => e is IdNode)) {
                    return exprList.Expressions.Cast<IdNode>().Select(i => i.Identifier).ToList();
                }
            }

            if (expr is IdListNode idList)
                return idList.Identifiers.Select(i => i.Identifier).ToList();

            if (expr is ArrInitExprNode arr) {
                // Check if all elements are IdNode
                if (arr.Expressions.All(e => e is IdNode)) {
                    return arr.Expressions.Cast<IdNode>().Select(i => i.Identifier).ToList();
                }
            }

            return null;
        }
    }
}
