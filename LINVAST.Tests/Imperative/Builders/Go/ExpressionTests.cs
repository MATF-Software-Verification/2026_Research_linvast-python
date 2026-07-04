using System;
using System.Linq;
using LINVAST.Imperative.Builders.C;
using LINVAST.Imperative.Builders.Go;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Visitors;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Go
{
    internal sealed class ExpressionTests : ExpressionTestsBase
    {
        [Test]
        public void LiteralExpressionTest()
        {
            this.AssertExpressionValue("3", 3L);
            this.AssertExpressionValue("2.3", 2.3);
            this.AssertExpressionValue("'a'", 'a');
            this.AssertExpressionValue("\"abc\"", "abc");
        }

        [Test]
        public void ArithmeticExpressionTest()
        {
            this.AssertExpressionValue("1 << (1 + 1 * 2) >> 3", 1L);
            this.AssertExpressionValue("2.3 + 4.0 / 2.0", 4.3);
            this.AssertExpressionValue("3.3 + (4.1 - 1.1) * 2.0", 9.3);
            this.AssertExpressionValue("1 << (1 + 1 * 2) >> 3", 1L);
            this.AssertExpressionValue("2.3 + 4 / 2", 4.3);
            this.AssertExpressionValue("3.3 + (4.1 - 1.1) * 2", 9.3);
        }

        [Test]
        public void ArithmeticBitwiseExpressionTest()
        {
            this.AssertExpressionValue("1 | ^0", ~0L);
            this.AssertExpressionValue("1 | ^1", ~0L);
            this.AssertExpressionValue("1 & ^0", 1L & ~0L);
            this.AssertExpressionValue("(1 << 1) & ^0", (1L << 1) & ~0L);
            this.AssertExpressionValue("(1 << 10 >> 2) ^ (^0 << 10)", (1L << 10 >> 2) ^ (~0L << 10));
        }

        [Test]
        public void RelationalExpressionTest()
        {
            this.AssertExpressionValue("1 > 1", false);
            this.AssertExpressionValue("1 >= 1", true);
            this.AssertExpressionValue("2 > 1", true);
            this.AssertExpressionValue("3 < 1", false);
            this.AssertExpressionValue("1 != 1", false);
            this.AssertExpressionValue("(1 + 1) == 2", true);
            this.AssertExpressionValue("1.1 > 1.0", true);
            this.AssertExpressionValue("1.101 >= 1.1", true);
            this.AssertExpressionValue("(1 + 3 * 2) > 7", false);
            this.AssertExpressionValue("3.0 + 0.1 > 3.0", true);
            this.AssertExpressionValue("1.01 != 1.0", true);
            this.AssertExpressionValue("(1 << 1) == 2", true);
            this.AssertExpressionValue("(2 >> 1) == 1", true);
        }

        [Test]
        public void LogicExpressionTest()
        {
            this.AssertExpressionValue("1 || 1", true);
            this.AssertExpressionValue("1 || 0", true);
            this.AssertExpressionValue("0 || 0", false);
            this.AssertExpressionValue("0 && 0", false);
            this.AssertExpressionValue("0 && 1", false);
            this.AssertExpressionValue("1 && 0", false);
            this.AssertExpressionValue("1 && 1", true);
            this.AssertExpressionValue("0.0001 && 1.1", true);
            this.AssertExpressionValue("1 > 1 && 2 < 3", false);
            this.AssertExpressionValue("1 >= 1 && 2 < 3", true);
            this.AssertExpressionValue("1 >= 1 || 3 < 3", true);
            this.AssertExpressionValue("1 > 1 || 3 >= 3", true);
            this.AssertExpressionValue("1 > 1 || 3 > 3", false);
            this.AssertExpressionValue("2 > 1 && 1 != 2 && 2 <= 3", true);
            this.AssertExpressionValue("2 > 1 && 1 != 2 && 2 > 3", false);
            this.AssertExpressionValue("3 < 1 || 2 < 1 || 1 > 1", false);
            this.AssertExpressionValue("3 < 1 || 2 > 1 || 1 == 1", true);
            this.AssertExpressionValue("1 != 1 || 1 == 1", true);
            this.AssertExpressionValue("(1 + 1) == 2 || 2 == 2", true);
            this.AssertExpressionValue("1.1 > 1.0 && 1.0 > 1.02", false);
            this.AssertExpressionValue("1.1 > 1.0 || 1.0 > 1.02", true);
            this.AssertExpressionValue("1.101 >= 1.1 && (7 > 3.2 || 2 > 3)", true);
            this.AssertExpressionValue("(1 + 3 * 2 > 3 && 4 != 2.0) && 8 > 7", true);
            this.AssertExpressionValue("1 != 0 && 1 > 1 || 1 == 1", true);
            this.AssertExpressionValue("1 != 0 && (1 == 1 || 1 == 3)", true);
            this.AssertExpressionValue("1 != 0 && (1 != 1 || 1 == 3)", false);
            this.AssertExpressionValue("3 > 2 || 3 > 1 && 1 > 1", true);
            this.AssertExpressionValue("(3 > 2 || 3 > 1) && 1 > 1", false);
            this.AssertExpressionValue("(1 << 1) == 2 && (3 / 2 == 1)", true);
            this.AssertExpressionValue("(1 << 1) == 2 && (3 / 2 != 1)", false);
            this.AssertExpressionValue("(1 << 1) == 2 || (3 / 2 != 1)", true);
            this.AssertExpressionValue("(1 << 2) == 2 || (3 / 2 != 1)", false);
            this.AssertExpressionValue("(1 << 1) == (4 >> 1) || 1 != 1", true);
        }

        [Test]
        public void UnaryExpressionTests()
        {
            this.AssertExpressionValue("-1", -1L);
            this.AssertExpressionValue("^0", ~0L);
            this.AssertExpressionValue("^(^0)", 0L);
            this.AssertExpressionValue("!0", true);
            this.AssertExpressionValue("!(1 > 2)", true);
            this.AssertExpressionValue("!1", false);
            this.AssertExpressionValue("!(1 != 0)", false);
            this.AssertExpressionValue("(!1) != (!1)", false);
            this.AssertExpressionValue("(!1) != 0", false);
        }

        [Test]
        public void FunctionCallParameterTests()
        {
            this.AssertFunctionCallExpression("f()", "f");
            this.AssertFunctionCallExpression("g(3)", "g", 3);
            this.AssertFunctionCallExpression("g(3, 2)", "g", 3, 2);
            this.AssertFunctionCallExpression("g(3, 'a')", "g", 3, 'a');
            this.AssertFunctionCallExpression("g(3.1 + 1, 2 * 3)", "g", 3.1 + 1, 2 * 3);
            this.AssertFunctionCallExpression("g(((1 << 2) + 4) >> 3)", "g", ((1 << 2) + 4) >> 3);
            this.AssertFunctionCallExpression("g(1.1 > 1.0 && 1.0 > 1.02)", "g", false);
            this.AssertFunctionCallExpression("h(1.01 > 1.0 || 1.0 > 1.02)", "h", true);
        }

        [Test]
        public void ExtendedLiteralExpressionTests()
        {
            Assert.That(this.GenerateAST("nil"), Is.InstanceOf<NullLitExprNode>());
            Assert.That(ConstantExpressionEvaluator.Evaluate(this.GenerateAST("0b1010").As<LitExprNode>()), Is.EqualTo(10L));
            Assert.That(ConstantExpressionEvaluator.Evaluate(this.GenerateAST("077").As<LitExprNode>()), Is.EqualTo(63L));
            Assert.That(ConstantExpressionEvaluator.Evaluate(this.GenerateAST("`raw string`").As<LitExprNode>()), Is.EqualTo("raw string"));
            Assert.That(ConstantExpressionEvaluator.Evaluate(this.GenerateAST("1i").As<LitExprNode>()), Is.EqualTo("1i"));
        }

        [Test]
        public void CompositeSliceAndAssertionExpressionTests()
        {
            ConsExprNode composite = this.GenerateAST("[]int{1, 2}").As<ConsExprNode>();
            Assert.That(composite.Identifier, Is.EqualTo("[]int"));
            Assert.That(composite.Arguments!.Expressions.Select(ConstantExpressionEvaluator.Evaluate), Is.EqualTo(new object[] { 1L, 2L }));

            FuncCallExprNode slice = this.GenerateAST("xs[1:3]").As<FuncCallExprNode>();
            Assert.That(slice.Identifier, Is.EqualTo("__linvast_slice"));
            Assert.That(slice.Arguments!.Expressions.First().As<IdNode>().Identifier, Is.EqualTo("xs"));
            Assert.That(slice.Arguments!.Expressions.Last().As<IdNode>().Identifier, Is.EqualTo("[1:3]"));

            FuncCallExprNode openSlice = this.GenerateAST("xs[:3]").As<FuncCallExprNode>();
            Assert.That(openSlice.Identifier, Is.EqualTo("__linvast_slice"));
            Assert.That(openSlice.Arguments!.Expressions.Last().As<IdNode>().Identifier, Is.EqualTo("[:3]"));

            FuncCallExprNode assertion = this.GenerateAST("x.(int)").As<FuncCallExprNode>();
            Assert.That(assertion.Identifier, Is.EqualTo("__linvast_type_assert"));
        }

        [Test]
        public void KeyedCompositeLiteralExpressionTests()
        {
            ConsExprNode composite = this.GenerateAST("Point{x: 1, y: 2}").As<ConsExprNode>();

            Assert.That(composite.Identifier, Is.EqualTo("Point"));
            Assert.That(composite.Arguments!.Expressions, Has.All.InstanceOf<DictEntryNode>());
            Assert.That(composite.Arguments!.Expressions.Cast<DictEntryNode>().Select(e => e.Key.Identifier),
                Is.EqualTo(new[] { "x", "y" }));
            Assert.That(composite.Arguments!.Expressions.Cast<DictEntryNode>().Select(e => ConstantExpressionEvaluator.Evaluate(e.Value)),
                Is.EqualTo(new object[] { 1L, 2L }));
        }

        [Test]
        public void ConversionAndVariadicCallExpressionTests()
        {
            ConsExprNode conversion = this.GenerateAST("[]int(xs)").As<ConsExprNode>();
            Assert.That(conversion.Identifier, Is.EqualTo("[]int"));

            FuncCallExprNode call = this.GenerateAST("f(xs...)").As<FuncCallExprNode>();
            Assert.That(call.Arguments!.Expressions.Last().As<IdNode>().Identifier, Is.EqualTo("..."));

            LambdaFuncExprNode lambda = this.GenerateAST("func(x int, y ...string) { return }").As<LambdaFuncExprNode>();
            Assert.That(lambda.Parameters, Has.Exactly(2).Items);
            Assert.That(lambda.ParametersNode!.IsVariadic, Is.True);
            Assert.That(lambda.Definition.Children.Single(), Is.InstanceOf<JumpStatNode>());
        }

        [Test]
        public void MarkerOperatorExpressionTests()
        {
            Assert.That(this.GenerateAST("<-ch").As<FuncCallExprNode>().Identifier, Is.EqualTo("__linvast_recv"));
            Assert.That(this.GenerateAST("mask &^ bit").As<FuncCallExprNode>().Identifier, Is.EqualTo("__linvast_bit_clear"));
        }


        protected override ASTNode GenerateAST(string src)
            => new GoASTBuilder().BuildFromSource(src, p => p.expression());
    }
}
