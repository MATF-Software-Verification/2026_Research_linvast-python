using System;
using LINVAST.Imperative.Builders.Go;
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
            
            string src1 = "var re, im = complexSqrt(-1)";
            Assert.That(() => this.GenerateAST(src1), Throws.InstanceOf<NotImplementedException>());
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
            string src1 = "var x, y = -1, -2";
            string src2 = "var x, y = -1, \"abc\"";
            Assert.That(() => this.GenerateAST(src1), Throws.InstanceOf<NotImplementedException>());
            Assert.That(() => this.GenerateAST(src2), Throws.InstanceOf<NotImplementedException>());
        }

        [Test]
        public void ConstDeclarationTest()
        {
            string src = "const i";
            Assert.That(() => this.GenerateAST(src), Throws.InstanceOf<NotImplementedException>() );
            
            this.AssertVariableDeclaration("const i int = 0", "i", "int", AccessModifiers.Unspecified, QualifierFlags.Const, value:0);
            this.AssertVariableDeclaration("const j = 0", "j", "Int64", AccessModifiers.Unspecified, QualifierFlags.Const, value:0);
            this.AssertVariableDeclaration("const s = \"abc\"", "s", "String", AccessModifiers.Unspecified, QualifierFlags.Const, value: "abc");
            this.AssertVariableDeclarationList("const x, y float32 = -1, -2", "float32", AccessModifiers.Unspecified,
                QualifierFlags.Const, ("x", -1), ("y", -2));

        }
        
        protected override ASTNode GenerateAST(string src)
                    => new GoASTBuilder().BuildFromSource(src, p => p.declaration());
    }
}