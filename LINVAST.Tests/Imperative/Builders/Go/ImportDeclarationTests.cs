using System;
using System.Linq;
using LINVAST.Imperative.Builders.Go;
using LINVAST.Nodes;
using LINVAST.Imperative.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;


namespace LINVAST.Tests.Imperative.Builders.Go
{
    internal sealed class ImportDeclarationTests : DeclarationTestsBase
    {
        [Test]
        public void SimpleImportDeclarationTest()
        {
            string src1 = "import \"lib/math\"";
            string src2 = "import \"fmt\"";
            
            ImportNode ast1 = this.GenerateAST(src1).Children.First().As<ImportNode>();
            ImportNode ast2 = this.GenerateAST(src2).Children.First().As<ImportNode>();

            Assert.That(ast1.Directive, Is.EqualTo("\"lib/math\""));
            Assert.That(ast2.Directive, Is.EqualTo("\"fmt\""));
        }

        [Test]
        public void ImportDeclarationWithIdentifierTest()
        {
            string src1 = "import lm \"lib/math\"";
            string src2 = "import f \"fmt\"";

            ImportNode ast1 = this.GenerateAST(src1).Children.First().As<ImportNode>();
            ImportNode ast2 = this.GenerateAST(src2).Children.First().As<ImportNode>();

            Assert.That(ast1.Directive, Is.EqualTo("\"lib/math\""));
            Assert.That(ast1.QualifiedAs, Is.EqualTo("lm"));

            Assert.That(ast2.Directive, Is.EqualTo("\"fmt\""));
            Assert.That(ast2.QualifiedAs, Is.EqualTo("f"));
            
        }

        [Test]
        public void ImportDeclarationWithDotTest()
        {
            string src1 = "import . \"lib/math\"";
            string src2 = "import . \"fmt\"";

            ImportNode ast1 = this.GenerateAST(src1).Children.First().As<ImportNode>();
            ImportNode ast2 = this.GenerateAST(src2).Children.First().As<ImportNode>();

            Assert.That(ast1.Directive, Is.EqualTo("\"lib/math\""));
            Assert.That(ast1.QualifiedAs, Is.EqualTo(""));

            Assert.That(ast2.Directive, Is.EqualTo("\"fmt\""));
            Assert.That(ast2.QualifiedAs, Is.EqualTo(""));

        }
        
        [Test]
        public void ImportDeclarationListTest()
        {
            string src1 = "import (" +
                         "\"lib/math\" \n" +
                         "\"fmt\" \n" +
                         ")";
            
            string src2 = "import (" +
                          ". \"lib/math\" \n" +
                          "f \"fmt\" \n" +
                          ")";

            ImportListNode ast1 = this.GenerateAST(src1).As<ImportListNode>();
            ImportListNode ast2 = this.GenerateAST(src2).As<ImportListNode>();
            
            Assert.That(ast1.Children[0].As<ImportNode>().Directive, Is.EqualTo("\"lib/math\""));
            Assert.That(ast1.Children[1].As<ImportNode>().Directive, Is.EqualTo("\"fmt\""));
            
            Assert.That(ast2.Children[0].As<ImportNode>().Directive, Is.EqualTo("\"lib/math\""));
            Assert.That(ast2.Children[0].As<ImportNode>().QualifiedAs, Is.EqualTo(""));

            Assert.That(ast2.Children[1].As<ImportNode>().Directive, Is.EqualTo("\"fmt\""));
            Assert.That(ast2.Children[1].As<ImportNode>().QualifiedAs, Is.EqualTo("f"));
        }
        
        protected override ASTNode GenerateAST(string src)
            => new GoASTBuilder().BuildFromSource(src, p => p.importDecl());
    }
}