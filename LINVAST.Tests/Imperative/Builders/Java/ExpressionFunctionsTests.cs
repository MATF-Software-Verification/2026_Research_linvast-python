using System;
using System.Linq;
using LINVAST.Imperative.Builders.Java;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Visitors;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Java
{
    internal sealed class ExpressionFunctionsTests : ExpressionTestsBase
    {
        [Test]
        public void TestLiteralExpressions()
        {
            this.AssertExpressionValue("3", 3);
            this.AssertExpressionValue("2.3", 2.3);
            this.AssertExpressionValue("'a'", 'a');

            this.AssertLiteralSuffix("1", null, 1, typeof(int));
            this.AssertLiteralSuffix("1l", "L", 1L, typeof(long));
            this.AssertLiteralSuffix("1L", "L", 1L, typeof(long));

            this.AssertLiteralSuffix("01", null, 1, typeof(int));
            this.AssertLiteralSuffix("077", null, Convert.ToInt32("77", fromBase: 8), typeof(int));
            this.AssertLiteralSuffix("037777777777", null, Convert.ToInt32("37777777777", fromBase: 8), typeof(int));
            this.AssertLiteralSuffix("01L", "L", 1L, typeof(long));
            this.AssertLiteralSuffix("07L", "L", 7L, typeof(long));
            this.AssertLiteralSuffix("012345671234567l", "L", Convert.ToInt64("12345671234567", fromBase: 8), typeof(long));

            this.AssertLiteralSuffix("0x1", null, 0x1, typeof(int));
            this.AssertLiteralSuffix("0xAF", null, 0xAF, typeof(int));
            this.AssertLiteralSuffix("0xFFFFFF", null, 0xFFFFFF, typeof(int));
            this.AssertLiteralSuffix("0xFFFFFFFFFFl", "L", 0xFFFFFFFFFFL, typeof(long));
            this.AssertLiteralSuffix("0xAFl", "L", 0xAFL, typeof(long));
            this.AssertLiteralSuffix("0xAFL", "L", 0xAFL, typeof(long));
            this.AssertLiteralSuffix("0b1010", null, 10, typeof(int));
            this.AssertLiteralSuffix("0b1010_0101L", "L", 165L, typeof(long));

            this.AssertNullExpression("null");
            this.AssertEvaluationException("4 + null");
            this.AssertEvaluationException("4 * 2 - null");
            this.AssertEvaluationException("null >> 1");
            this.AssertEvaluationException("1 >> null");

            this.AssertExpressionValue("false", false);
            this.AssertExpressionValue("true", true);
        }

        [Test]
        public void TestPrimary()
        {
            this.AssertExpressionValue("3", 3);
            this.AssertExpressionValue("2.3", 2.3);

            this.AssertFunctionCallExpression("this", "this");
            this.AssertFunctionCallExpression("super", "super");

            this.AssertExpression("Point.class");
            this.AssertExpression("Point<int>.class");
        }

        [Test]
        public void TestParExpression()
        {
            this.AssertExpressionValue("(3)", 3);
            this.AssertExpressionValue("((2.3))", 2.3);
            this.AssertExpressionValue("('a')", 'a');
        }

        [Test]
        public void TestLambdaExpression()
        {
            string src1 = "x -> x*x";
            LambdaFuncExprNode ast1 = this.GenerateAST(src1).As<LambdaFuncExprNode>();
            Assert.That(ast1.Definition.Children[0], Is.EqualTo(this.GenerateAST("x*x").As<ArithmExprNode>()));
            Assert.That(ast1.Parameters, Has.Exactly(1).Items);

            string src2 = "(a) -> a+9";
            LambdaFuncExprNode ast2 = this.GenerateAST(src2).As<LambdaFuncExprNode>();
            Assert.That(ast2.Definition.Children[0], Is.EqualTo(this.GenerateAST("a+9").As<ArithmExprNode>()));
            Assert.That(ast2.Parameters, Has.Exactly(1).Items);

            string src3 = "(x, y) -> (x*y)";
            LambdaFuncExprNode ast3 = this.GenerateAST(src3).As<LambdaFuncExprNode>();
            Assert.That(ast3.Definition.Children[0], Is.EqualTo(this.GenerateAST("x*y").As<ArithmExprNode>()));
            Assert.That(ast3.Parameters, Has.Exactly(2).Items);

            string src4 = "(int x) -> x-2";
            LambdaFuncExprNode ast4 = this.GenerateAST(src4).As<LambdaFuncExprNode>();
            Assert.That(ast4.Definition.Children[0], Is.EqualTo(this.GenerateAST("x-2").As<ArithmExprNode>()));
            Assert.That(ast4.Parameters, Has.Exactly(1).Items);

            string src5 = "(bool x, bool y) -> x&&y";
            LambdaFuncExprNode ast5 = this.GenerateAST(src5).As<LambdaFuncExprNode>();
            Assert.That(ast5.Definition.Children[0], Is.EqualTo(this.GenerateAST("x&&y").As<LogicExprNode>()));
            Assert.That(ast5.Parameters, Has.Exactly(2).Items);

            LambdaFuncExprNode block = this.GenerateAST("x -> { return x; }").As<LambdaFuncExprNode>();
            Assert.That(block.Parameters, Has.Exactly(1).Items);
            Assert.That(block.Definition.Children.Single(), Is.InstanceOf<JumpStatNode>());
        }

        [Test]
        public void TestConditionalExpr()
        {
            this.AssertExpressionValue("true ? 1 : 2", 1);
            this.AssertExpressionValue("false ? 1 : 2", 2);
            this.AssertExpressionValue("2 == 3 ? 4 : 5", 5);
            this.AssertExpressionValue("2 != 3 ? 4 : 5", 4);
            this.AssertExpressionValue("2 < 3 ? 'a' : 'b'", 'a');
            this.AssertExpressionValue("2 > 3 ? 'a' : 'b'", 'b');
        }

        [Test]
        public void TestCreator()
        {
            this.AssertFunctionCallExpression("new Integer(3)", "Integer", 3);
            this.AssertFunctionCallExpression("new <int>Point('a')", "Point", 'a');
            this.AssertFunctionCallExpression("new Point<int>(3)", "Point<int>", 3);
            this.AssertFunctionCallExpression("new <string>Point<double>(3.8)", "Point<double>", 3.8);

            ArrAccessExprNode array = this.AssertExpression("new int[2][3]").As<ArrAccessExprNode>();
            Assert.That(array.IndexExpression, Is.InstanceOf<ExprListNode>());
            Assert.That(array.IndexExpression.As<ExprListNode>().Expressions.Select(ConstantExpressionEvaluator.Evaluate),
                Is.EqualTo(new object[] { 2, 3 }));
        }

        [Test]
        public void TestUnaryExpr()
        {
            Assert.That(this.AssertExpression("y++"), Is.InstanceOf<IncExprNode>());
            Assert.That(this.AssertExpression("y--"), Is.InstanceOf<DecExprNode>());
            Assert.That(this.AssertExpression("++y"), Is.InstanceOf<IncExprNode>());
            Assert.That(this.AssertExpression("--y"), Is.InstanceOf<DecExprNode>());
            Assert.That(this.AssertExpression("~y"), Is.InstanceOf<UnaryExprNode>());
            Assert.That(this.AssertExpression("!y"), Is.InstanceOf<UnaryExprNode>());

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
        public void TestArthmExpr()
        {
            this.AssertExpressionValue("1 << (1 + 1 * 2) >> 3", 1);
            this.AssertExpressionValue("2.3 + 4.0 / 2.0", 4.3);
            this.AssertExpressionValue("3.3 + (4.1 - 1.1) * 2.0", 9.3);
            this.AssertExpressionValue("1 << (1 + 1 * 2) >> 3", 1);
            this.AssertExpressionValue("2.3 + 4 / 2", 4.3);
            this.AssertExpressionValue("3.3 + (4.1 - 1.1) * 2", 9.3);
            this.AssertExpressionValue("1 & 3", 1 & 3);
            this.AssertExpressionValue("1 | 2", 1 | 2);
            this.AssertExpressionValue("3 ^ 1", 3 ^ 1);
            this.AssertExpressionValue("8 >>> 1", 8 >> 1);
            this.AssertExpressionValue("(int)(1 + 2)", 3);
        }

        [Test]
        public void TestLogicExpr()
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
        public void TestRelationalExpr()
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
        public void TestMethodCall()
        {
            this.AssertFunctionCallExpression("f()", "f");
            this.AssertFunctionCallExpression("g(3)", "g", 3);
            this.AssertFunctionCallExpression("g(3, 2)", "g", 3, 2);
            this.AssertFunctionCallExpression("g(3, 'a')", "g", 3, 'a');
            this.AssertFunctionCallExpression("g(3.1 + 1, 2 * 3)", "g", 3.1 + 1, 2 * 3);
            this.AssertFunctionCallExpression("g(((1 << 2) + 4) >> 3)", "g", ((1 << 2) + 4) >> 3);
            this.AssertFunctionCallExpression("g(1.1 > 1.0 && 1.0 > 1.02)", "g", false);
            this.AssertFunctionCallExpression("h(1.01 > 1.0 || 1.0 > 1.02)", "h", true);
            this.AssertFunctionCallExpression("this()", "this");
            this.AssertFunctionCallExpression("this(3)", "this", 3);
            this.AssertFunctionCallExpression("super()", "super");
            this.AssertFunctionCallExpression("super(3)", "super", 3);
            this.AssertFunctionCallExpression("obj.f(3)", "obj.f", 3);
        }

        [Test]
        public void TestExprRest()
        {
            this.AssertFunctionCallExpression("Point::new", "Point");
            this.AssertFunctionCallExpression("Point<int>::new", "Point<int>");
            this.AssertFunctionCallExpression("super.f(3)", "super.f", 3);
            this.AssertFunctionCallExpression("super.g('a')", "super.g", 'a');
        }

        [Test]
        public void TestMemberAndArrayAccess()
        {
            Assert.That(this.AssertExpression("obj.field").As<IdNode>().Identifier, Is.EqualTo("obj.field"));
            Assert.That(this.AssertExpression("obj.inner.value").As<IdNode>().Identifier, Is.EqualTo("obj.inner.value"));

            ArrAccessExprNode access = this.AssertExpression("items[3]").As<ArrAccessExprNode>();
            Assert.That(access.Array.As<IdNode>().Identifier, Is.EqualTo("items"));
            Assert.That(ConstantExpressionEvaluator.Evaluate(access.IndexExpression), Is.EqualTo(3));

            ArrAccessExprNode nested = this.AssertExpression("matrix[1][2]").As<ArrAccessExprNode>();
            Assert.That(ConstantExpressionEvaluator.Evaluate(nested.IndexExpression), Is.EqualTo(2));
            ArrAccessExprNode inner = nested.Array.As<ArrAccessExprNode>();
            Assert.That(inner.Array.As<IdNode>().Identifier, Is.EqualTo("matrix"));
            Assert.That(ConstantExpressionEvaluator.Evaluate(inner.IndexExpression), Is.EqualTo(1));
        }

        [Test]
        public void TestAssignmentOperatorExpressions()
        {
            AssignExprNode mod = this.AssertExpression("x %= 2").As<AssignExprNode>();
            Assert.That(mod.Operator.Symbol, Is.EqualTo("%="));
            Assert.That(ConstantExpressionEvaluator.Evaluate(mod.RightOperand), Is.EqualTo(2));

            AssignExprNode shift = this.AssertExpression("x <<= 1").As<AssignExprNode>();
            Assert.That(shift.Operator.Symbol, Is.EqualTo("<<="));
            Assert.That(ConstantExpressionEvaluator.Evaluate(shift.RightOperand), Is.EqualTo(1));
        }

        protected override ASTNode GenerateAST(string src)
            => new JavaASTBuilder().BuildFromSource(src, p => p.expression());
    }
}
