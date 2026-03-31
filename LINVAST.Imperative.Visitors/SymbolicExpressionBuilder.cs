using System.Collections.Generic;
using System.Text.RegularExpressions;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using Expr = MathNet.Symbolics.SymbolicExpression;

namespace LINVAST.Imperative.Visitors
{
    public sealed class SymbolicExpressionBuilder : BaseASTVisitor<Expr>
    {
        private static readonly Regex _wildcardRegex = new(@"v__\d+", RegexOptions.Compiled);
        private static int _lastUsedId = 0;
        private static readonly Dictionary<string, int> _wildcards = new();


        public static string WildcardReplace(string expr, string replacement = "?")
            => _wildcardRegex.Replace(expr, replacement);


        public ASTNode Node { get; set; }


        public SymbolicExpressionBuilder(ASTNode node)
        {
            this.Node = node;
        }


        public Expr Parse()
            => this.Visit(this.Node);


        public override Expr Visit(ArithmExprNode node)
            => this.EvaluateBinaryExpression(node);

        public override Expr Visit(RelExprNode node)
            => this.EvaluateBinaryExpression(node);

        public override Expr Visit(LogicExprNode node)
            => this.EvaluateBinaryExpression(node);

        public override Expr Visit(UnaryExprNode node)
            => this.EvaluateUnaryExpression(node);

        public override Expr Visit(IdNode node)
            => Expr.Variable(node.Identifier);

        public override Expr Visit(FuncCallExprNode node)
            => Expr.Variable($"r_{node.Identifier}");

        public override Expr Visit(LitExprNode node)
            // TODO string literals need to be substituted as well...
            => node.Value is null ? Expr.Undefined : Expr.Parse(node.Value.ToString());

        public override Expr Visit(NullLitExprNode node)
            => Expr.Undefined;


        private Expr EvaluateBinaryExpression(BinaryExprNode node)
        {
            Expr left = this.Visit(node.LeftOperand);
            Expr right = this.Visit(node.RightOperand);
            return this.TryEvaluate(node, $"{left} {node.Operator.Symbol} {right}");
        }

        private Expr EvaluateUnaryExpression(UnaryExprNode node)
        {
            Expr operand = this.Visit(node.Operand);
            return this.TryEvaluate(node, $"{node.Operator.Symbol}({operand})");
        }

        private Expr TryEvaluate(ExprNode e, string exprStr)
        {
            Expr expr;
            object? constantValue = null;
            try {
                constantValue = ConstantExpressionEvaluator.Evaluate(e);
            } catch {

            }

            if (constantValue is not null) {
                try {
                    expr = Expr.Parse(constantValue.ToString());
                    return expr;
                } catch {

                }
            }

            try {
                expr = Expr.Parse(exprStr);
            } catch {
                expr = this.GetWildcard(exprStr);
            }
            return expr;
        }

        private Expr GetWildcard(string exprStr)
        {
            if (_wildcards.TryGetValue(exprStr, out int id))
                return Expr.Variable($"v__{id}");
            _wildcards.Add(exprStr, _lastUsedId);
            return Expr.Variable($"v__{_lastUsedId++}");
        }
    }
}
