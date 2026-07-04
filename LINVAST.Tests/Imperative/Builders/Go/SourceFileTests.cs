using System.Linq;
using LINVAST.Imperative.Builders.Go;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Go
{
    internal sealed class SourceFileTests : ASTBuilderTestBase
    {
        [Test]
        public void SourceFileIncludesPackageNodeTest()
        {
            SourceNode source = this.GenerateAST("package demo; var x int").As<SourceNode>();

            PackageNode package = source.Children.First().As<PackageNode>();
            Assert.That(package.Identifier, Is.EqualTo("demo"));
            Assert.That(source.Children.Last(), Is.InstanceOf<DeclStatNode>());
        }

        [Test]
        public void TypeDeclarationTest()
        {
            DeclStatNode typeDecl = this.GenerateAST("package demo; type Point struct { x int }")
                .As<SourceNode>()
                .Children
                .Last()
                .As<DeclStatNode>();

            Assert.That(typeDecl.Specifiers.TypeName, Is.EqualTo("struct{xint}"));
            Assert.That(typeDecl.DeclaratorList.Declarators.Single().Identifier, Is.EqualTo("Point"));
        }

        [Test]
        public void MethodDeclarationWithParametersKeepsBodyTest()
        {
            FuncNode method = this.GenerateAST("package demo; func (p Point) Move(dx int) { return }")
                .As<SourceNode>()
                .Children
                .Last()
                .As<FuncNode>();

            Assert.That(method.Identifier, Is.EqualTo("Point.Move"));
            Assert.That(method.Parameters, Has.Exactly(2).Items);
            Assert.That(method.Parameters!.Select(p => (p.Specifiers.TypeName, p.Declarator.Identifier)),
                Is.EqualTo(new[] { ("Point", "p"), ("int", "dx") }));
            Assert.That(method.Definition, Is.Not.Null);
            Assert.That(method.Definition!.Children.Single(), Is.InstanceOf<JumpStatNode>());
        }

        protected override ASTNode GenerateAST(string src)
            => new GoASTBuilder().BuildFromSource(src);
    }
}
