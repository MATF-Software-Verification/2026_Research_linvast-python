using System.Linq;
using LINVAST.Imperative.Builders.Java;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Java
{
    internal sealed class TypeTypesTests : ASTBuilderTestBase
    {
        [Test]

        public void PrimitiveTypeTest()
        {

            TypeNameNode ast = this.GenerateAST("int").As<TypeNameNode>();


            Assert.That(ast.Identifier, Is.EqualTo("int"));
            Assert.That(ast.TemplateArguments.Count, Is.EqualTo(0));
        }

        [Test]
        public void VoidTypeTest()
        {
            TypeNameNode ast = this.GenerateAST("void").As<TypeNameNode>();

            Assert.That(ast.Identifier, Is.EqualTo("void"));
        }

        [Test]

        public void ClassOrInterfaceTypeTest()
        {

            TypeNameNode ast = this.GenerateAST("Point").As<TypeNameNode>();

            Assert.That(ast.Identifier, Is.EqualTo("Point"));
            Assert.That(ast.TemplateArguments.Count, Is.EqualTo(0));
        }

        [Test]
        public void ClassOrInterfaceTypeWithTypeArgumentTest()
        {

            TypeNameNode ast = this.GenerateAST("ArrayList<double>").As<TypeNameNode>();

            Assert.That(ast.Identifier, Is.EqualTo("ArrayList"));
            Assert.That(ast.TemplateArguments.Count, Is.EqualTo(1));
            Assert.That(ast.TemplateArguments.First().Identifier, Is.EqualTo("double"));
        }

        [Test]
        public void ClassOrInterfaceTypeWithTypeArgumentsTest()
        {

            TypeNameNode ast = this.GenerateAST("Map<String, Point>").As<TypeNameNode>();

            Assert.That(ast.Identifier, Is.EqualTo("Map"));
            Assert.That(ast.TemplateArguments.Count, Is.EqualTo(2));
            Assert.That(ast.TemplateArguments.First().Identifier, Is.EqualTo("String"));
            Assert.That(ast.TemplateArguments.Last().Identifier, Is.EqualTo("Point"));
        }


        [Test]
        public void ClassOrInterfaceTypeWithClassOrInterfaceTest()
        {

            TypeNameNode ast = this.GenerateAST("Map<String, Point<int, int>>").As<TypeNameNode>();

            Assert.That(ast.Identifier, Is.EqualTo("Map"));
            Assert.That(ast.TemplateArguments.Count, Is.EqualTo(2));
            Assert.That(ast.TemplateArguments.First().Identifier, Is.EqualTo("String"));
            Assert.That(ast.TemplateArguments.Last().Identifier, Is.EqualTo("Point"));
            Assert.That(ast.TemplateArguments.Last().TemplateArguments.Count, Is.EqualTo(2));
            Assert.That(ast.TemplateArguments.Last().TemplateArguments.First().Identifier, Is.EqualTo("int"));
            Assert.That(ast.TemplateArguments.Last().TemplateArguments.Last().Identifier, Is.EqualTo("int"));
        }
        protected override ASTNode GenerateAST(string src)
        {
            return new JavaASTBuilder().BuildFromSource(src, p => p.typeTypeOrVoid());
        }
    }
}