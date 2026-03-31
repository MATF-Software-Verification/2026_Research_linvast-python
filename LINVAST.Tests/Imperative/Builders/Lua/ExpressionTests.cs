using LINVAST.Imperative.Builders.Lua;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Lua
{
    internal sealed class ExpressionTests : ExpressionTestsBase
    {
        [Test]
        public void LiteralExpressionTest()
        {
            this.AssertExpressionValue("3", 3);
            this.AssertExpressionValue("true", true);
            this.AssertExpressionValue("false", false);
            this.AssertExpressionValue("2.3", 2.3);
            this.AssertExpressionValue("'a'", "a");
            this.AssertExpressionValue("\"abc\"", "abc");
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
            this.AssertExpressionValue("(1 << 10 >> 2) ~ (~0 << 10)", (1 << 10 >> 2) ^ (~0 << 10));
        }

        [Test]
        public void RelationalExpressionTest()
        {
            this.AssertExpressionValue("1 > 1", false);
            this.AssertExpressionValue("1 >= 1", true);
            this.AssertExpressionValue("2 > 1", true);
            this.AssertExpressionValue("3 < 1", false);
            this.AssertExpressionValue("1 ~= 1", false);
            this.AssertExpressionValue("(1 + 1) == 2", true);
            this.AssertExpressionValue("1.1 > 1.0", true);
            this.AssertExpressionValue("1.101 >= 1.1", true);
            this.AssertExpressionValue("(1 + 3 * 2) > 7", false);
            this.AssertExpressionValue("3.0 + 0.1 > 3.0", true);
            this.AssertExpressionValue("1.01 ~= 1.0", true);
            this.AssertExpressionValue("(1 << 1) == 2", true);
            this.AssertExpressionValue("(2 >> 1) == 1", true);
        }

        [Test]
        public void LogicExpressionTest()
        {
            this.AssertExpressionValue("true or true", true);
            this.AssertExpressionValue("true or false", true);
            this.AssertExpressionValue("false or false", false);
            this.AssertExpressionValue("false and false", false);
            this.AssertExpressionValue("false and true", false);
            this.AssertExpressionValue("true and false", false);
            this.AssertExpressionValue("true and true", true);
            this.AssertExpressionValue("1 > 1 and 2 < 3", false);
            this.AssertExpressionValue("1 >= 1 and 2 < 3", true);
            this.AssertExpressionValue("1 >= 1 or 3 < 3", true);
            this.AssertExpressionValue("1 > 1 or 3 >= 3", true);
            this.AssertExpressionValue("1 > 1 or 3 > 3", false);
            this.AssertExpressionValue("0.0001 > 0 and 1.1 > 1", true);
            this.AssertExpressionValue("2 > 1 and 1 ~= 2 and 2 <= 3", true);
            this.AssertExpressionValue("2 > 1 and 1 ~= 2 and 2 > 3", false);
            this.AssertExpressionValue("3 < 1 or 2 < 1 or 1 > 1", false);
            this.AssertExpressionValue("3 < 1 or 2 > 1 or 1 == 1", true);
            this.AssertExpressionValue("1 ~= 1 or 1 == 1", true);
            this.AssertExpressionValue("(1 + 1) == 2 or 2 == 2", true);
            this.AssertExpressionValue("1.1 > 1.0 and 1.0 > 1.02", false);
            this.AssertExpressionValue("1.1 > 1.0 or 1.0 > 1.02", true);
            this.AssertExpressionValue("1.101 >= 1.1 and (7 > 3.2 or 2 > 3)", true);
            this.AssertExpressionValue("(1 + 3 * 2 > 3 and 4 ~= 2.0) and 8 > 7", true);
            this.AssertExpressionValue("1 ~= 0 and 1 > 1 or 1 == 1", true);
            this.AssertExpressionValue("1 ~= 0 and (1 == 1 or 1 == 3)", true);
            this.AssertExpressionValue("1 ~= 0 and (1 ~= 1 or 1 == 3)", false);
            this.AssertExpressionValue("3 > 2 or 3 > 1 and 1 > 1", true);
            this.AssertExpressionValue("(3 > 2 or 3 > 1) and 1 > 1", false);
            this.AssertExpressionValue("(1 << 1) == 2 and (3 / 2 == 1)", true);
            this.AssertExpressionValue("(1 << 1) == 2 and (3 / 2 ~= 1)", false);
            this.AssertExpressionValue("(1 << 1) == 2 or (3 / 2 ~= 1)", true);
            this.AssertExpressionValue("(1 << 2) == 2 or (3 / 2 ~= 1)", false);
            this.AssertExpressionValue("(1 << 1) == (4 >> 1) or 1 ~= 1", true);
        }

        [Test]
        public void UnaryExpressionTests()
        {
            this.AssertExpressionValue("-1", -1);
            this.AssertExpressionValue("~0", ~0);
            this.AssertExpressionValue("~(~0)", 0);
            this.AssertExpressionValue("not true", false);
            this.AssertExpressionValue("not false", true);
            this.AssertExpressionValue("not (1 > 2)", true);
            this.AssertExpressionValue("not (1 ~= 0)", false);
            this.AssertExpressionValue("(not true) ~= (not true)", false);
            this.AssertExpressionValue("(not true) ~= false", false);
        }

        [Test]
        public void NullTests()
        {
            this.AssertNullExpression("nil");

            this.AssertEvaluationException("4 + nil");
            this.AssertEvaluationException("nil + nil");
            this.AssertEvaluationException("4 * 2 - nil");
            this.AssertEvaluationException("3 | nil");
            this.AssertEvaluationException("nil | 1");
            this.AssertEvaluationException("nil >> 1");
            this.AssertEvaluationException("1 >> nil");
            this.AssertEvaluationException("2 ~ nil");
            this.AssertEvaluationException("nil ~ 2");
        }

        [Test]
        public void FunctionCallParameterTests()
        {
            // TODO
            Assert.Inconclusive();

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
            => new LuaASTBuilder().BuildFromSource(src, p => p.exp());
    }
}
