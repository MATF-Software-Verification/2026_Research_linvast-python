using System.Linq;
using LINVAST.Imperative.Builders.Java;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Java
{
    internal sealed class ControlFlowTests : ASTBuilderTestBase
    {
        [Test]
        public void SwitchStatementTests()
        {
            SwitchStatNode @switch = this.GenerateAST("switch (x) { case 1: return 1; default: return 0; }").As<SwitchStatNode>();

            Assert.That(@switch.Condition.As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(@switch.Body.Children.Select(c => c.As<LabeledStatNode>().Label), Is.EqualTo(new[] { "case 1", "default" }));
            Assert.That(@switch.Body.Children.Select(c => c.As<LabeledStatNode>().Statement.As<BlockStatNode>().Children.Single()), Has.All.InstanceOf<JumpStatNode>());
        }

        [Test]
        public void SwitchStatementWithStackedLabelsTest()
        {
            SwitchStatNode @switch = this.GenerateAST("switch (x) { case A: case B: return; }").As<SwitchStatNode>();

            LabeledStatNode firstLabel = @switch.Body.Children.Single().As<LabeledStatNode>();
            LabeledStatNode secondLabel = firstLabel.Statement.As<LabeledStatNode>();
            Assert.That(firstLabel.Label, Is.EqualTo("case A"));
            Assert.That(secondLabel.Label, Is.EqualTo("case B"));
            Assert.That(secondLabel.Statement.As<BlockStatNode>().Children.Single(), Is.InstanceOf<JumpStatNode>());
        }

        [Test]
        public void SwitchStatementWithTrailingEmptyLabelsTest()
        {
            SwitchStatNode @switch = this.GenerateAST("switch (x) { case 1: return; case 2: default: }").As<SwitchStatNode>();

            Assert.That(@switch.Body.Children.Select(c => c.As<LabeledStatNode>().Label),
                Is.EqualTo(new[] { "case 1", "case 2", "default" }));
            Assert.That(@switch.Body.Children.Skip(1).Select(c => c.As<LabeledStatNode>().Statement),
                Has.All.InstanceOf<EmptyStatNode>());
        }

        [Test]
        public void ForStatementTests()
        {
            ForStatNode loop = this.GenerateAST("for (int i = 0; i < 3; i++) { continue; }").As<ForStatNode>();

            Assert.That(loop.InitExpr, Is.InstanceOf<FuncCallExprNode>());
            Assert.That(loop.InitExpr!.As<FuncCallExprNode>().Identifier, Is.EqualTo("__linvast_stmt"));
            Assert.That(loop.Condition, Is.InstanceOf<RelExprNode>());
            Assert.That(loop.IncrExpr, Is.InstanceOf<IncExprNode>());
            Assert.That(loop.Statement.As<BlockStatNode>().Children.Single(), Is.InstanceOf<JumpStatNode>());
        }

        [Test]
        public void SparseForStatementTests()
        {
            ForStatNode loop = this.GenerateAST("for (; i < 3;) { break; }").As<ForStatNode>();

            Assert.That(loop.InitExpr, Is.Null);
            Assert.That(loop.Condition, Is.InstanceOf<RelExprNode>());
            Assert.That(loop.IncrExpr, Is.Null);
            Assert.That(loop.Statement.As<BlockStatNode>().Children.Single().As<JumpStatNode>().Type,
                Is.EqualTo(JumpStatType.Break));
        }

        [Test]
        public void EnhancedForStatementTests()
        {
            ForStatNode loop = this.GenerateAST("for (String s : names) { break; }").As<ForStatNode>();

            Assert.That(loop.InitExpr, Is.InstanceOf<FuncCallExprNode>());
            Assert.That(loop.InitExpr!.As<FuncCallExprNode>().Identifier, Is.EqualTo("__linvast_foreach"));
            Assert.That(loop.Statement.As<BlockStatNode>().Children.Single(), Is.InstanceOf<JumpStatNode>());
        }

        [Test]
        public void DoWhileStatementTests()
        {
            BlockStatNode lowered = this.GenerateAST("do { x++; } while (x < 5);").As<BlockStatNode>();

            Assert.That(lowered.Children.First(), Is.InstanceOf<BlockStatNode>());
            Assert.That(lowered.Children.Last(), Is.InstanceOf<WhileStatNode>());
        }

        [Test]
        public void TryCatchFinallyStatementTests()
        {
            BlockStatNode lowered = this.GenerateAST("try { f(); } catch (Exception e) { g(); } finally { h(); }").As<BlockStatNode>();

            Assert.That(lowered.Children.First().As<ExprStatNode>().Expression.As<FuncCallExprNode>().Identifier,
                Is.EqualTo("__linvast_try"));
            Assert.That(lowered.Children.OfType<LabeledStatNode>().Select(l => l.Label),
                Is.EqualTo(new[] { "catch Exception e", "finally" }));

            BlockStatNode multiCatch = this.GenerateAST("try { f(); } catch (IOException | RuntimeException e) { g(); }").As<BlockStatNode>();
            Assert.That(multiCatch.Children.OfType<LabeledStatNode>().Single().Label,
                Is.EqualTo("catch IOException|RuntimeException e"));
        }

        [Test]
        public void TryWithResourcesStatementTests()
        {
            BlockStatNode lowered = this.GenerateAST("try (Reader r = open()) { read(); }").As<BlockStatNode>();

            Assert.That(lowered.Children.First().As<ExprStatNode>().Expression.As<FuncCallExprNode>().Identifier,
                Is.EqualTo("__linvast_try_resources"));
        }

        [Test]
        public void ThrowAssertAndSynchronizedStatementTests()
        {
            Assert.That(this.GenerateAST("throw ex;"), Is.InstanceOf<ThrowStatNode>());
            Assert.That(this.GenerateAST("assert ok : message;").As<ExprStatNode>().Expression.As<FuncCallExprNode>().Identifier,
                Is.EqualTo("__linvast_assert"));

            BlockStatNode synchronized = this.GenerateAST("synchronized (lock) { run(); }").As<BlockStatNode>();
            Assert.That(synchronized.Children.First().As<ExprStatNode>().Expression.As<FuncCallExprNode>().Identifier,
                Is.EqualTo("__linvast_synchronized"));
        }

        protected override ASTNode GenerateAST(string src)
            => new JavaASTBuilder().BuildFromSource(src, p => p.statement());
    }
}
