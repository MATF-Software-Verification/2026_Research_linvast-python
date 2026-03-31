using LINVAST.Imperative.Builders.C;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.C
{
    internal sealed class ControlFlowTests : ControlFlowTestsBase
    {
        [Test]
        public void IfStatementTests()
        {
            this.AssertIfStatement("if (3 > 0) return 1;", true);
            this.AssertIfStatement("if (0 != 0) return 1;", false);
            this.AssertIfStatement("if (3) return 1;", 3);
            this.AssertIfStatement("if (3) { return 1; }", 3);
            this.AssertIfStatement("if (1) { return 1; } else return 1;", 1, 1, 1);
            this.AssertIfStatement("if (1) { return 1; } else {}", 1, 1, 0);
            this.AssertIfStatement("if (1) { ; } else {}", 1, 1, 0);
            this.AssertIfStatement("if (1) { ; } else { return 1; }", 1, 1, 1);
            this.AssertIfStatement("if (1) { ; ; } else { ; ; ; }", 1, 2, 3);
            this.AssertIfStatement("if (1) return 1; else return 0;", 1, 1, 1);
        }

        [Test]
        public void WhileStatementTests()
        {
            this.AssertWhileStatement("while (3 > 0) return 1;", true);
            this.AssertWhileStatement("while (0 != 0) return 1;", false);
            this.AssertWhileStatement("while (3) return 1;", 3);
            this.AssertWhileStatement("while (1) { return 1; }", 1);
            this.AssertWhileStatement("while (1) { ; }", 1);
            this.AssertWhileStatement("while (1) { ; ; }", 1, 2);
        }

        [Test]
        public void ForStatementTests()
        {
            // TODO
            Assert.Inconclusive();
        }

        [Test]
        public void RepeatUntilStatementTests()
        {
            // TODO
            Assert.Inconclusive();
        }

        [Test]
        public void GotoStatementTests()
        {
            // TODO
            Assert.Inconclusive();
        }



        protected override ASTNode GenerateAST(string src)
            => new CASTBuilder().BuildFromSource(src, p => p.statement());
    }
}