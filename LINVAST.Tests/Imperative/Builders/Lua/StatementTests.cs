using System.Linq;
using LINVAST.Imperative.Builders.Lua;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Imperative.Visitors;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Lua
{
    internal sealed class StatementTests : ASTBuilderTestBase
    {
        [Test]
        public void NumericForStatementTest()
        {
            ForStatNode loop = this.GenerateAST("for i = 1, 3 do x = i end").As<ForStatNode>();

            AssignExprNode init = loop.InitExpr!.As<AssignExprNode>();
            Assert.That(init.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("i"));
            Assert.That(ConstantExpressionEvaluator.Evaluate(init.RightOperand), Is.EqualTo(1));

            RelExprNode condition = loop.Condition.As<RelExprNode>();
            Assert.That(condition.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("i"));
            Assert.That(condition.Operator.Symbol, Is.EqualTo("<="));
            Assert.That(ConstantExpressionEvaluator.Evaluate(condition.RightOperand), Is.EqualTo(3));

            AssignExprNode increment = loop.IncrExpr!.As<AssignExprNode>();
            Assert.That(increment.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("i"));
            Assert.That(increment.Operator.Symbol, Is.EqualTo("+="));
            Assert.That(ConstantExpressionEvaluator.Evaluate(increment.RightOperand), Is.EqualTo(1));

            BlockStatNode body = loop.Statement.As<BlockStatNode>();
            AssignExprNode assignment = body.Children.Single().As<ExprStatNode>().Expression.As<AssignExprNode>();
            Assert.That(assignment.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(assignment.RightOperand.As<IdNode>().Identifier, Is.EqualTo("i"));
        }

        [Test]
        public void NumericForStatementWithNegativeStepTest()
        {
            ForStatNode loop = this.GenerateAST("for i = 3, 1, -1 do x = i end").As<ForStatNode>();

            RelExprNode condition = loop.Condition.As<RelExprNode>();
            Assert.That(condition.Operator.Symbol, Is.EqualTo(">="));

            AssignExprNode increment = loop.IncrExpr!.As<AssignExprNode>();
            Assert.That(ConstantExpressionEvaluator.Evaluate(increment.RightOperand), Is.EqualTo(-1));
        }

        [Test]
        public void GenericForStatementTest()
        {
            ForStatNode loop = this.GenerateAST("for k, v in pairs(t) do x = v end").As<ForStatNode>();

            AssignExprNode init = loop.InitExpr!.As<AssignExprNode>();
            Assert.That(init.LeftOperand.As<IdListNode>().Identifiers.Select(i => i.Identifier), Is.EqualTo(new[] { "k", "v" }));

            ExprListNode iterators = init.RightOperand.As<ExprListNode>();
            FuncCallExprNode pairsCall = iterators.Expressions.Single().As<FuncCallExprNode>();
            Assert.That(pairsCall.Identifier, Is.EqualTo("pairs"));
            Assert.That(pairsCall.Arguments!.Expressions.Single().As<IdNode>().Identifier, Is.EqualTo("t"));

            Assert.That(ConstantExpressionEvaluator.Evaluate(loop.Condition), Is.EqualTo(true));

            BlockStatNode body = loop.Statement.As<BlockStatNode>();
            AssignExprNode assignment = body.Children.Single().As<ExprStatNode>().Expression.As<AssignExprNode>();
            Assert.That(assignment.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(assignment.RightOperand.As<IdNode>().Identifier, Is.EqualTo("v"));
        }

        [Test]
        public void IfElseIfElseStatementTest()
        {
            IfStatNode node = this.GenerateAST("if x > 0 then a = 1 elseif x < 0 then a = -1 else a = 0 end")
                .As<IfStatNode>();

            Assert.That(node.Condition, Is.InstanceOf<RelExprNode>());
            Assert.That(node.ThenStat.As<BlockStatNode>().Children.Single(), Is.InstanceOf<ExprStatNode>());

            IfStatNode elseIf = node.ElseStat!.As<IfStatNode>();
            Assert.That(elseIf.Condition, Is.InstanceOf<RelExprNode>());
            Assert.That(elseIf.ElseStat, Is.InstanceOf<BlockStatNode>());
        }

        [Test]
        public void RepeatUntilStatementTest()
        {
            BlockStatNode lowered = this.GenerateAST("repeat x = x + 1 until x > 3").As<BlockStatNode>();

            Assert.That(lowered.Children.First().As<BlockStatNode>().Children.Single(), Is.InstanceOf<ExprStatNode>());
            WhileStatNode loop = lowered.Children.Last().As<WhileStatNode>();
            Assert.That(loop.Condition, Is.InstanceOf<UnaryExprNode>());
            Assert.That(loop.Statement.As<BlockStatNode>().Children.Single(), Is.InstanceOf<ExprStatNode>());
        }

        [Test]
        public void LabelsGotoBreakAndEmptyStatementTests()
        {
            Assert.That(this.GenerateAST(";"), Is.InstanceOf<EmptyStatNode>());

            LabeledStatNode label = this.GenerateAST("::done::").As<LabeledStatNode>();
            Assert.That(label.Label, Is.EqualTo("done"));
            Assert.That(label.Statement, Is.InstanceOf<EmptyStatNode>());

            JumpStatNode @goto = this.GenerateAST("goto done").As<JumpStatNode>();
            Assert.That(@goto.Type, Is.EqualTo(JumpStatType.Goto));
            Assert.That(@goto.GotoLabel!.Identifier, Is.EqualTo("done"));

            Assert.That(this.GenerateAST("break").As<JumpStatNode>().Type, Is.EqualTo(JumpStatType.Break));
        }

        [Test]
        public void EmptyNestedBlockStatementTest()
        {
            BlockStatNode block = this.GenerateAST("do end").As<BlockStatNode>();
            Assert.That(block.Children, Is.Empty);

            WhileStatNode loop = this.GenerateAST("while true do end").As<WhileStatNode>();
            Assert.That(loop.Statement.As<BlockStatNode>().Children, Is.Empty);
        }

        protected override ASTNode GenerateAST(string src)
            => new LuaASTBuilder().BuildFromSource(src, p => p.stat());
    }
}
