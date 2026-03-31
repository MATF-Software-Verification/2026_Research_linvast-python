using System;
using LINVAST.Imperative.Builders.Java;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Java
{
    internal sealed class PackageDeclarationTests : DeclarationTestsBase
    {
        [Test]
        public void PackageDeclarationTest()
        {
            string src1 = "package mypkg;";
            Assert.AreEqual(new PackageNode(1, "mypkg"), this.GenerateAST(src1));
        }


        protected override ASTNode GenerateAST(string src)
            => new JavaASTBuilder().BuildFromSource(src, p => p.packageDeclaration());
    }
}
