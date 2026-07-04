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
    internal sealed class ShortVarDeclarationTests : DeclarationTestsBase
    { 
        
        [Test]
      public void ShortVarDeclarationTest()
      {
          this.AssertVariableDeclaration("i:= 3", "i", "Int64", value: 3 );
          this.AssertVariableDeclaration("s:= \"str\" ", "s", "String", value: "str" );
          
          this.AssertVariableDeclarationList("i, s := 3, \"str\" ", "object", AccessModifiers.Unspecified,
              QualifierFlags.None, ("i", 3), ("s", "str"));
          
          DeclStatNode funcDecl = this.AssertDeclarationNode("f := func() int { return 7 }", "object");
          Assert.That(funcDecl.DeclaratorList.Declarators.Single().As<VarDeclNode>().Initializer, Is.InstanceOf<LambdaFuncExprNode>());
      }
        
        protected override ASTNode GenerateAST(string src)
            => new GoASTBuilder().BuildFromSource(src, p => p.statement().simpleStmt());
    }
}
