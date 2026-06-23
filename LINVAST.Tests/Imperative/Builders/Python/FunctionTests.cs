using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class FunctionTests : FunctionTestsBase
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void FunctionDefinitionBuildsFuncNode()
        {
            var source = this.Parse<SourceNode>("def add(x: int, y: int) -> int:\n    return x + y\n");
            var function = source.Children.Single().As<FuncNode>();

            Assert.That(function.Identifier, Is.EqualTo("add"));
            Assert.That(function.ReturnTypeName, Is.EqualTo("int"));
            Assert.That(function.Parameters!.Count(), Is.EqualTo(2));
            Assert.That(function.Parameters!.ElementAt(0).Declarator.Identifier, Is.EqualTo("x"));
            Assert.That(function.Parameters!.ElementAt(1).Declarator.Identifier, Is.EqualTo("y"));

            var returnStatement = function.Definition!.Children.Single().As<JumpStatNode>();
            Assert.That(returnStatement.ReturnExpr, Is.TypeOf<ArithmExprNode>());
        }

        [Test]
        public void FunctionDefinitionPreservesDefaultParameterValue()
        {
            var source = this.Parse<SourceNode>("def identity(x: int = 1) -> int:\n    return x\n");
            var function = source.Children.Single().As<FuncNode>();

            var parameterDecl = function.Parameters!.Single().Declarator.As<VarDeclNode>();
            Assert.That(parameterDecl.Identifier, Is.EqualTo("x"));
            Assert.That(parameterDecl.Initializer, Is.TypeOf<LitExprNode>());
            Assert.That(parameterDecl.Initializer!.As<LitExprNode>().Value, Is.EqualTo(1L));
        }

        [Test]
        public void DecoratedFunctionPreservesTags()
        {
            var source = this.Parse<SourceNode>("@staticmethod\ndef make() -> int:\n    return 1\n");
            var function = source.Children.Single().As<FuncNode>();

            Assert.That(function.Identifier, Is.EqualTo("make"));
            Assert.That(function.Tags.Single().Identifier, Is.EqualTo("staticmethod"));
        }

        [Test]
        public void AsyncFunctionPreservesAsyncTag()
        {
            var source = this.Parse<SourceNode>("async def load() -> int:\n    return 1\n");
            var function = source.Children.Single().As<FuncNode>();

            Assert.That(function.Identifier, Is.EqualTo("load"));
            Assert.That(function.Tags.Single().Identifier, Is.EqualTo("async"));
        }

        [Test]
        public void LambdaExpressionBuildsLambdaNode()
        {
            var lambda = this.Parse<LambdaFuncExprNode>("lambda x: x + 1", parser => parser.test());

            Assert.That(lambda.Parameters!.Single().Declarator.Identifier, Is.EqualTo("x"));
            Assert.That(lambda.Definition.Children.Single().As<JumpStatNode>().ReturnExpr, Is.TypeOf<ArithmExprNode>());
        }

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
        public void FunctionReturnExpressionTest()
        {
            this.AssertReturnValue("def g() -> int: return 3\n", 3L);
            this.AssertReturnValue("def g() -> float: return 3.3\n", 3.3);
            this.AssertReturnValue("def g() -> int: return 3 + 1\n", 4L);
            this.AssertReturnValue("def g() -> int: return 2 * 3\n", 6L);
            this.AssertReturnValue("def g() -> bool: return 1.1 > 1.0\n", true);
            this.AssertReturnValue("def g() -> bool: return 1.0 > 1.02\n", false);
        }

        private TNode Parse<TNode>(string source, System.Func<Python3Parser, Antlr4.Runtime.ParserRuleContext> entry)
            where TNode : ASTNode
            => this.builder.BuildFromSource(source, entry).As<TNode>();

        private TNode Parse<TNode>(string source)
            where TNode : ASTNode
            => this.builder.BuildFromSource(source).As<TNode>();

        protected override ASTNode GenerateAST(string src)
            => this.Parse<SourceNode>(src).Children.Single().As<FuncNode>();
    }
}
