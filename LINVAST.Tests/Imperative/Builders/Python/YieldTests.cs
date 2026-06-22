using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class YieldTests
    {
        private readonly PythonASTBuilder builder = new();

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

        private StatNode ParseStatement(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<StatNode>();
    }
}
