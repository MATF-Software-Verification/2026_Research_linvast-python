using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class BlockTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void SimpleFunctionBlockBuildsBlockNode()
        {
            var function = this.ParseFunction("def f():\n    pass\n");

            Assert.That(function.Definition, Is.Not.Null);
            Assert.That(function.Definition!.Children.Single(), Is.TypeOf<EmptyStatNode>());
        }

        private FuncNode ParseFunction(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<FuncNode>();
    }
}
