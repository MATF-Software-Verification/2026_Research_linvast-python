using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Visitors;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class ExpressionTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void LiteralExpressionTests()
        {
            this.AssertExpressionValue("42", 42L);
            this.AssertExpressionValue("\"hello\"", "hello");

            Assert.That(this.ParseExpression("None"), Is.TypeOf<NullLitExprNode>());
        }

        [Test]
        public void LiteralExprBuildsExprNode()
        {
            var literal = this.builder.BuildFromSource("42", parser => parser.literal_expr()).As<LitExprNode>();

            Assert.That(literal.Value, Is.EqualTo(42L));
        }

        [Test]
        public void BareYieldBuildsYieldExprNode()
        {
            var statement = this.ParseStatement("yield\n");
            var yieldExpr = statement.As<ExprStatNode>().Expression.As<YieldExprNode>();

            Assert.That(yieldExpr.Value, Is.Null);
            Assert.That(yieldExpr.IsDelegating, Is.False);
        }

        [Test]
        public void YieldValueBuildsYieldExprNode()
        {
            var statement = this.ParseStatement("yield 1\n");
            var yieldExpr = statement.As<ExprStatNode>().Expression.As<YieldExprNode>();

            Assert.That(yieldExpr.Value, Is.TypeOf<LitExprNode>());
            Assert.That(yieldExpr.IsDelegating, Is.False);
        }

        [Test]
        public void YieldFromBuildsDelegatingYieldExprNode()
        {
            var statement = this.ParseStatement("yield from items\n");
            var yieldExpr = statement.As<ExprStatNode>().Expression.As<YieldExprNode>();

            Assert.That(yieldExpr.Value, Is.TypeOf<IdNode>());
            Assert.That(yieldExpr.IsDelegating, Is.True);
        }

        [Test]
        public void ChainedComparisonSharesMiddleOperandNode()
        {
            var expr = this.ParseExpression("a < f() < b").As<LogicExprNode>();
            var left = expr.LeftOperand.As<RelExprNode>();
            var right = expr.RightOperand.As<RelExprNode>();

            // `a < f() < b` desugars to `a < f() and f() < b`, but the shared
            // f() must be the exact same node so it is only evaluated once.
            Assert.That(left.RightOperand, Is.TypeOf<FuncCallExprNode>());
            Assert.That(left.RightOperand, Is.SameAs(right.LeftOperand));
        }

        [Test]
        public void ListComprehensionBuildsListCall()
        {
            var call = this.ParseExpression("[x * 2 for x in items]").As<FuncCallExprNode>();
            ExprNode[] args = call.Arguments!.Expressions.ToArray();

            Assert.That(call.Identifier, Is.EqualTo("list"));
            Assert.That(args[0], Is.TypeOf<ArithmExprNode>());
            Assert.That(args[1].As<FuncCallExprNode>().Identifier, Is.EqualTo("for"));
        }

        [Test]
        public void GeneratorExpressionInParensBuildsGeneratorCall()
        {
            var call = this.ParseExpression("(x for x in items)").As<FuncCallExprNode>();

            Assert.That(call.Identifier, Is.EqualTo("generator"));
            Assert.That(call.Arguments!.Expressions.ElementAt(1).As<FuncCallExprNode>().Identifier, Is.EqualTo("for"));
        }

        [Test]
        public void SetComprehensionBuildsSetCall()
        {
            var call = this.ParseExpression("{x for x in items}").As<FuncCallExprNode>();

            Assert.That(call.Identifier, Is.EqualTo("set"));
        }

        [Test]
        public void DictComprehensionBuildsDictCall()
        {
            var call = this.ParseExpression("{k: v for k, v in pairs}").As<FuncCallExprNode>();

            Assert.That(call.Identifier, Is.EqualTo("dict"));
            Assert.That(call.Arguments!.Expressions.First(), Is.TypeOf<DictEntryNode>());
        }

        [Test]
        public void GeneratorArgumentWithFilterBuildsClauses()
        {
            var call = this.ParseExpression("sum(x for x in xs if x > 0)").As<FuncCallExprNode>();
            var generator = call.Arguments!.Expressions.Single().As<FuncCallExprNode>();
            ExprNode[] clauses = generator.Arguments!.Expressions.Skip(1).ToArray();

            Assert.That(call.Identifier, Is.EqualTo("sum"));
            Assert.That(generator.Identifier, Is.EqualTo("generator"));
            Assert.That(clauses[0].As<FuncCallExprNode>().Identifier, Is.EqualTo("for"));
            Assert.That(clauses[1].As<FuncCallExprNode>().Identifier, Is.EqualTo("if"));
        }

        [Test]
        public void FullSliceBuildsSliceCall()
        {
            var access = this.ParseExpression("a[1:10:2]").As<ArrAccessExprNode>();
            var slice = access.IndexExpression.As<FuncCallExprNode>();
            ExprNode[] parts = slice.Arguments!.Expressions.ToArray();

            Assert.That(slice.Identifier, Is.EqualTo("slice"));
            Assert.That(parts[0].As<LitExprNode>().Value, Is.EqualTo(1L));
            Assert.That(parts[1].As<LitExprNode>().Value, Is.EqualTo(10L));
            Assert.That(parts[2].As<LitExprNode>().Value, Is.EqualTo(2L));
        }

        [Test]
        public void SliceWithOmittedBoundsUsesNullLiterals()
        {
            var access = this.ParseExpression("a[:5]").As<ArrAccessExprNode>();
            var slice = access.IndexExpression.As<FuncCallExprNode>();
            ExprNode[] parts = slice.Arguments!.Expressions.ToArray();

            Assert.That(parts[0], Is.TypeOf<NullLitExprNode>());
            Assert.That(parts[1].As<LitExprNode>().Value, Is.EqualTo(5L));
            Assert.That(parts[2], Is.TypeOf<NullLitExprNode>());
        }

        [Test]
        public void AwaitExpressionBuildsAwaitCall()
        {
            var call = this.ParseExpression("await f()").As<FuncCallExprNode>();

            Assert.That(call.Identifier, Is.EqualTo("await"));
            Assert.That(call.Arguments!.Expressions.Single(), Is.TypeOf<FuncCallExprNode>());
        }

        private ExprNode ParseExpression(string source)
            => this.builder.BuildFromSource(source, parser => parser.test()).As<ExprNode>();

        private StatNode ParseStatement(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<StatNode>();

        private void AssertExpressionValue<T>(string source, T expected)
        {
            ExprNode expression = this.ParseExpression(source);
            Assert.That(ConstantExpressionEvaluator.TryEvaluateAs(expression, out T result));
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
