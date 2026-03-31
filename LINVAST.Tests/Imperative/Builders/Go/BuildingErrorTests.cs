using System;
using System.IO;
using LINVAST.Exceptions;
using LINVAST.Imperative;
using LINVAST.Imperative.Builders.Go;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Go
{
    internal sealed class BuildingErrorTests : BuildingErrorTestsBase
    {
        [Test]
        public void SourceNotFoundTest()
        {
            Assert.Throws<FileNotFoundException>(() => new ImperativeASTFactory().BuildFromFile("404.go"));
        }

        [Test]
        public void MissingPackageDeclarationTest()
        {
            Assert.Throws<SyntaxErrorException>(() => GenerateAST("var i = 5"));
        }

        [Test]
        public void InvalidDeclarationTests()
        {
            this.AssertThrows<SyntaxErrorException>("package test; func f int { }");
            this.AssertThrows<SyntaxErrorException>("package test; func ()");
            this.AssertThrows<SyntaxErrorException>("package test; func f(0, x) int { }");
            this.AssertThrows<SyntaxErrorException>("package test; func f(3) int { }");
            this.AssertThrows<SyntaxErrorException>("package test; func f(int x[]) int { }");
            this.AssertThrows<SyntaxErrorException>("package test; func f[](x int,) int { }");
            this.AssertThrows<SyntaxErrorException>("package test; func f(int x, int y,,) int { }");
            this.AssertThrows<SyntaxErrorException>("package test; var x = ;;");
            // this.AssertThrows<SyntaxErrorException>("var x int = .3, 2..", p => ((GoParser)p).varDecl());
            this.AssertThrows<SyntaxErrorException>("package test; var x int = ..3");
            this.AssertThrows<SyntaxErrorException>("package test; var x int = ()");
            this.AssertThrows<SyntaxErrorException>("package test; \"math\"");
            this.AssertThrows<SyntaxErrorException>("package test; import");
            this.AssertThrows<SyntaxErrorException>("package test; import * \"math\" ");
        }

        [Test]
        public void InvalidIfStatementTests()
        {
            this.AssertThrows<SyntaxErrorException>("package test; func test() {if x }");
            this.AssertThrows<SyntaxErrorException>("package test; func test() {if {x} {} else {} }");
            this.AssertThrows<SyntaxErrorException>("package test; func test() {if x then { } else { } }");
            this.AssertThrows<SyntaxErrorException>("package test; func test() {if (x > 1 {} }");
            this.AssertThrows<SyntaxErrorException>("package test; func test() {if 1 ;; else ; }");
        }

        [Test]
        public void InvalidForStatementTests()
        {
            this.AssertThrows<SyntaxErrorException>("package test; func test() {for (x := 0; x < 5; x++) {}}");
            this.AssertThrows<NotImplementedException>("package test; func test() {for x := 0; x < 5; x++ {}}");
            this.AssertThrows<SyntaxErrorException>("package test; func test() {for (;;;;){}}");
        }


        protected override ASTNode GenerateAST(string src) => new GoASTBuilder().BuildFromSource(src);
    }
}
