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

        [Test]
        public void TopLevelSemicolonSeparatedStatementsAreFlattened()
        {
            var children = this.builder.BuildFromSource("pass; pass\n").As<SourceNode>().Children.ToList();

            Assert.That(children.Count, Is.EqualTo(2));
            Assert.That(children[0], Is.TypeOf<EmptyStatNode>());
            Assert.That(children[1], Is.TypeOf<EmptyStatNode>());
        }

        [Test]
        public void SemicolonSeparatedAssignmentsAreFlattened()
        {
            var children = this.builder.BuildFromSource("x = 1; y = 2; z = 3\n").As<SourceNode>().Children.ToList();

            Assert.That(children.Count, Is.EqualTo(3));
            Assert.That(children[0], Is.TypeOf<DeclStatNode>());
            Assert.That(children[1], Is.TypeOf<DeclStatNode>());
            Assert.That(children[2], Is.TypeOf<DeclStatNode>());
        }

        private FuncNode ParseFunction(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<FuncNode>();
    }
}
