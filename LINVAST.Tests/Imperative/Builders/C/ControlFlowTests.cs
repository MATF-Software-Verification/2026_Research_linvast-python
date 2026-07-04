using System.Linq;
using LINVAST.Imperative.Builders.C;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Imperative.Visitors;
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
            ForStatNode counted = this.GenerateAST("for (int i = 0; i < 3; i++) { continue; }").As<ForStatNode>();

            Assert.That(counted.ForDeclaration, Is.InstanceOf<DeclListNode>());
            Assert.That(counted.ForDeclaration!.As<DeclListNode>().Declarators.Single().Identifier, Is.EqualTo("i"));
            Assert.That(counted.Condition, Is.InstanceOf<ExprListNode>());
            Assert.That(counted.Condition.As<ExprListNode>().Expressions.Single(), Is.InstanceOf<RelExprNode>());
            Assert.That(counted.IncrExpr, Is.InstanceOf<ExprListNode>());
            Assert.That(counted.IncrExpr!.As<ExprListNode>().Expressions.Single(), Is.InstanceOf<IncExprNode>());
            Assert.That(counted.Statement.As<BlockStatNode>().Children.Single().As<JumpStatNode>().Type,
                Is.EqualTo(JumpStatType.Continue));

            ForStatNode sparse = this.GenerateAST("for (; i < 3;) break;").As<ForStatNode>();
            Assert.That(sparse.InitExpr, Is.Null);
            Assert.That(sparse.Condition, Is.InstanceOf<ExprListNode>());
            Assert.That(sparse.Condition.As<ExprListNode>().Expressions.Single(), Is.InstanceOf<RelExprNode>());
            Assert.That(sparse.IncrExpr, Is.Null);
            Assert.That(sparse.Statement.As<JumpStatNode>().Type, Is.EqualTo(JumpStatType.Break));
        }

        [Test]
        public void RepeatUntilStatementTests()
        {
            BlockStatNode node = this.GenerateAST("do return 1; while (0);").As<BlockStatNode>();

            Assert.That(node.Children, Has.Exactly(2).Items);
            Assert.That(node.Children[0], Is.InstanceOf<JumpStatNode>());
            WhileStatNode loop = node.Children[1].As<WhileStatNode>();
            Assert.That(ConstantExpressionEvaluator.Evaluate(loop.Condition), Is.EqualTo(0));
            Assert.That(loop.Statement, Is.InstanceOf<JumpStatNode>());
        }

        [Test]
        public void GotoStatementTests()
        {
            LabeledStatNode label = this.GenerateAST("label: return 1;").As<LabeledStatNode>();
            Assert.That(label.Label, Is.EqualTo("label"));
            Assert.That(label.Statement, Is.InstanceOf<JumpStatNode>());

            JumpStatNode @goto = this.GenerateAST("goto label;").As<JumpStatNode>();
            Assert.That(@goto.Type, Is.EqualTo(JumpStatType.Goto));
            Assert.That(@goto.GotoLabel!.Identifier, Is.EqualTo("label"));

            LabeledStatNode @case = this.GenerateAST("case 1: return 1;").As<LabeledStatNode>();
            Assert.That(@case.Label, Is.EqualTo("case 1"));

            LabeledStatNode @default = this.GenerateAST("default: return 0;").As<LabeledStatNode>();
            Assert.That(@default.Label, Is.EqualTo("default"));
        }

        [Test]
        public void SwitchStatementTests()
        {
            SwitchStatNode @switch = this.GenerateAST("switch (3) { case 1: return 1; default: return 0; }").As<SwitchStatNode>();

            Assert.That(ConstantExpressionEvaluator.Evaluate(@switch.Condition), Is.EqualTo(3));
            Assert.That(@switch.Body.Children, Has.Exactly(2).Items);
            Assert.That(@switch.Body.Children[0].As<LabeledStatNode>().Label, Is.EqualTo("case 1"));
            Assert.That(@switch.Body.Children[0].As<LabeledStatNode>().Statement, Is.InstanceOf<JumpStatNode>());
            Assert.That(@switch.Body.Children[1].As<LabeledStatNode>().Label, Is.EqualTo("default"));
            Assert.That(@switch.Body.Children[1].As<LabeledStatNode>().Statement, Is.InstanceOf<JumpStatNode>());
        }

        [Test]
        public void SwitchStatementWithStackedAndEmptyLabelsTest()
        {
            SwitchStatNode @switch = this.GenerateAST("switch (x) { case 1: case 2: break; default: ; }").As<SwitchStatNode>();

            Assert.That(@switch.Condition.As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(@switch.Body.Children, Has.Exactly(2).Items);

            LabeledStatNode first = @switch.Body.Children.First().As<LabeledStatNode>();
            Assert.That(first.Label, Is.EqualTo("case 1"));
            LabeledStatNode second = first.Statement.As<LabeledStatNode>();
            Assert.That(second.Label, Is.EqualTo("case 2"));
            Assert.That(second.Statement.As<JumpStatNode>().Type, Is.EqualTo(JumpStatType.Break));

            LabeledStatNode @default = @switch.Body.Children.Last().As<LabeledStatNode>();
            Assert.That(@default.Label, Is.EqualTo("default"));
            Assert.That(@default.Statement, Is.InstanceOf<EmptyStatNode>());
        }


        protected override ASTNode GenerateAST(string src)
            => new CASTBuilder().BuildFromSource(src, p => p.statement());
    }
}
