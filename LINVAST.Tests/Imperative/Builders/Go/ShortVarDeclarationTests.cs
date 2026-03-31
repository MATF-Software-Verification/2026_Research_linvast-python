using System;
using LINVAST.Imperative.Builders.Go;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Go
{
    internal sealed class ShortVarDeclarationTests : DeclarationTestsBase
    { 
        
        [Test]
      public void ShortVarDeclarationTest()
      {
          this.AssertVariableDeclaration("i:= 3", "i", "Int64", value: 3 );
          this.AssertVariableDeclaration("s:= \"str\" ", "s", "String", value: "str" );
          
          string src1 = "i, s := 3, \"str\" ";
          Assert.That(() => this.GenerateAST(src1), Throws.InstanceOf<NotImplementedException>());
          
          string src2 = "f := func() int { return 7 }";
          Assert.That(() => this.GenerateAST(src2), Throws.InstanceOf<NotImplementedException>());
      }
        
        protected override ASTNode GenerateAST(string src)
            => new GoASTBuilder().BuildFromSource(src, p => p.statement().simpleStmt());
    }
}