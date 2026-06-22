using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class WithTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void WithStatementBuildsWithStatNode()
        {
            var withStatement = this.ParseStatement("with open(path):\n    pass\n").As<WithStatNode>();

            Assert.That(withStatement.ContextManager, Is.TypeOf<FuncCallExprNode>());
            Assert.That(withStatement.Target, Is.Null);
            Assert.That(withStatement.Body.As<BlockStatNode>().Children.Single(), Is.TypeOf<EmptyStatNode>());
        }

        [Test]
        public void WithAsTargetBuildsWithStatNode()
        {
            var withStatement = this.ParseStatement("with open(path) as f:\n    pass\n").As<WithStatNode>();

            Assert.That(withStatement.ContextManager, Is.TypeOf<FuncCallExprNode>());
            Assert.That(withStatement.Target, Is.TypeOf<IdNode>());
            Assert.That(withStatement.Target!.As<IdNode>().Identifier, Is.EqualTo("f"));
        }

        [Test]
        public void MultipleWithItemsBuildNestedWithStatNodes()
        {
            var withStatement = this.ParseStatement("with a() as x, b() as y:\n    pass\n").As<WithStatNode>();

            Assert.That(withStatement.ContextManager.As<FuncCallExprNode>().Identifier, Is.EqualTo("a"));
            Assert.That(withStatement.Target!.As<IdNode>().Identifier, Is.EqualTo("x"));

            var innerWith = withStatement.Body.As<WithStatNode>();
            Assert.That(innerWith.ContextManager.As<FuncCallExprNode>().Identifier, Is.EqualTo("b"));
            Assert.That(innerWith.Target!.As<IdNode>().Identifier, Is.EqualTo("y"));
            Assert.That(innerWith.Body.As<BlockStatNode>().Children.Single(), Is.TypeOf<EmptyStatNode>());
        }

        private StatNode ParseStatement(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<StatNode>();
    }
}
