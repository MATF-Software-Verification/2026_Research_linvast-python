using System;
using System.Linq;
using LINVAST.Imperative.Builders.Java;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Java
{
    internal sealed class VariableDeclarationTests : DeclarationTestsBase
    {
        [Test]
        public void WithoutInitializerLocalVariableTest()
        {
            string src1 = "String str1;";
            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();

            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<VarDeclNode>().Identifier,
                Is.EqualTo("str1"));
            Assert.That(ast1.DeclaratorList.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public void SingleDeclaratorLocalVariableTest()
        {
            string src1 = "String str1 = null;";
            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();

            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<VarDeclNode>().Identifier,
                Is.EqualTo("str1"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<VarDeclNode>().Initializer,
                Is.InstanceOf<NullLitExprNode>());
            Assert.That(ast1.DeclaratorList.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public void MultipleDeclaratorsLocalVariableTest()
        {
            string src1 = "String str1 = null, str2, str3 = null;";
            string src2 = "String str1, str2, str3;";

            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();
            DeclStatNode ast2 = this.GenerateAST(src2).As<DeclStatNode>();

            Assert.That(ast1.DeclaratorList.Children.Count, Is.EqualTo(3));
            Assert.That(ast1.DeclaratorList.Declarators.First().Identifier, Is.EqualTo("str1"));
            Assert.That(ast1.DeclaratorList.Declarators.Last().Identifier, Is.EqualTo("str3"));
            Assert.That(ast2.DeclaratorList.Children.Count, Is.EqualTo(3));
        }

        [Test]
        public void FinalLocalVariableTest()
        {
            string src1 = "final String str = null;";
            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();

            Assert.That(ast1.DeclaratorList.Children.Count, Is.EqualTo(1));
            Assert.That(ast1.Specifiers.Modifiers.ToString(), Is.EqualTo("const"));
            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.First().As<VarDeclNode>().Identifier,
                Is.EqualTo("str"));
            Assert.That(ast1.DeclaratorList.Children.Count, Is.EqualTo(1));
        }

        [Test]
        public void StartLineFinalLocalVariableTest()
        {
            string src1 = @"final 
                            String str = null;";
            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();

            Assert.That(ast1.Specifiers.Modifiers.ToString(), Is.EqualTo("const"));
            Assert.That(ast1.DeclaratorList.Children.Count, Is.EqualTo(1));
            Assert.That(ast1.Specifiers.Line, Is.EqualTo(1));
            Assert.That(ast1.DeclaratorList.Declarators.First().Line, Is.EqualTo(2));
        }

        [Test]
        public void AnnotationLocalVariableTest()
        {
            string src1 = "@Edible(true) String str = null;";
            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();

            Assert.That(ast1.Tags.Single().Identifier, Is.EqualTo("Edible"));
            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.Single().As<VarDeclNode>().Identifier, Is.EqualTo("str"));
        }

        [Test]
        public void ArrayLocalVariableTest()
        {
            string src1 = "String str[] = null;";
            DeclStatNode ast1 = this.GenerateAST(src1).As<DeclStatNode>();

            Assert.That(ast1.Specifiers.TypeName, Is.EqualTo("String"));
            Assert.That(ast1.DeclaratorList.Declarators.Single(), Is.InstanceOf<ArrDeclNode>());
            Assert.That(ast1.DeclaratorList.Declarators.Single().Identifier, Is.EqualTo("str"));

            this.AssertArrayDeclaration("int nums[] = {1, 2};", "int", "nums", init: new object[] { 1, 2 });
        }

        [Test]
        public void MultipleArrayDeclaratorsTest()
        {
            DeclStatNode ast = this.GenerateAST("int left[], right[] = {1, 2};").As<DeclStatNode>();

            Assert.That(ast.Specifiers.TypeName, Is.EqualTo("int"));
            Assert.That(ast.DeclaratorList.Declarators, Has.All.InstanceOf<ArrDeclNode>());
            Assert.That(ast.DeclaratorList.Declarators.Select(d => d.Identifier), Is.EqualTo(new[] { "left", "right" }));

            ArrDeclNode initialized = ast.DeclaratorList.Declarators.Last().As<ArrDeclNode>();
            Assert.That(initialized.Initializer!.Initializers.Select(LINVAST.Imperative.Visitors.ConstantExpressionEvaluator.Evaluate),
                Is.EqualTo(new object[] { 1, 2 }));
        }


        protected override ASTNode GenerateAST(string src)
            => new JavaASTBuilder().BuildFromSource(src, p => p.localVariableDeclaration());
    }
}
