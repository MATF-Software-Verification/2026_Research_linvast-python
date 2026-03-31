using System.Linq;
using LINVAST.Imperative.Builders.Java;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Java
{
    internal sealed class TypeParametersTests : ASTBuilderTestBase
    {

        [Test]

        public void TypeParameterTest()
        {
            TypeNameListNode ast = this.GenerateAST("< Point >").As<TypeNameListNode>();

            Assert.That(ast.Types.Count, Is.EqualTo(1));
            Assert.That(ast.Types.First().Identifier, Is.EqualTo("Point"));
            Assert.That(ast.Types.First().TemplateArguments.Count, Is.EqualTo(0));
        }

        [Test]

        public void TypeParametersTest()
        {
            TypeNameListNode ast = this.GenerateAST("< String, Square >").As<TypeNameListNode>();

            Assert.That(ast.Types.Count, Is.EqualTo(2));
            Assert.That(ast.Types.First().Identifier, Is.EqualTo("String"));
            Assert.That(ast.Types.Last().Identifier, Is.EqualTo("Square"));
        }

        [Test]
        public void TypeParameterWithExtendsTest()
        {

            TypeNameListNode ast = this.GenerateAST("< Square extends Rectangle>").As<TypeNameListNode>();

            Assert.That(ast.Types.Count, Is.EqualTo(1));
            Assert.That(ast.Types.First().Identifier, Is.EqualTo("Square"));
            Assert.That(ast.Types.First().TemplateArguments.Count, Is.EqualTo(1));
            Assert.That(ast.Types.First().TemplateArguments.First().Identifier, Is.EqualTo("Rectangle"));
        }
        [Test]
        public void TypeParametersWithExtendsTest()
        {

            TypeNameListNode ast = this.GenerateAST("< Square extends Rectangle, bool >").As<TypeNameListNode>();

            Assert.That(ast.Types.Count, Is.EqualTo(2));
            Assert.That(ast.Types.First().Identifier, Is.EqualTo("Square"));
            Assert.That(ast.Types.First().TemplateArguments.Count, Is.EqualTo(1));
            Assert.That(ast.Types.First().TemplateArguments.First().Identifier, Is.EqualTo("Rectangle"));
            Assert.That(ast.Types.Last().Identifier, Is.EqualTo("bool"));
        }

        [Test]
        public void TypeParametersWithExtendsMultipleTest()
        {

            TypeNameListNode ast = this.GenerateAST("< TypeA extends TypeC & TypeD, TypeB>").As<TypeNameListNode>();

            Assert.That(ast.Types.First().Identifier, Is.EqualTo("TypeA"));
            Assert.That(ast.Types.Count, Is.EqualTo(2));
            Assert.That(ast.Types.First().TemplateArguments.Count, Is.EqualTo(2));
            Assert.That(ast.Types.First().TemplateArguments.First().Identifier, Is.EqualTo("TypeC"));
            Assert.That(ast.Types.First().TemplateArguments.Last().Identifier, Is.EqualTo("TypeD"));
            Assert.That(ast.Types.Last().Identifier, Is.EqualTo("TypeB"));
        }
        protected override ASTNode GenerateAST(string src)
        {
            return new JavaASTBuilder().BuildFromSource(src, p => p.typeParameters());

        }

    }
}