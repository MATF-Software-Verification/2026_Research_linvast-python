using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Visitors;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class ExpressionTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void LiteralExpressionTests()
        {
            this.AssertExpressionValue("42", 42L);
            this.AssertExpressionValue("\"hello\"", "hello");

            Assert.That(this.ParseExpression("None"), Is.TypeOf<NullLitExprNode>());
        }

        private ExprNode ParseExpression(string source)
            => this.builder.BuildFromSource(source, parser => parser.test()).As<ExprNode>();

        private void AssertExpressionValue<T>(string source, T expected)
        {
            ExprNode expression = this.ParseExpression(source);
            Assert.That(ConstantExpressionEvaluator.TryEvaluateAs(expression, out T result));
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
