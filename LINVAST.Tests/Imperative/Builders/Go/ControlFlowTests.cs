using LINVAST.Imperative.Builders.C;
using LINVAST.Imperative.Builders.Go;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Go
{
    internal sealed class ControlFlowTests : ControlFlowTestsBase
    {
        [Test]
        public void IfStatementTests()
        {
            this.AssertIfStatement("if 3 > 0 { return 1; }", true);
            this.AssertIfStatement("if 0 != 0 { return 1; }", false);
            this.AssertIfStatement("if 3 { return 1; }", 3);
            this.AssertIfStatement("if 3 { return 1; }", 3);
            this.AssertIfStatement("if 1 { return 1; } else { return 1; }", 1, 1, 1);
            this.AssertIfStatement("if 1 { return 1; } else {}", 1, 1, 0);
            this.AssertIfStatement("if 1 { var x int } else {}", 1, 1, 0);
            this.AssertIfStatement("if 1 { var x int } else { return 1; }", 1, 1, 1);
            this.AssertIfStatement("if 1 { var x int; var y int } else { var x int; var y int; var z int }", 1, 2, 3);
            this.AssertIfStatement("if 1 { return 1; } else { return 0; }", 1, 1, 1);
        }

        [Test]
        public void WhileStatementTests()
        {
            this.AssertWhileStatement("for (3 > 0) { return 1; }", true);
            this.AssertWhileStatement("for (0 != 0) { return 1; }", false);
            this.AssertWhileStatement("for (3) { return 1; }", 3);
            this.AssertWhileStatement("for (1) { return 1; }", 1);
            this.AssertWhileStatement("for (1) { var x int }", 1);
            this.AssertWhileStatement("for (1) { var x int; var y int }", 1, 2);
        }

        protected override ASTNode GenerateAST(string src)
            => new GoASTBuilder().BuildFromSource(src, p => p.statement());
    }
}