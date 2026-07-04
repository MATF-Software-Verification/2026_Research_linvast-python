using System.Linq;
using LINVAST.Imperative.Builders.Go;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Imperative.Visitors;
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

        [Test]
        public void SwitchStatementTests()
        {
            SwitchStatNode @switch = this.GenerateAST("switch 3 { case 1: return 1; default: return 0 }").As<SwitchStatNode>();

            Assert.That(ConstantExpressionEvaluator.Evaluate(@switch.Condition), Is.EqualTo(3));
            Assert.That(@switch.Body.Children.Select(c => c.As<LabeledStatNode>().Label), Is.EqualTo(new[] { "case 1", "default" }));
        }

        [Test]
        public void ConditionlessSwitchStatementTests()
        {
            SwitchStatNode @switch = this.GenerateAST("switch { default: return 0 }").As<SwitchStatNode>();

            Assert.That(ConstantExpressionEvaluator.Evaluate(@switch.Condition), Is.EqualTo(true));
            Assert.That(@switch.Body.Children.Single().As<LabeledStatNode>().Label, Is.EqualTo("default"));
        }

        [Test]
        public void TypeSwitchStatementTests()
        {
            SwitchStatNode @switch = this.GenerateAST("switch v := x.(type) { case int: return 1; default: return 0 }").As<SwitchStatNode>();

            FuncCallExprNode condition = @switch.Condition.As<FuncCallExprNode>();
            Assert.That(condition.Identifier, Is.EqualTo("__linvast_type_switch"));
            Assert.That(@switch.Body.Children.Select(c => c.As<LabeledStatNode>().Label), Is.EqualTo(new[] { "case int", "default" }));
        }

        [Test]
        public void ForClauseStatementTests()
        {
            ForStatNode loop = this.GenerateAST("for x := 0; x < 5; x++ { continue }").As<ForStatNode>();

            Assert.That(loop.InitExpr, Is.InstanceOf<FuncCallExprNode>());
            Assert.That(loop.InitExpr!.As<FuncCallExprNode>().Identifier, Is.EqualTo("__linvast_stmt"));
            Assert.That(loop.Condition, Is.InstanceOf<RelExprNode>());
            Assert.That(loop.IncrExpr, Is.InstanceOf<IncExprNode>());
            Assert.That(loop.Statement.As<BlockStatNode>().Children.Single(), Is.InstanceOf<JumpStatNode>());
        }

        [Test]
        public void RangeForStatementTests()
        {
            ForStatNode loop = this.GenerateAST("for i, v := range xs { break }").As<ForStatNode>();

            Assert.That(loop.InitExpr, Is.InstanceOf<FuncCallExprNode>());
            Assert.That(loop.InitExpr!.As<FuncCallExprNode>().Identifier, Is.EqualTo("__linvast_range"));
            Assert.That(ConstantExpressionEvaluator.Evaluate(loop.Condition), Is.EqualTo(true));
            Assert.That(loop.Statement.As<BlockStatNode>().Children.Single().As<JumpStatNode>().Type, Is.EqualTo(JumpStatType.Break));
        }

        [Test]
        public void BareForStatementTests()
        {
            ForStatNode loop = this.GenerateAST("for { return }").As<ForStatNode>();

            Assert.That(loop.InitExpr, Is.Null);
            Assert.That(loop.IncrExpr, Is.Null);
            Assert.That(ConstantExpressionEvaluator.Evaluate(loop.Condition), Is.EqualTo(true));
            Assert.That(loop.Statement.As<BlockStatNode>().Children.Single().As<JumpStatNode>().ReturnExpr, Is.Null);
        }

        [Test]
        public void IfWithInitializerStatementTests()
        {
            BlockStatNode block = this.GenerateAST("if x := 1; x > 0 { return }").As<BlockStatNode>();

            Assert.That(block.Children.First(), Is.InstanceOf<DeclStatNode>());
            Assert.That(block.Children.Last(), Is.InstanceOf<IfStatNode>());
        }

        [Test]
        public void ElseIfStatementTests()
        {
            IfStatNode node = this.GenerateAST("if x > 0 { return 1 } else if x < 0 { return -1 } else { return 0 }")
                .As<IfStatNode>();

            Assert.That(node.Condition, Is.InstanceOf<RelExprNode>());
            Assert.That(node.ThenStat.As<BlockStatNode>().Children.Single(), Is.InstanceOf<JumpStatNode>());

            IfStatNode elseIf = node.ElseStat!.As<IfStatNode>();
            Assert.That(elseIf.Condition, Is.InstanceOf<RelExprNode>());
            Assert.That(elseIf.ElseStat, Is.InstanceOf<BlockStatNode>());
        }

        [Test]
        public void GoDeferAndChannelStatementTests()
        {
            Assert.That(this.GenerateAST("go f()").As<ExprStatNode>().Expression.As<FuncCallExprNode>().Identifier,
                Is.EqualTo("__linvast_go"));
            Assert.That(this.GenerateAST("defer f()").As<ExprStatNode>().Expression.As<FuncCallExprNode>().Identifier,
                Is.EqualTo("__linvast_defer"));
            Assert.That(this.GenerateAST("ch <- 1").As<ExprStatNode>().Expression.As<FuncCallExprNode>().Identifier,
                Is.EqualTo("__linvast_send"));
            Assert.That(this.GenerateAST("<-ch").As<ExprStatNode>().Expression.As<FuncCallExprNode>().Identifier,
                Is.EqualTo("__linvast_recv"));
        }

        [Test]
        public void SelectStatementTests()
        {
            SwitchStatNode select = this.GenerateAST("select { case ch <- 1: return; case v := <-ch: return v; default: return }")
                .As<SwitchStatNode>();

            Assert.That(ConstantExpressionEvaluator.Evaluate(select.Condition), Is.EqualTo(true));
            Assert.That(select.Body.Children.Select(c => c.As<LabeledStatNode>().Label),
                Is.EqualTo(new[] { "case __linvast_send<>(ch, 1)", "case object v = __linvast_recv<>(ch)", "default" }));
        }

        [Test]
        public void EmptySelectDefaultCaseTest()
        {
            SwitchStatNode select = this.GenerateAST("select { default: }").As<SwitchStatNode>();

            Assert.That(ConstantExpressionEvaluator.Evaluate(select.Condition), Is.EqualTo(true));
            LabeledStatNode @default = select.Body.Children.Single().As<LabeledStatNode>();
            Assert.That(@default.Label, Is.EqualTo("default"));
            Assert.That(@default.Statement.As<BlockStatNode>().Children, Is.Empty);
        }

        [Test]
        public void SwitchWithInitializerAndMultiCaseLabelsTest()
        {
            BlockStatNode lowered = this.GenerateAST("switch x := f(); x { case 1, 2: fallthrough; default: return }")
                .As<BlockStatNode>();

            Assert.That(lowered.Children.First(), Is.InstanceOf<DeclStatNode>());
            SwitchStatNode @switch = lowered.Children.Last().As<SwitchStatNode>();
            Assert.That(@switch.Condition.As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(@switch.Body.Children.Select(c => c.As<LabeledStatNode>().Label),
                Is.EqualTo(new[] { "case 1, 2", "default" }));
            Assert.That(@switch.Body.Children.First().As<LabeledStatNode>().Statement.As<BlockStatNode>().Children.Single()
                .As<ExprStatNode>().Expression.As<FuncCallExprNode>().Identifier, Is.EqualTo("__linvast_fallthrough"));
        }

        [Test]
        public void EmptyLabeledStatementTests()
        {
            LabeledStatNode label = this.GenerateAST("done:").As<LabeledStatNode>();

            Assert.That(label.Label, Is.EqualTo("done"));
            Assert.That(label.Statement, Is.InstanceOf<EmptyStatNode>());
        }

        protected override ASTNode GenerateAST(string src)
            => new GoASTBuilder().BuildFromSource(src, p => p.statement());
    }
}
