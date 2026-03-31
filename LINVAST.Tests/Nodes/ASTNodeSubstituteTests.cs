using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Nodes
{
    internal sealed class ASTNodeSubstituteTests
    {
        [Test]
        public void BasicTest()
        {
            var ast1 = new IdListNode(1, new IdNode(1, "x"), new IdNode(1, "y"));
            var ast2 = new IdListNode(1, new IdNode(1, "X"), new IdNode(1, "y"));
            Assert.That(ast1.Substitute(new IdNode(2, "x"), new IdNode(2, "X")), Is.EqualTo(ast2));
        }

        [Test]
        public void MultipleSubstituteTest()
        {
            var ast1 = new IdListNode(1, new IdNode(1, "x"), new IdNode(1, "x"), new IdNode(1, "y"));
            var ast2 = new IdListNode(1, new IdNode(1, "X"), new IdNode(1, "X"), new IdNode(1, "y"));
            Assert.That(ast1.Substitute(new IdNode(2, "x"), new IdNode(2, "X")), Is.EqualTo(ast2));
        }

        [Test]
        public void ExpressionSubstituteTest()
        {
            var ast1 = new ArithmExprNode(1,
                new IdNode(1, "x"),
                ArithmOpNode.FromSymbol(1, "+"),
                new IdNode(1, "x")
            );
            var ast2 = new ArithmExprNode(1,
                new LitExprNode(1, 1),
                ArithmOpNode.FromSymbol(1, "+"),
                new LitExprNode(1, 1)
            );
            Assert.That(ast1.Substitute(new IdNode(2, "x"), new LitExprNode(2, 1)), Is.EqualTo(ast2));
        }

        [Test]
        public void SelfReferenceSubstituteTest()
        {
            var ast1 = new ArithmExprNode(1,
                new IdNode(1, "x"),
                ArithmOpNode.FromSymbol(1, "+"),
                new ArithmExprNode(1,
                    new IdNode(1, "y"),
                    ArithmOpNode.FromSymbol(1, "+"),
                    new IdNode(1, "x")
                )
            );

            var repl = new ArithmExprNode(1,
                new IdNode(1, "y"),
                new ArithmOpNode(1, "-", BinaryOperations.ArithmeticFromSymbol("-")),
                new IdNode(1, "x")
            );
            var ast2 = new ArithmExprNode(1,
                new IdNode(1, "x"),
                ArithmOpNode.FromSymbol(1, "+"),
                new ArithmExprNode(1,
                    repl,
                    ArithmOpNode.FromSymbol(1, "+"),
                    new IdNode(1, "x")
                )
            );

            Assert.That(ast1.Substitute(new IdNode(2, "y"), repl), Is.EqualTo(ast2));
        }

        [Test]
        public void SubstituteKeepsOriginalTest()
        {
            ASTNode ast1 = new IdListNode(1, new IdNode(1, "x"), new IdNode(1, "y"));
            ASTNode ast2 = new IdListNode(1, new IdNode(1, "X"), new IdNode(1, "y"));
            Assert.That(ast1.Substitute(new IdNode(2, "x"), new IdNode(2, "X")), Is.EqualTo(ast2));
            Assert.That(ast1, Is.EqualTo(new IdListNode(1, new IdNode(1, "x"), new IdNode(1, "y"))));
        }
    }
}
