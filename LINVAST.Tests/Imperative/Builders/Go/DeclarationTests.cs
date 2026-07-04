using System;
using System.Linq;
using LINVAST.Imperative.Builders.Go;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;


namespace LINVAST.Tests.Imperative.Builders.Go
{
    internal sealed class DeclarationTests : DeclarationTestsBase
    {
        [Test]
        public void SimpleDeclarationWithTypeTest()
        {
            this.AssertVariableDeclaration("var i int", "i", "int");
            this.AssertVariableDeclaration("var a string", "a", "string");
            this.AssertVariableDeclaration("var l float32", "l", "float32");
            this.AssertVariableDeclaration("var p Point", "p", "Point");
            this.AssertVariableDeclaration("var b bool", "b", "bool");
            this.AssertVariableDeclaration("var c string = \"abc\"", "c", "string", value: "abc");
            this.AssertVariableDeclaration("var j int = 1", "j", "int", value:1);
        }

        [Test]
        public void SimpleDeclarationWithoutTypeTest()
        {
            this.AssertVariableDeclaration("var i = 33", "i", "Int64", value: 33);
            this.AssertVariableDeclaration("var a = \"abc\" ", "a", "String", value: "abc");

            DeclStatNode decl = this.AssertDeclarationNode("var re, im = complexSqrt(-1)", "object");
            Assert.That(decl.DeclaratorList.Declarators.Select(d => d.Identifier), Is.EqualTo(new[] { "re", "im" }));
            Assert.That(decl.DeclaratorList.Declarators.Select(d => d.As<VarDeclNode>().Initializer!.As<FuncCallExprNode>().Identifier),
                Is.EqualTo(new[] { "__linvast_multi_value", "__linvast_multi_value" }));
        }


        [Test]
        public void VariableDeclarationListWithTypeTest()
        {
            this.AssertVariableDeclarationList("var U, V, W float64", "float64", AccessModifiers.Unspecified,
                QualifierFlags.None, ("U", null), ("V", null), ("W", null));
            
            this.AssertVariableDeclarationList("var x, y float32 = -1, -2", "float32", AccessModifiers.Unspecified,
                QualifierFlags.None, ("x", -1), ("y", -2));
        }

        [Test]
        public void VariableDeclarationListWithoutTypeTest()
        {
            this.AssertVariableDeclarationList("var x, y = -1, -2", "object", AccessModifiers.Unspecified,
                QualifierFlags.None, ("x", -1), ("y", -2));
            this.AssertVariableDeclarationList("var x, y = -1, \"abc\"", "object", AccessModifiers.Unspecified,
                QualifierFlags.None, ("x", -1), ("y", "abc"));
        }

        [Test]
        public void ConstDeclarationTest()
        {
            this.AssertVariableDeclaration("const i", "i", "object", AccessModifiers.Unspecified, QualifierFlags.Const);
            
            this.AssertVariableDeclaration("const i int = 0", "i", "int", AccessModifiers.Unspecified, QualifierFlags.Const, value:0);
            this.AssertVariableDeclaration("const j = 0", "j", "Int64", AccessModifiers.Unspecified, QualifierFlags.Const, value:0);
            this.AssertVariableDeclaration("const s = \"abc\"", "s", "String", AccessModifiers.Unspecified, QualifierFlags.Const, value: "abc");
            this.AssertVariableDeclarationList("const x, y float32 = -1, -2", "float32", AccessModifiers.Unspecified,
                QualifierFlags.Const, ("x", -1), ("y", -2));

        }

        [Test]
        public void ParenthesizedDeclarationBlocksTest()
        {
            BlockStatNode vars = this.GenerateAST("var (x int; y = 2)").As<BlockStatNode>();
            Assert.That(vars.Children, Has.Exactly(2).Items);
            Assert.That(vars.Children.Cast<DeclStatNode>().Select(d => d.DeclaratorList.Declarators.Single().Identifier),
                Is.EqualTo(new[] { "x", "y" }));

            BlockStatNode consts = this.GenerateAST("const (a = 1; b string = \"two\")").As<BlockStatNode>();
            Assert.That(consts.Children, Has.Exactly(2).Items);
            Assert.That(consts.Children.Cast<DeclStatNode>().Select(d => d.Modifiers.QualifierFlags), Has.All.EqualTo(QualifierFlags.Const));
        }
        
        protected override ASTNode GenerateAST(string src)
                    => new GoASTBuilder().BuildFromSource(src, p => p.declaration());
    }
}
