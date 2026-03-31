using System;
using System.IO;
using LINVAST.Exceptions;
using LINVAST.Imperative;
using LINVAST.Imperative.Builders.C;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.C
{
    internal sealed class BuildingErrorTests : BuildingErrorTestsBase
    {
        [Test]
        public void SourceNotFoundTest()
        {
            Assert.Throws<FileNotFoundException>(() => new ImperativeASTFactory().BuildFromFile("404.c"));
        }

        [Test]
        public void InvalidDeclarationTests()
        {
            this.AssertThrows<NotImplementedException>("something;");
            this.AssertThrows<SyntaxErrorException>("void f { };");
            this.AssertThrows<SyntaxErrorException>("void ();");
            this.AssertThrows<SyntaxErrorException>("int f(int, int x);");
            this.AssertThrows<SyntaxErrorException>("int f(0 x);");
            this.AssertThrows<SyntaxErrorException>("int f(3);");
            this.AssertThrows<SyntaxErrorException>("int f(int[] x);");
            this.AssertThrows<SyntaxErrorException>("int[] f(int x);");
            this.AssertThrows<SyntaxErrorException>("int f[](int x,,);");
            this.AssertThrows<SyntaxErrorException>("int f(int x, int y,);");
            this.AssertThrows<SyntaxErrorException>("int f(int x, int y){}{};");
            this.AssertThrows<SyntaxErrorException>("int x = ;;");
            this.AssertThrows<SyntaxErrorException>("int x = .3, 2..;");
            this.AssertThrows<SyntaxErrorException>("int x = ..3;;");
            this.AssertThrows<SyntaxErrorException>("int x = ();");
        }

        [Test]
        public void InvalidExpressionTests()
        {
            this.AssertThrows<SyntaxErrorException>("int x = 1 +;");
            this.AssertThrows<SyntaxErrorException>("int x = 1 // 2;");
            this.AssertThrows<SyntaxErrorException>("int x = 1++");
            this.AssertThrows<SyntaxErrorException>("int x = 1 +* 2");
            this.AssertThrows<SyntaxErrorException>("int x = 1 << >> 2");
            this.AssertThrows<SyntaxErrorException>("int x = 1 || || 2");
            this.AssertThrows<SyntaxErrorException>("int x = 1 ? 2 : 3 : 2");
            this.AssertThrows<SyntaxErrorException>("int x = f(,);");
        }

        [Test]
        public void InvalidIfStatementTests()
        {
            this.AssertThrows<SyntaxErrorException>("void f () { if (x) }");
            this.AssertThrows<SyntaxErrorException>("void f () { if x {} else {} }");
            this.AssertThrows<SyntaxErrorException>("void f () { if (x) then { } else { } }");
            this.AssertThrows<SyntaxErrorException>("void f () { if (x > 1 {} }");
            this.AssertThrows<SyntaxErrorException>("void f () { if (1) ;; else ; }");
        }

        [Test]
        public void InvalidForStatementTests()
        {
            this.AssertThrows<SyntaxErrorException>("void f () { for (int x, int y; x < y; x++ y++) {} }");
            this.AssertThrows<SyntaxErrorException>("void f () { for (int x, y; x < y; ; x++ y++) {} }");
            this.AssertThrows<SyntaxErrorException>("void f () { for (;;;;){} }");
            this.AssertThrows<SyntaxErrorException>("void f () { for (int i = 0; i < n; i++,) {} }");
        }


        protected override ASTNode GenerateAST(string src)
            => new CASTBuilder().BuildFromSource(src);
    }
}
