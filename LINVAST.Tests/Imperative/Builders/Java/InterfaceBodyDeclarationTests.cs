using System;
using System.Linq;
using LINVAST.Imperative.Builders.Java;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Java
{
    internal sealed class InterfaceBodyDeclarationTests : DeclarationTestsBase
    {
        [Test]
        public void EmptyInterfaceBodyDeclTest()
        {
            string src1 = ";";
            EmptyStatNode ast1 = this.GenerateAST(src1).As<EmptyStatNode>();
            Assert.That(ast1.GetText(), Is.EqualTo(";"));
        }

        [Test]
        public void WithAnnotationClassBodyDeclTest()
        {
            string src1 = "@Override public string toString() {return this.attr.toString();}";
            Assert.That(() => this.GenerateAST(src1), Throws.InstanceOf<NotImplementedException>());
        }

        [Test]
        public void ConstDeclarationTest()
        {
            string src1 = "String str = null;";
            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();

            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.First().Identifier, Is.EqualTo("str"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<VarDeclNode>().Initializer,
                Is.InstanceOf<NullLitExprNode>());
        }

        [Test]
        public void MultipleDeclaratorsConstDeclTest()
        {
            string src1 = "String str1 = null, str2 = null;";
            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();

            Assert.That(ast1.DeclaratorList.Children.Count, Is.EqualTo(2));
            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.First().Identifier, Is.EqualTo("str1"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<VarDeclNode>().Initializer,
                Is.InstanceOf<NullLitExprNode>());
            Assert.That(ast1.DeclaratorList.Declarators.Last().Identifier, Is.EqualTo("str2"));
            Assert.That(ast1.DeclaratorList.Declarators.Last().As<VarDeclNode>().Initializer,
                Is.InstanceOf<NullLitExprNode>());
        }

        [Test]
        public void InterfaceMethodTest()
        {
            string src1 = "String f(){}";
            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();

            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<FuncDeclNode>().Identifier,
                Is.EqualTo("f"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<FuncDeclNode>().Definition?.Children.Count,
                Is.EqualTo(0));
        }

        [Test]
        public void WithModifiersInterfaceMethodTest()
        {
            string src1 = "public static String f() {}";
            string src2 = "public String f() {}";

            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();
            DeclStatNode ast2 = this.GenerateAST(src2).As<DeclStatNode>();

            Assert.That(ast1.Specifiers.Modifiers.ToString(), Is.EqualTo("public static"));
            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<FuncDeclNode>().Identifier,
                Is.EqualTo("f"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<FuncDeclNode>().Definition?.Children.Count,
                Is.EqualTo(0));
            Assert.That(ast2.Specifiers.Modifiers.ToString(), Is.EqualTo("public"));
            Assert.That(ast2.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast2.DeclaratorList.Declarators.First().As<FuncDeclNode>().Identifier,
                Is.EqualTo("f"));
            Assert.That(ast2.DeclaratorList.Declarators.First().As<FuncDeclNode>().Definition?.Children.Count,
                Is.EqualTo(0));
        }

        [Test]
        public void WithInterfaceSpecificModifiersIMethodTest()
        {
            string src1 = "public static default String f() {}";
            string src2 = "default String f() {}";

            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();
            DeclStatNode ast2 = this.GenerateAST(src2).As<DeclStatNode>();

            Assert.That(ast1.Specifiers.Modifiers.ToString(), Is.EqualTo("public static default"));
            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<FuncDeclNode>().Identifier,
                Is.EqualTo("f"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<FuncDeclNode>().Definition?.Children.Count,
                Is.EqualTo(0));
            Assert.That(ast2.Specifiers.Modifiers.ToString(), Is.EqualTo("default"));
            Assert.That(ast2.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast2.DeclaratorList.Declarators.First().As<FuncDeclNode>().Identifier,
                Is.EqualTo("f"));
            Assert.That(ast2.DeclaratorList.Declarators.First().As<FuncDeclNode>().Definition?.Children.Count,
                Is.EqualTo(0));
        }

        [Test]
        public void WithBracketsInterfaceMethodTest()
        {
            string src1 = "String f()[] {}";
            Assert.That(() => this.GenerateAST(src1), Throws.InstanceOf<NotImplementedException>());
        }

        [Test]
        public void GenericInterfaceMethodTest()
        {
            string src1 = "<Class1> String f(){}";
            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();

            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<FuncDeclNode>().
                TemplateArgs.Declarators.First().Identifier, Is.EqualTo("Class1"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<FuncDeclNode>().Identifier,
                Is.EqualTo("f"));
        }

        [Test]
        public void WitModifiersGenericInterfaceMethodTest()
        {
            string src1 = "public <Class1> String f(){}";
            string src2 = "volatile <Class1> String f(){}";

            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();
            DeclStatNode ast2 = this.GenerateAST(src2).As<DeclStatNode>();

            Assert.That(ast1.Modifiers.ToString(), Is.EqualTo("public"));
            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<FuncDeclNode>().
                TemplateArgs.Declarators.First().Identifier, Is.EqualTo("Class1"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<FuncDeclNode>().Identifier,
                Is.EqualTo("f"));
            Assert.That(ast2.Modifiers.ToString(), Is.EqualTo("volatile"));
            Assert.That(ast2.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast2.DeclaratorList.Declarators.First().As<FuncDeclNode>().
                TemplateArgs.Declarators.First().Identifier, Is.EqualTo("Class1"));
            Assert.That(ast2.DeclaratorList.Declarators.First().As<FuncDeclNode>().Identifier,
                Is.EqualTo("f"));
        }


        protected override ASTNode GenerateAST(string src)
            => new JavaASTBuilder().BuildFromSource(src, p => p.interfaceBodyDeclaration());
    }
}
