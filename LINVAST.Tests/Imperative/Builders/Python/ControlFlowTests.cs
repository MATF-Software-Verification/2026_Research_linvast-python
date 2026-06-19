using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class ControlFlowTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void IfElifElseBuildsNestedIfNodes()
        {
            var source = this.ParseSource("if True:\n    pass\nelif False:\n    pass\nelse:\n    pass\n");
            var ifStatement = source.Children.Single().As<IfStatNode>();

            Assert.That(ifStatement.Condition, Is.TypeOf<LitExprNode>());
            Assert.That(ifStatement.ThenStat.As<BlockStatNode>().Children.Single(), Is.TypeOf<EmptyStatNode>());
            Assert.That(ifStatement.ElseStat, Is.TypeOf<IfStatNode>());
        }

        private SourceNode ParseSource(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>();
    }
}
