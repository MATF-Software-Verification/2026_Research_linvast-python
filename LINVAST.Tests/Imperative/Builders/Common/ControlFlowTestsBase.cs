using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Visitors;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Common
{
    internal abstract class ControlFlowTestsBase : ASTBuilderTestBase
    {
        protected IfStatNode AssertIfStatement(string src, object condValue, int thenStatementCount = 1, int? elseStatementCount = null)
        {
            IfStatNode node = this.GenerateAST(src).As<IfStatNode>();
            Assert.That(node, Is.Not.Null);
            this.AssertChildrenParentProperties(node);
            Assert.That(ConstantExpressionEvaluator.Evaluate(node.Condition), Is.EqualTo(condValue));
            Assert.That(node.ThenStat.Children, Has.Exactly(thenStatementCount).Items);
            if (elseStatementCount is not null) {
                Assert.That(node.ElseStat, Is.Not.Null);
                Assert.That(node.ElseStat!.Children, Has.Exactly(elseStatementCount.Value).Items);
            } else {
                Assert.That(node.ElseStat, Is.Null);
            }
            return node;
        }

        protected WhileStatNode AssertWhileStatement(string src, object condValue, int statCount = 1)
        {
            WhileStatNode node = this.GenerateAST(src).As<WhileStatNode>();
            Assert.That(node, Is.Not.Null);
            this.AssertChildrenParentProperties(node);
            Assert.That(ConstantExpressionEvaluator.Evaluate(node.Condition), Is.EqualTo(condValue));
            if (node.Statement is BlockStatNode block)
                Assert.That(block.Children, Has.Exactly(statCount).Items);
            else
                Assert.That(node.Statement.Children, Has.Exactly(statCount).Items);
            return node;
        }
    }
}
