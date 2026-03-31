using System.Linq;
using LINVAST.Imperative.Builders.Java;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;


namespace LINVAST.Tests.Imperative.Builders.Java
{
    internal sealed class ClassDeclarationTests : DeclarationTestsBase
    {
        [Test]
        public void EmptyBodyClassDeclTest()
        {
            string src1 = "class Class1 {}";
            TypeDeclNode ast1 = this.GenerateAST(src1).As<TypeDeclNode>();

            Assert.That(ast1.Identifier, Is.EqualTo("Class1"));
            Assert.That(ast1.Declarations.Count, Is.EqualTo(0));
        }

        [Test]
        public void ExtendsTypeClassDeclTest()
        {
            string src1 = "class Class1 extends String {}";
            TypeDeclNode ast1 = this.GenerateAST(src1).As<TypeDeclNode>();

            Assert.That(ast1.Identifier, Is.EqualTo("Class1"));
            Assert.That(ast1.BaseTypes.Types.First().TypeName, Is.EqualTo("String"));
        }

        [Test]
        public void ExtendsQualifiedTypeClassDeclTest()
        {
            string src1 = "class Class1 extends java.sql.SqlConnection {}";
            TypeDeclNode ast1 = this.GenerateAST(src1).As<TypeDeclNode>();

            Assert.That(ast1.Identifier, Is.EqualTo("Class1"));
            Assert.That(ast1.BaseTypes.Types.First().TypeName, Is.EqualTo("java.sql.SqlConnection"));
        }

        [Test]
        public void WithTypeParamsClassDeclTest()
        {
            string src1 = "class Name <Class1> {}";
            TypeDeclNode ast1 = this.GenerateAST(src1).As<TypeDeclNode>();

            Assert.That(ast1.Identifier, Is.EqualTo("Name"));
            Assert.That(ast1.TemplateParameters.Types.First().TypeName, Is.EqualTo("Class1"));
        }

        [Test]
        public void WithTypeParamsExtendsTypeClassDeclTest()
        {
            string src1 = "class Name <Class1> extends String {}";
            TypeDeclNode ast1 = this.GenerateAST(src1).As<TypeDeclNode>();

            Assert.That(ast1.Identifier, Is.EqualTo("Name"));
            Assert.That(ast1.TemplateParameters.Types.First().TypeName, Is.EqualTo("Class1"));
            Assert.That(ast1.BaseTypes.Types.First().TypeName, Is.EqualTo("String"));
        }


        protected override ASTNode GenerateAST(string src)
            => new JavaASTBuilder().BuildFromSource(src, p => p.classDeclaration());
    }
}
