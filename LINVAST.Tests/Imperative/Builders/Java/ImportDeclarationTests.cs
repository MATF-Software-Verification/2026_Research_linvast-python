using System;
using LINVAST.Imperative.Builders.Java;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Java
{
    internal sealed class ImportDeclarationTests : DeclarationTestsBase
    {
        [Test]
        public void SingleNameImportDeclTest()
        {
            string src1 = "import system;";
            string src2 = "import system ;";

            ImportNode ast1 = this.GenerateAST(src1).As<ImportNode>();
            ImportNode ast2 = this.GenerateAST(src2).As<ImportNode>();

            Assert.That(ast1.Directive, Is.EqualTo("system"));
            Assert.That(ast2.Directive, Is.EqualTo("system"));
        }

        [Test]
        public void WithDotsImportDeclTest()
        {
            string src1 = "import name1.name2.name3.name4;";
            string src2 = "import system.text.json;";
            string src3 = "import system.text ;";

            ImportNode ast1 = this.GenerateAST(src1).As<ImportNode>();
            ImportNode ast2 = this.GenerateAST(src2).As<ImportNode>();
            ImportNode ast3 = this.GenerateAST(src3).As<ImportNode>();

            Assert.That(ast1.Directive, Is.EqualTo("name1.name2.name3.name4"));
            Assert.That(ast2.Directive, Is.EqualTo("system.text.json"));
            Assert.That(ast3.Directive, Is.EqualTo("system.text"));
        }

        [Test]
        public void WithWildcardImportDeclTest()
        {
            string src1 = "import system.text.json.*;";
            string src2 = "import system.text. *;";
            string src3 = "import system.utils.*;";

            ImportNode ast1 = this.GenerateAST(src1).As<ImportNode>();
            ImportNode ast2 = this.GenerateAST(src2).As<ImportNode>();
            ImportNode ast3 = this.GenerateAST(src3).As<ImportNode>();

            Assert.That(ast1.Directive, Is.EqualTo("system.text.json.*"));
            Assert.That(ast2.Directive, Is.EqualTo("system.text.*"));
            Assert.That(ast3.Directive, Is.EqualTo("system.utils.*"));
        }

        [Test]
        public void StaticImportDeclTest()
        {
            string src1 = "import static system.text.json.*;";
            string src2 = "import static system;";
            string src3 = "import static system.text;";

            Assert.That(() => this.GenerateAST(src1), Throws.Nothing);
            Assert.That(() => this.GenerateAST(src2), Throws.Nothing);
            Assert.That(() => this.GenerateAST(src3), Throws.Nothing);
        }


        protected override ASTNode GenerateAST(string src)
            => new JavaASTBuilder().BuildFromSource(src, p => p.importDeclaration());
    }
}
