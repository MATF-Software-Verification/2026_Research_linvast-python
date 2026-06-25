using System;
using System.Collections.Generic;
using System.Linq;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    public sealed partial class PythonASTBuilder
    {
        private sealed class UnpackingTarget
        {
            public UnpackingTarget(string identifier, bool isStarred)
            {
                this.Identifier = identifier;
                this.IsStarred = isStarred;
            }

            public string Identifier { get; }

            public bool IsStarred { get; }
        }

        // Promotes unpacking assignment to declarations when safe.
        private bool TryPromoteTupleUnpacking(ExprNode lhs, ExprNode rhs, int line, List<ASTNode> nodes, HashSet<string> declared)
        {
            if (!this.TryCreateTupleUnpackingDeclaration(lhs, rhs, line, declared, typeName: null, out DeclStatNode? declaration))
                return false;

            nodes.Add(declaration!);
            return true;
        }

        private bool TryCreateTupleUnpackingDeclaration(
            ExprNode lhs,
            ExprNode rhs,
            int line,
            HashSet<string>? declared,
            string? typeName,
            out DeclStatNode? declaration)
        {
            declaration = null;

            if (!this.TryExtractTargets(lhs, out List<UnpackingTarget>? targets, out int starredIndex) || targets.Count == 0)
                return false;

            if (!this.TryExtractRhsExpressions(rhs, out List<ExprNode>? rhsExpressions))
                return false;

            foreach (UnpackingTarget target in targets) {
                if (declared is not null && declared.Contains(target.Identifier))
                    return false;
            }

            var varDecls = new List<VarDeclNode>();
            if (starredIndex < 0) {
                if (targets.Count != rhsExpressions.Count)
                    return false;

                for (int i = 0; i < targets.Count; i++) {
                    varDecls.Add(new VarDeclNode(line, new IdNode(line, targets[i].Identifier), rhsExpressions[i]));
                }
            } else {
                int prefixCount = starredIndex;
                int suffixCount = targets.Count - starredIndex - 1;

                if (rhsExpressions.Count < prefixCount + suffixCount)
                    return false;

                int starredLength = rhsExpressions.Count - prefixCount - suffixCount;
                var starredValues = rhsExpressions
                    .Skip(prefixCount)
                    .Take(starredLength)
                    .ToList();

                for (int i = 0; i < targets.Count; i++) {
                    ExprNode initializer;
                    if (i < prefixCount) {
                        initializer = rhsExpressions[i];
                    } else if (i == starredIndex) {
                        initializer = new ArrInitExprNode(line, starredValues);
                    } else {
                        int suffixIndex = i - starredIndex - 1;
                        int rhsIndex = rhsExpressions.Count - suffixCount + suffixIndex;
                        initializer = rhsExpressions[rhsIndex];
                    }

                    varDecls.Add(new VarDeclNode(line, new IdNode(line, targets[i].Identifier), initializer));
                }
            }

            var declSpecs = typeName is null ? new DeclSpecsNode(line) : new DeclSpecsNode(line, typeName);
            declaration = new DeclStatNode(line, declSpecs, new DeclListNode(line, varDecls));

            if (declared is not null) {
                foreach (UnpackingTarget target in targets)
                    declared.Add(target.Identifier);
            }

            return true;
        }

        // Typed wrapper for tuple-unpacking declaration creation.
        private bool TryCreateTypedTupleUnpackingDeclaration(
            ExprNode lhs,
            ExprNode rhs,
            int line,
            string typeName,
            out DeclStatNode? declaration) =>
            this.TryCreateTupleUnpackingDeclaration(lhs, rhs, line, declared: null, typeName, out declaration);

        // Extracts unpack targets and the starred target index.
        private bool TryExtractTargets(ExprNode expr, out List<UnpackingTarget> targets, out int starredIndex)
        {
            targets = new List<UnpackingTarget>();
            starredIndex = -1;

            if (expr is IdNode id) {
                targets.Add(new UnpackingTarget(id.Identifier, isStarred: false));
                return true;
            }

            if (expr is IdListNode idList) {
                targets.AddRange(idList.Identifiers.Select(i => new UnpackingTarget(i.Identifier, isStarred: false)));
                return true;
            }

            IEnumerable<ExprNode>? expressions = null;
            if (expr is ExprListNode exprList)
                expressions = exprList.Expressions;
            else if (expr is ArrInitExprNode arr)
                expressions = arr.Expressions;

            if (expressions is null)
                return false;

            foreach (ExprNode targetExpr in expressions) {
                if (targetExpr is IdNode targetId) {
                    targets.Add(new UnpackingTarget(targetId.Identifier, isStarred: false));
                    continue;
                }

                if (targetExpr is AssignExprNode starAssign
                    && starAssign.LeftOperand is IdNode { Identifier: "*" }
                    && starAssign.RightOperand is IdNode starTarget) {
                    if (starredIndex >= 0)
                        return false;
                    starredIndex = targets.Count;
                    targets.Add(new UnpackingTarget(starTarget.Identifier, isStarred: true));
                    continue;
                }

                return false;
            }

            return targets.Count > 0;
        }

        private bool TryExtractRhsExpressions(ExprNode rhs, out List<ExprNode> rhsExpressions)
        {
            rhsExpressions = new List<ExprNode>();

            if (rhs is ExprListNode exprList) {
                rhsExpressions.AddRange(exprList.Expressions);
                return true;
            }

            if (rhs is ArrInitExprNode arrInit) {
                rhsExpressions.AddRange(arrInit.Expressions);
                return true;
            }

            return false;
        }

        private bool HasMultipleStarredTargets(ExprNode expr)
        {
            IEnumerable<ExprNode>? expressions = null;
            if (expr is ArrInitExprNode arrInit)
                expressions = arrInit.Expressions;
            else if (expr is ExprListNode exprList)
                expressions = exprList.Expressions;

            if (expressions is null)
                return false;

            int starredCount = expressions.Count(e =>
                e is AssignExprNode assign
                && assign.LeftOperand is IdNode { Identifier: "*" }
                && assign.RightOperand is IdNode);

            return starredCount > 1;
        }
    }
}
