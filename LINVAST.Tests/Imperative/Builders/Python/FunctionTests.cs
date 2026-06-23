using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class FunctionTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void ParametersBuildFuncParamsNode()
        {
            var parameters = this.Parse<FuncParamsNode>("(x: int, y: str)", parser => parser.parameters());

            Assert.That(parameters.Parameters.Count(), Is.EqualTo(2));
            Assert.That(parameters.Parameters.ElementAt(0).Declarator.Identifier, Is.EqualTo("x"));
            Assert.That(parameters.Parameters.ElementAt(0).Specifiers.TypeName, Is.EqualTo("int"));
            Assert.That(parameters.Parameters.ElementAt(1).Declarator.Identifier, Is.EqualTo("y"));
            Assert.That(parameters.Parameters.ElementAt(1).Specifiers.TypeName, Is.EqualTo("str"));
            Assert.That(parameters.IsVariadic, Is.False);
        }

        [Test]
        public void ParametersPreserveArgsAndKwargs()
        {
            var parameters = this.Parse<FuncParamsNode>("(x, *args, y, **kwargs)", parser => parser.parameters());

            Assert.That(parameters.Parameters.Count(), Is.EqualTo(4));
            Assert.That(parameters.IsVariadic, Is.True);
            Assert.That(parameters.Parameters.ElementAt(1).Tags.Single().Identifier, Is.EqualTo("args"));
            Assert.That(parameters.Parameters.ElementAt(1).Declarator.Identifier, Is.EqualTo("args"));
            Assert.That(parameters.Parameters.ElementAt(2).Tags, Is.Empty);
            Assert.That(parameters.Parameters.ElementAt(3).Tags.Single().Identifier, Is.EqualTo("kwargs"));
            Assert.That(parameters.Parameters.ElementAt(3).Declarator.Identifier, Is.EqualTo("kwargs"));
        }

        [Test]
        public void BareStarDoesNotMarkKeywordOnlyParameterAsArgs()
        {
            var parameters = this.Parse<FuncParamsNode>("(x, *, y)", parser => parser.parameters());

            Assert.That(parameters.Parameters.Count(), Is.EqualTo(2));
            Assert.That(parameters.Parameters.ElementAt(1).Declarator.Identifier, Is.EqualTo("y"));
            Assert.That(parameters.Parameters.ElementAt(1).Tags, Is.Empty);
            Assert.That(parameters.IsVariadic, Is.False);
        }

        [Test]
        public void DecoratorBuildsTagNode()
        {
            var decorator = this.Parse<TagNode>("@route.get(\"/items\")\n", parser => parser.decorator());

            Assert.That(decorator.Identifier, Is.EqualTo("route.get(\"/items\")"));
        }

        [Test]
        public void AsyncDefBuildsFuncNodeWithAsyncTag()
        {
            var function = this.ParseStatement("async def f():\n    pass\n").As<FuncNode>();

            Assert.That(function.Tags.Single().Identifier, Is.EqualTo("async"));
            Assert.That(function.Identifier, Is.EqualTo("f"));
        }

        private StatNode ParseStatement(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<StatNode>();

        private TNode Parse<TNode>(string source, System.Func<Python3Parser, Antlr4.Runtime.ParserRuleContext> entry)
            where TNode : ASTNode
            => this.builder.BuildFromSource(source, entry).As<TNode>();
    }
}
