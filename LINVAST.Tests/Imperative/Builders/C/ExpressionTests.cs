using System;
using LINVAST.Imperative.Builders.C;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.C
{
    internal sealed class ExpressionTests : ExpressionTestsBase
    {
        [Test]
        public void LiteralExpressionTest()
        {
            this.AssertExpressionValue("3", 3);
            this.AssertExpressionValue("2.3", 2.3);
            this.AssertExpressionValue("'a'", 'a');
            this.AssertExpressionValue("\"abc\"", "abc");

            this.AssertLiteralSuffix("1u", "U", 1U, typeof(uint));
            this.AssertLiteralSuffix("1U", "U", 1U, typeof(uint));
            this.AssertLiteralSuffix("1l", "L", 1L, typeof(long));
            this.AssertLiteralSuffix("1L", "L", 1L, typeof(long));
            this.AssertLiteralSuffix("1ll", "LL", 1L, typeof(long));
            this.AssertLiteralSuffix("1ul", "UL", 1UL, typeof(ulong));
            this.AssertLiteralSuffix("1ull", "ULL", 1UL, typeof(ulong));
            this.AssertLiteralSuffix("1Ul", "UL", 1UL, typeof(ulong));
            this.AssertLiteralSuffix("1ULL", "ULL", 1UL, typeof(ulong));
            this.AssertLiteralSuffix("1LL", "LL", 1L, typeof(long));
            this.AssertLiteralSuffix("1ll", "LL", 1L, typeof(long));

            this.AssertLiteralSuffix("01U", "U", 1U, typeof(uint));
            this.AssertLiteralSuffix("077u", "U", Convert.ToUInt32("77", fromBase: 8), typeof(uint));
            this.AssertLiteralSuffix("037777777777u", "U", Convert.ToUInt32("37777777777", fromBase: 8), typeof(uint));
            this.AssertLiteralSuffix("01L", "L", 1L, typeof(long));
            this.AssertLiteralSuffix("07L", "L", 7L, typeof(long));
            this.AssertLiteralSuffix("012345671234567l", "L", Convert.ToInt64("12345671234567", fromBase: 8), typeof(long));
            this.AssertLiteralSuffix("012345671234567ll", "LL", Convert.ToInt64("12345671234567", fromBase: 8), typeof(long));
            this.AssertLiteralSuffix("012345671234567LL", "LL", Convert.ToInt64("12345671234567", fromBase: 8), typeof(long));
            this.AssertLiteralSuffix("01ul", "UL", 1UL, typeof(ulong));
            this.AssertLiteralSuffix("01Ul", "UL", 1UL, typeof(ulong));
            this.AssertLiteralSuffix("077UL", "UL", Convert.ToUInt64("77", fromBase: 8), typeof(ulong));
            this.AssertLiteralSuffix("012345671234567uL", "UL", Convert.ToUInt64("12345671234567", fromBase: 8), typeof(ulong));
            this.AssertLiteralSuffix("01ull", "ULL", 1UL, typeof(ulong));
            this.AssertLiteralSuffix("01Ull", "ULL", 1UL, typeof(ulong));
            this.AssertLiteralSuffix("077ULL", "ULL", Convert.ToUInt64("77", fromBase: 8), typeof(ulong));
            this.AssertLiteralSuffix("012345671234567uLL", "ULL", Convert.ToUInt64("12345671234567", fromBase: 8), typeof(ulong));

            this.AssertLiteralSuffix("0x1u", "U", 0x1U, typeof(uint));
            this.AssertLiteralSuffix("0xAFu", "U", 0xAF, typeof(uint));
            this.AssertLiteralSuffix("0xFFFFFFFFu", "U", 0xFFFFFFFF, typeof(uint));
            this.AssertLiteralSuffix("0xFFFFFFFFFFl", "L", 0xFFFFFFFFFFL, typeof(long));
            this.AssertLiteralSuffix("0xAFl", "L", 0xAFL, typeof(long));
            this.AssertLiteralSuffix("0xAFL", "L", 0xAFL, typeof(long));
            this.AssertLiteralSuffix("0xFll", "LL", 0xFUL, typeof(long));
            this.AssertLiteralSuffix("0xFLL", "LL", 0xFUL, typeof(long));
            this.AssertLiteralSuffix("0xFFFFFFFFFFul", "UL", 0xFFFFFFFFFFUL, typeof(ulong));
            this.AssertLiteralSuffix("0xFFFFFFFFFFuL", "UL", 0xFFFFFFFFFFUL, typeof(ulong));
            this.AssertLiteralSuffix("0xFFFFFFFFFFUl", "UL", 0xFFFFFFFFFFUL, typeof(ulong));
            this.AssertLiteralSuffix("0xFULL", "ULL", 0xFUL, typeof(ulong));
            this.AssertLiteralSuffix("0xFuLL", "ULL", 0xFUL, typeof(ulong));
            this.AssertLiteralSuffix("0xFUll", "ULL", 0xFUL, typeof(ulong));
            this.AssertLiteralSuffix("0xFull", "ULL", 0xFUL, typeof(ulong));
        }

        [Test]
        public void ArithmeticExpressionTest()
        {
            this.AssertExpressionValue("1 << (1 + 1 * 2) >> 3", 1);
            this.AssertExpressionValue("2.3 + 4.0 / 2.0", 4.3);
            this.AssertExpressionValue("3.3 + (4.1 - 1.1) * 2.0", 9.3);
            this.AssertExpressionValue("1 << (1 + 1 * 2) >> 3", 1);
            this.AssertExpressionValue("2.3 + 4 / 2", 4.3);
            this.AssertExpressionValue("3.3 + (4.1 - 1.1) * 2", 9.3);
        }

        [Test]
        public void ArithmeticBitwiseExpressionTest()
        {
            this.AssertExpressionValue("1 | ~0", ~0);
            this.AssertExpressionValue("1 | ~1", ~0);
            this.AssertExpressionValue("1 & ~0", 1 & ~0);
            this.AssertExpressionValue("(1 << 1) & ~0", (1 << 1) & ~0);
            this.AssertExpressionValue("(1 << 10 >> 2) ^ (~0 << 10)", (1 << 10 >> 2) ^ (~0 << 10));
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
            Assert.That(this.AssertExpression("y++"), Is.InstanceOf<IncExprNode>());
            Assert.That(this.AssertExpression("y--"), Is.InstanceOf<DecExprNode>());
            Assert.That(this.AssertExpression("++y"), Is.InstanceOf<UnaryExprNode>());
            Assert.That(this.AssertExpression("--y"), Is.InstanceOf<UnaryExprNode>());
            this.AssertExpressionValue("++1", 2);
            this.AssertExpressionValue("--1", 0);
            this.AssertExpressionValue("-1", -1);
            this.AssertExpressionValue("~0", ~0);
            this.AssertExpressionValue("~(~0)", 0);
            this.AssertExpressionValue("!0", true);
            this.AssertExpressionValue("!(1 > 2)", true);
            this.AssertExpressionValue("!1", false);
            this.AssertExpressionValue("!(1 != 0)", false);
            this.AssertExpressionValue("(!1) != (!1)", false);
            this.AssertExpressionValue("(!1) != 0", false);
        }

        [Test]
        public void NullTests()
        {
            this.AssertNullExpression("null");
            this.AssertNullExpression("NULL");

            this.AssertEvaluationException("4 + NULL");
            this.AssertEvaluationException("NULL + NULL");
            this.AssertEvaluationException("4 * 2 - NULL");
            this.AssertEvaluationException("3 | NULL");
            this.AssertEvaluationException("NULL | 1");
            this.AssertEvaluationException("NULL >> 1");
            this.AssertEvaluationException("1 >> NULL");
            this.AssertEvaluationException("2 ^ NULL");
            this.AssertEvaluationException("NULL ^ 2");
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


        protected override ASTNode GenerateAST(string src)
            => new CASTBuilder().BuildFromSource(src, p => p.assignmentExpression());
    }
}
