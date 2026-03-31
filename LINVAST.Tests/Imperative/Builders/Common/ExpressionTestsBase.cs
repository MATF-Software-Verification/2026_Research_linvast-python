using System;
using System.Linq;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Visitors;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Common
{
    internal abstract class ExpressionTestsBase : ASTBuilderTestBase
    {
        protected void AssertEvaluationException(string code)
        {
            ExprNode expr = this.AssertExpression(code);
            Assert.That(() => ConstantExpressionEvaluator.TryEvaluateAs(expr, out object result), Throws.InstanceOf<EvaluationException>());
        }

        protected void AssertExpressionValue<T>(string code, T expected)
        {
            ExprNode expr = this.AssertExpression(code);
            Assert.That(ConstantExpressionEvaluator.TryEvaluateAs(expr, out T result));
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expected).Within(1e-10));
        }

        protected void AssertNullExpression(string code)
        {
            ExprNode expr = this.AssertExpression(code);
            if (expr is NullLitExprNode @null) {
                Assert.That(@null.Value, Is.Null);
                Assert.That(@null.TypeCode, Is.EqualTo(TypeCode.Empty));
            } else {
                Assert.Fail("Initializer is not of type NullLiteralNode");
            }
        }

        protected ExprNode AssertExpression(string code)
        {
            ExprNode expr = this.GenerateAST(code).As<ExprNode>();
            Assert.That(expr, Is.Not.Null);
            return expr;
        }

        protected void AssertLiteralSuffix(string code, string? suffix, object value, Type type)
        {
            LitExprNode literal = this.GenerateAST(code).As<LitExprNode>();
            Assert.That(literal, Is.Not.Null);
            Assert.That(literal.Value?.GetType(), Is.EqualTo(type));
            Assert.That(literal.Suffix, Is.EqualTo(suffix));
            Assert.That(ConstantExpressionEvaluator.Evaluate(literal), Is.EqualTo(value).Within(1e-10));
        }

        protected void AssertFunctionCallExpression(string code, string fname, params object[] args)
        {
            FuncCallExprNode fcall = this.GenerateAST(code).As<FuncCallExprNode>();
            this.AssertChildrenParentProperties(fcall);
            Assert.That(fcall.Identifier, Is.EqualTo(fname));

            if (args is null || !args.Any()) {
                Assert.That(fcall.Arguments, Is.Null);
            } else {
                Assert.That(fcall.Arguments, Is.Not.Null);
                Assert.That(fcall.Arguments!.Expressions.Count, Is.EqualTo(args.Length));
                foreach ((ExprNode arg, object? expected) in fcall.Arguments!.Expressions.Zip(args))
                    Assert.That(ConstantExpressionEvaluator.Evaluate(arg), Is.EqualTo(expected).Within(1e-10));
            }
        }
    }
}
