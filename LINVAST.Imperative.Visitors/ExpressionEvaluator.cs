using System.Collections.Generic;
using System.Linq;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using MathNet.Symbolics;
using Expr = MathNet.Symbolics.SymbolicExpression;

namespace LINVAST.Imperative.Visitors
{
    public sealed class ExpressionEvaluator
    {
        public static Expr TryEvaluate(ExprNode node, Dictionary<string, Expr> symbols)
            => TryEvaluate(new SymbolicExpressionBuilder(node).Parse(), symbols);

        public static Expr TryEvaluate(Expr expr, Dictionary<string, Expr> symbols)
        {
            IEnumerable<Expr> vars = expr.CollectVariables();
            bool canReduce = true;
            for (int i = 0; canReduce && vars.Any(); i++) {
                if (i > symbols.Count)
                    throw new EvaluationException("Infinite cycle detected.");
                canReduce = false;
                foreach (Expr v in vars) {
                    string varStr = v.VariableName;
                    if (symbols.ContainsKey(varStr)) {
                        expr = expr.Substitute(v, symbols[varStr]);
                        canReduce = true;
                    }
                }
                vars = expr.CollectVariables();
            }

            if (expr.Type == SymbolicExpressionType.Undefined)
                throw new EvaluationException("Undefined variable found in expression");

            return expr;
        }
    }
}
