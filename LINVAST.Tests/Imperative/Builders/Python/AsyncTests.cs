using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class AsyncTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void AsyncDefBuildsFuncNodeWithAsyncTag()
        {
            var function = this.ParseStatement("async def f():\n    pass\n").As<FuncNode>();

            Assert.That(function.Tags.Single().Identifier, Is.EqualTo("async"));
            Assert.That(function.Identifier, Is.EqualTo("f"));
        }

        [Test]
        public void AsyncForBuildsAsyncStatNode()
        {
            var asyncFor = this.ParseStatement("async for x in items:\n    pass\n").As<AsyncStatNode>();

            Assert.That(asyncFor.Tags.Single().Identifier, Is.EqualTo("async"));
            Assert.That(asyncFor.Statement, Is.TypeOf<ForStatNode>());
        }

        [Test]
        public void AsyncWithBuildsAsyncStatNode()
        {
            var asyncWith = this.ParseStatement("async with lock:\n    pass\n").As<AsyncStatNode>();

            Assert.That(asyncWith.Tags.Single().Identifier, Is.EqualTo("async"));
            Assert.That(asyncWith.Statement, Is.TypeOf<WithStatNode>());
        }

        private StatNode ParseStatement(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<StatNode>();
    }
}
