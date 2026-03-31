using System.Collections.Generic;
using LINVAST.Exceptions;
using LINVAST.Imperative.Builders.Pseudo;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Visitors;
using NUnit.Framework;
using Expr = MathNet.Symbolics.SymbolicExpression;

namespace LINVAST.Tests.Visitors
{
    internal sealed class ExpressionEvaluatorTests
    {
        private readonly Dictionary<string, Expr> symbols = new()
        {
            { "one", 1 },
            { "two", 2 },
            { "twoo", Expr.Variable("two") },
            { "four", Expr.Parse("twoo + 2") },
            { "cycle1", Expr.Variable("cycle1") },
            { "cycle2", Expr.Variable("cycle2") },
        };


        [Test]
        public void ConstantExpressionTests()
        {
            Assert.That(this.Evaluate("4"), Is.EqualTo("4"));
            Assert.That(this.Evaluate("-4"), Is.EqualTo("-4"));
            Assert.That(this.Evaluate("1 + 3"), Is.EqualTo("4"));
            Assert.That(this.Evaluate("1 + (3*2)"), Is.EqualTo("7"));
            Assert.That(this.Evaluate("(1/1) + (3*2)"), Is.EqualTo("7"));
            Assert.That(this.Evaluate("(1/1) - (1 + 1) + (3*2)"), Is.EqualTo("5"));
            Assert.That(this.Evaluate("(1 + 3)*2 - 1"), Is.EqualTo("7"));
            Assert.That(this.Evaluate("(1 + 3)*3 - 1"), Is.EqualTo("11"));
        }

        [Test]
        public void VariableExpressionTests()
        {
            Assert.That(this.Evaluate("1 + a"), Is.EqualTo("1 + a"));
            Assert.That(this.Evaluate("0 - a"), Is.EqualTo("-a"));
            Assert.That(this.Evaluate("a + 1"), Is.EqualTo("1 + a"));
            Assert.That(this.Evaluate("a * 2 + 1"), Is.EqualTo("1 + 2*a"));
            Assert.That(this.Evaluate("a + a + 1"), Is.EqualTo("1 + 2*a"));
            Assert.That(this.Evaluate("1 + one"), Is.EqualTo("2"));
            Assert.That(this.Evaluate("two + 1"), Is.EqualTo("3"));
            Assert.That(this.Evaluate("two * 2 + 1"), Is.EqualTo("5"));
            Assert.That(this.Evaluate("twoo + four + 1"), Is.EqualTo("7"));
            Assert.That(this.Evaluate("twoo + four + five"), Is.EqualTo("6 + five"));
            Assert.That(this.Evaluate("-two"), Is.EqualTo("-2"));
        }

        [Test]
        public void InfiniteCycleTest()
        {
            Assert.That(() => this.Evaluate("cycle1"), Throws.InstanceOf<EvaluationException>());
        }


        private string Evaluate(string expr)
            => ExpressionEvaluator.TryEvaluate(this.GenerateAST(expr), this.symbols).ToString();

        private ExprNode GenerateAST(string src)
            => new PseudoASTBuilder().BuildFromSource(src, p => p.exp()).As<ExprNode>();
    }
}
