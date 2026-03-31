using System.Linq;
using LINVAST.Imperative.Builders.Java;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Java
{
    internal sealed class ClassTypeTests : ASTBuilderTestBase
    {
        [Test]

        public void ClassTypeIdentifierTest()
        {

            TypeDeclNode ast = this.GenerateAST("Point").As<TypeDeclNode>();

            Assert.That(ast.Identifier, Is.EqualTo("Point"));
            Assert.That(ast.BaseTypes.Children.Count, Is.EqualTo(0));
            Assert.That(ast.TemplateParameters.Children.Count, Is.EqualTo(0));
        }

        [Test]
        public void ClassTypeTypeArgumentsTest()
        {

            TypeDeclNode ast = this.GenerateAST("List<Point>").As<TypeDeclNode>();

            Assert.That(ast.Identifier, Is.EqualTo("List"));
            Assert.That(ast.BaseTypes.Children.Count, Is.EqualTo(0));
            Assert.That(ast.TemplateParameters.Children.Count, Is.EqualTo(1));
            Assert.That(ast.TemplateParameters.Types.First().Identifier, Is.EqualTo("Point"));

        }

        [Test]
        public void ClassTypeTest()
        {

            TypeDeclNode ast = this.GenerateAST("BaseClass<T>.Class<Template>").As<TypeDeclNode>();

            Assert.That(ast.Identifier, Is.EqualTo("Class"));
            Assert.That(ast.BaseTypes.Children.Count, Is.EqualTo(1));
            Assert.That(ast.TemplateParameters.Children.Count, Is.EqualTo(1));
            Assert.That(ast.BaseTypes.Types.Count, Is.EqualTo(1));
            Assert.That(ast.BaseTypes.Types.First().Identifier, Is.EqualTo("BaseClass"));
            Assert.That(ast.TemplateParameters.Types.First().Identifier, Is.EqualTo("Template"));

        }
        [Test]
        public void ClassTypeBaseAndArgumentTypesTest()
        {

            TypeDeclNode ast = this.GenerateAST("BaseClass.Class<TemplateClass>").As<TypeDeclNode>();

            Assert.That(ast.Identifier, Is.EqualTo("Class"));
            Assert.That(ast.BaseTypes.Children.Count, Is.EqualTo(1));
            Assert.That(ast.TemplateParameters.Children.Count, Is.EqualTo(1));
            Assert.That(ast.BaseTypes.Types.First().Identifier, Is.EqualTo("BaseClass"));
            Assert.That(ast.TemplateParameters.Types.First().Identifier, Is.EqualTo("TemplateClass"));

        }

        protected override ASTNode GenerateAST(string src)
        {
            return new JavaASTBuilder().BuildFromSource(src, p => p.classType());

        }
    }
}