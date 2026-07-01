using System.Linq;
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

        [Test]
        public void LiteralExprBuildsExprNode()
        {
            var literal = this.builder.BuildFromSource("42", parser => parser.literal_expr()).As<LitExprNode>();

            Assert.That(literal.Value, Is.EqualTo(42L));
        }

        [Test]
        public void BareYieldBuildsYieldExprNode()
        {
            var statement = this.ParseStatement("yield\n");
            var yieldExpr = statement.As<ExprStatNode>().Expression.As<YieldExprNode>();

            Assert.That(yieldExpr.Value, Is.Null);
            Assert.That(yieldExpr.IsDelegating, Is.False);
        }

        [Test]
        public void YieldValueBuildsYieldExprNode()
        {
            var statement = this.ParseStatement("yield 1\n");
            var yieldExpr = statement.As<ExprStatNode>().Expression.As<YieldExprNode>();

            Assert.That(yieldExpr.Value, Is.TypeOf<LitExprNode>());
            Assert.That(yieldExpr.IsDelegating, Is.False);
        }

        [Test]
        public void YieldFromBuildsDelegatingYieldExprNode()
        {
            var statement = this.ParseStatement("yield from items\n");
            var yieldExpr = statement.As<ExprStatNode>().Expression.As<YieldExprNode>();

            Assert.That(yieldExpr.Value, Is.TypeOf<IdNode>());
            Assert.That(yieldExpr.IsDelegating, Is.True);
        }

        [Test]
        public void ChainedComparisonDuplicatesMiddleOperandIntoSeparateNodes()
        {
            var expr = this.ParseExpression("a < f() < b").As<LogicExprNode>();
            var left = expr.LeftOperand.As<RelExprNode>();
            var right = expr.RightOperand.As<RelExprNode>();

            // `a < f() < b` desugars structurally to `a < f() and f() < b`. Each
            // comparison must own a distinct, fully-parented `f()` subtree:
            // sharing one instance would break the AST parent invariant because
            // ASTNode wires `child.Parent = this` in every constructor.
            Assert.That(left.RightOperand, Is.TypeOf<FuncCallExprNode>());
            Assert.That(right.LeftOperand, Is.TypeOf<FuncCallExprNode>());
            Assert.That(left.RightOperand, Is.EqualTo(right.LeftOperand));
            Assert.That(left.RightOperand, Is.Not.SameAs(right.LeftOperand));
            Assert.That(left.RightOperand.Parent, Is.SameAs(left));
            Assert.That(right.LeftOperand.Parent, Is.SameAs(right));
        }

        [Test]
        public void ListComprehensionBuildsListCall()
        {
            var call = this.ParseExpression("[x * 2 for x in items]").As<FuncCallExprNode>();
            ExprNode[] args = call.Arguments!.Expressions.ToArray();

            Assert.That(call.Identifier, Is.EqualTo("list"));
            Assert.That(args[0], Is.TypeOf<ArithmExprNode>());
            Assert.That(args[1].As<FuncCallExprNode>().Identifier, Is.EqualTo("for"));
        }

        [Test]
        public void GeneratorExpressionInParensBuildsGeneratorCall()
        {
            var call = this.ParseExpression("(x for x in items)").As<FuncCallExprNode>();

            Assert.That(call.Identifier, Is.EqualTo("generator"));
            Assert.That(call.Arguments!.Expressions.ElementAt(1).As<FuncCallExprNode>().Identifier, Is.EqualTo("for"));
        }

        [Test]
        public void SetComprehensionBuildsSetCall()
        {
            var call = this.ParseExpression("{x for x in items}").As<FuncCallExprNode>();

            Assert.That(call.Identifier, Is.EqualTo("set"));
        }

        [Test]
        public void DictComprehensionBuildsDictCall()
        {
            var call = this.ParseExpression("{k: v for k, v in pairs}").As<FuncCallExprNode>();

            Assert.That(call.Identifier, Is.EqualTo("dict"));
            Assert.That(call.Arguments!.Expressions.First(), Is.TypeOf<DictEntryNode>());
        }

        [Test]
        public void DictComprehensionAssignmentBuildsDictCallWithEntryAndForClause()
        {
            var stat = this.ParseStatement("squares = {x: x ** 2 for x in range(1, 6)}\n");
            var decl = stat.As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var call = decl.Initializer!.As<FuncCallExprNode>();
            ExprNode[] args = call.Arguments!.Expressions.ToArray();

            Assert.That(decl.Identifier, Is.EqualTo("squares"));
            Assert.That(call.Identifier, Is.EqualTo("dict"));

            var entry = args[0].As<DictEntryNode>();
            Assert.That(entry.Key.Identifier, Is.EqualTo("x"));
            Assert.That(entry.Value, Is.TypeOf<ArithmExprNode>());

            var forClause = args[1].As<FuncCallExprNode>();
            ExprNode[] clauseArgs = forClause.Arguments!.Expressions.ToArray();
            Assert.That(forClause.Identifier, Is.EqualTo("for"));
            Assert.That(clauseArgs[0].As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(clauseArgs[1].As<FuncCallExprNode>().Identifier, Is.EqualTo("range"));
        }

        [Test]
        public void GeneratorArgumentWithFilterBuildsClauses()
        {
            var call = this.ParseExpression("sum(x for x in xs if x > 0)").As<FuncCallExprNode>();
            var generator = call.Arguments!.Expressions.Single().As<FuncCallExprNode>();
            ExprNode[] clauses = generator.Arguments!.Expressions.Skip(1).ToArray();

            Assert.That(call.Identifier, Is.EqualTo("sum"));
            Assert.That(generator.Identifier, Is.EqualTo("generator"));
            Assert.That(clauses[0].As<FuncCallExprNode>().Identifier, Is.EqualTo("for"));
            Assert.That(clauses[1].As<FuncCallExprNode>().Identifier, Is.EqualTo("if"));
        }

        [Test]
        public void FullSliceBuildsSliceCall()
        {
            var access = this.ParseExpression("a[1:10:2]").As<ArrAccessExprNode>();
            var slice = access.IndexExpression.As<FuncCallExprNode>();
            ExprNode[] parts = slice.Arguments!.Expressions.ToArray();

            Assert.That(slice.Identifier, Is.EqualTo("slice"));
            Assert.That(parts[0].As<LitExprNode>().Value, Is.EqualTo(1L));
            Assert.That(parts[1].As<LitExprNode>().Value, Is.EqualTo(10L));
            Assert.That(parts[2].As<LitExprNode>().Value, Is.EqualTo(2L));
        }

        [Test]
        public void SliceWithOmittedBoundsUsesNullLiterals()
        {
            var access = this.ParseExpression("a[:5]").As<ArrAccessExprNode>();
            var slice = access.IndexExpression.As<FuncCallExprNode>();
            ExprNode[] parts = slice.Arguments!.Expressions.ToArray();

            Assert.That(parts[0], Is.TypeOf<NullLitExprNode>());
            Assert.That(parts[1].As<LitExprNode>().Value, Is.EqualTo(5L));
            Assert.That(parts[2], Is.TypeOf<NullLitExprNode>());
        }

        [Test]
        public void AwaitExpressionBuildsAwaitCall()
        {
            var call = this.ParseExpression("await f()").As<FuncCallExprNode>();

            Assert.That(call.Identifier, Is.EqualTo("await"));
            Assert.That(call.Arguments!.Expressions.Single(), Is.TypeOf<FuncCallExprNode>());
        }

        [Test]
        public void EllipsisBuildsEllipsisLiteral()
        {
            var ellipsis = this.ParseExpression("...").As<EllipsisLitExprNode>();

            Assert.That(ellipsis.GetText(), Is.EqualTo("..."));
        }

        [Test]
        public void FStringSimpleFieldBuildsFormatCall()
        {
            var call = this.ParseExpression("f\"{x}\"").As<FuncCallExprNode>();

            Assert.That(call.Identifier, Is.EqualTo("format"));
            Assert.That(call.Arguments!.Expressions.Single().As<IdNode>().Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void FStringSplitsLiteralAndFieldParts()
        {
            var call = this.ParseExpression("f\"a{x}b\"").As<FuncCallExprNode>();
            ExprNode[] parts = call.Arguments!.Expressions.ToArray();

            Assert.That(parts[0].As<LitExprNode>().Value, Is.EqualTo("a"));
            Assert.That(parts[1], Is.TypeOf<IdNode>());
            Assert.That(parts[2].As<LitExprNode>().Value, Is.EqualTo("b"));
        }

        [Test]
        public void FStringEscapedBracesAreLiteralText()
        {
            var call = this.ParseExpression("f\"{{x}}\"").As<FuncCallExprNode>();

            Assert.That(call.Arguments!.Expressions.Single().As<LitExprNode>().Value, Is.EqualTo("{x}"));
        }

        [Test]
        public void FStringFieldExpressionIsParsed()
        {
            var call = this.ParseExpression("f\"{a + b}\"").As<FuncCallExprNode>();

            Assert.That(call.Arguments!.Expressions.Single(), Is.TypeOf<ArithmExprNode>());
        }

        [Test]
        public void FStringComparisonOperatorIsNotTreatedAsConversion()
        {
            var call = this.ParseExpression("f\"{a != b}\"").As<FuncCallExprNode>();

            Assert.That(call.Arguments!.Expressions.Single(), Is.TypeOf<RelExprNode>());
        }

        [Test]
        public void FStringConversionBuildsFormatField()
        {
            var call = this.ParseExpression("f\"{x!r}\"").As<FuncCallExprNode>();
            var field = call.Arguments!.Expressions.Single().As<FuncCallExprNode>();
            ExprNode[] args = field.Arguments!.Expressions.ToArray();

            Assert.That(field.Identifier, Is.EqualTo("format_field"));
            Assert.That(args[0].As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(args[1].As<LitExprNode>().Value, Is.EqualTo("!r"));
            Assert.That(args[2], Is.TypeOf<NullLitExprNode>());
        }

        [Test]
        public void FStringFormatSpecBuildsFormatField()
        {
            var call = this.ParseExpression("f\"{x:.2f}\"").As<FuncCallExprNode>();
            var field = call.Arguments!.Expressions.Single().As<FuncCallExprNode>();
            ExprNode[] args = field.Arguments!.Expressions.ToArray();

            Assert.That(args[1], Is.TypeOf<NullLitExprNode>());
            Assert.That(args[2].As<LitExprNode>().Value, Is.EqualTo(".2f"));
        }

        [Test]
        public void FStringNestedSpecFieldIsParsed()
        {
            var call = this.ParseExpression("f\"{x:{w}}\"").As<FuncCallExprNode>();
            var field = call.Arguments!.Expressions.Single().As<FuncCallExprNode>();
            var spec = field.Arguments!.Expressions.ElementAt(2).As<FuncCallExprNode>();

            Assert.That(spec.Identifier, Is.EqualTo("format"));
            Assert.That(spec.Arguments!.Expressions.Single().As<IdNode>().Identifier, Is.EqualTo("w"));
        }

        [Test]
        public void FStringDebugEqualsEmitsSourceTextAndReprField()
        {
            var call = this.ParseExpression("f\"{x=}\"").As<FuncCallExprNode>();
            ExprNode[] parts = call.Arguments!.Expressions.ToArray();
            var field = parts[1].As<FuncCallExprNode>();

            Assert.That(parts[0].As<LitExprNode>().Value, Is.EqualTo("x="));
            Assert.That(field.Identifier, Is.EqualTo("format_field"));
            Assert.That(field.Arguments!.Expressions.ElementAt(1).As<LitExprNode>().Value, Is.EqualTo("!r"));
        }

        [Test]
        public void FStringImplicitlyConcatenatesWithPlainString()
        {
            var call = this.ParseExpression("\"a\" f\"{x}\"").As<FuncCallExprNode>();
            ExprNode[] parts = call.Arguments!.Expressions.ToArray();

            Assert.That(parts[0].As<LitExprNode>().Value, Is.EqualTo("a"));
            Assert.That(parts[1].As<IdNode>().Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void FStringFieldCanBeAStringLiteral()
        {
            var call = this.ParseExpression("f\"{'test'}\"").As<FuncCallExprNode>();

            Assert.That(call.Identifier, Is.EqualTo("format"));
            Assert.That(call.Arguments!.Expressions.Single().As<LitExprNode>().Value, Is.EqualTo("test"));
        }

        [Test]
        public void FStringFieldCanBeAStringConcatenationExpression()
        {
            var call = this.ParseExpression("f\"{'Hello, ' + 'world'}\"").As<FuncCallExprNode>();
            var arithm = call.Arguments!.Expressions.Single().As<ArithmExprNode>();

            Assert.That(arithm.LeftOperand.As<LitExprNode>().Value, Is.EqualTo("Hello, "));
            Assert.That(arithm.RightOperand.As<LitExprNode>().Value, Is.EqualTo("world"));
        }

        [Test]
        public void FStringFieldCanBeAnArithmeticExpression()
        {
            var call = this.ParseExpression("f\"{3 + 2}\"").As<FuncCallExprNode>();

            Assert.That(call.Arguments!.Expressions.Single(), Is.TypeOf<ArithmExprNode>());
        }

        [Test]
        public void FStringFieldStringLiteralContainingClosingBraceIsNotTruncated()
        {
            // The '}' lives inside the embedded literal, so it must not be treated
            // as the end of the replacement field.
            var call = this.ParseExpression("f\"{'}'}\"").As<FuncCallExprNode>();

            Assert.That(call.Arguments!.Expressions.Single().As<LitExprNode>().Value, Is.EqualTo("}"));
        }

        [Test]
        public void FStringFieldStringLiteralContainingColonIsNotTreatedAsFormatSpec()
        {
            // The ':' is part of the literal value, not a format-spec separator.
            var call = this.ParseExpression("f\"{'a:b'}\"").As<FuncCallExprNode>();

            Assert.That(call.Arguments!.Expressions.Single().As<LitExprNode>().Value, Is.EqualTo("a:b"));
        }

        [Test]
        public void FStringFieldStringLiteralContainingBangIsNotTreatedAsConversion()
        {
            // The '!' is part of the literal value, not a conversion marker.
            var call = this.ParseExpression("f\"{'a!b'}\"").As<FuncCallExprNode>();

            Assert.That(call.Arguments!.Expressions.Single().As<LitExprNode>().Value, Is.EqualTo("a!b"));
        }

        [Test]
        public void FStringStringFieldStillHonoursTrailingFormatSpec()
        {
            // A literal that contains a ':' followed by a real format spec: only the
            // last ':' outside the quotes introduces the spec.
            var call = this.ParseExpression("f\"{'a:b':>10}\"").As<FuncCallExprNode>();
            var field = call.Arguments!.Expressions.Single().As<FuncCallExprNode>();
            ExprNode[] args = field.Arguments!.Expressions.ToArray();

            Assert.That(field.Identifier, Is.EqualTo("format_field"));
            Assert.That(args[0].As<LitExprNode>().Value, Is.EqualTo("a:b"));
            Assert.That(args[2].As<LitExprNode>().Value, Is.EqualTo(">10"));
        }

        [Test]
        public void ChainedOrBuildsDistinctOperatorInstancesWithValidParents()
        {
            var expr = this.ParseExpression("a or b or c").As<LogicExprNode>();
            var left = expr.LeftOperand.As<LogicExprNode>();

            Assert.That(expr.RightOperand.As<IdNode>().Identifier, Is.EqualTo("c"));
            Assert.That(left.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("a"));
            Assert.That(left.RightOperand.As<IdNode>().Identifier, Is.EqualTo("b"));

            // Each binary logic expression must own its own operator instance;
            // sharing one node would re-parent it and break the AST invariant.
            Assert.That(expr.Operator, Is.Not.SameAs(left.Operator));
            Assert.That(expr.Operator.Parent, Is.SameAs(expr));
            Assert.That(left.Operator.Parent, Is.SameAs(left));
        }

        [Test]
        public void ChainedAndBuildsDistinctOperatorInstancesWithValidParents()
        {
            var expr = this.ParseExpression("a and b and c").As<LogicExprNode>();
            var left = expr.LeftOperand.As<LogicExprNode>();

            Assert.That(expr.Operator.Symbol, Is.EqualTo("and"));
            Assert.That(left.Operator.Symbol, Is.EqualTo("and"));
            Assert.That(expr.Operator, Is.Not.SameAs(left.Operator));
            Assert.That(expr.Operator.Parent, Is.SameAs(expr));
            Assert.That(left.Operator.Parent, Is.SameAs(left));
        }

        [Test]
        public void MixedBooleanExpressionKeepsOperatorParentsValid()
        {
            // `and` binds tighter than `or`, so this parses as `a or (b and c)`.
            var expr = this.ParseExpression("a or b and c").As<LogicExprNode>();
            var right = expr.RightOperand.As<LogicExprNode>();

            Assert.That(expr.Operator.Symbol, Is.EqualTo("or"));
            Assert.That(right.Operator.Symbol, Is.EqualTo("and"));
            Assert.That(expr.Operator, Is.Not.SameAs(right.Operator));
            Assert.That(expr.Operator.Parent, Is.SameAs(expr));
            Assert.That(right.Operator.Parent, Is.SameAs(right));
        }

        [Test]
        public void StringHexEscapeIsDecoded()
            => this.AssertExpressionValue("\"\\x41\"", "A");

        [Test]
        public void StringShortUnicodeEscapeIsDecoded()
            => this.AssertExpressionValue("\"\\u0041\"", "A");

        [Test]
        public void StringLongUnicodeEscapeIsDecoded()
            => this.AssertExpressionValue("\"\\U00000041\"", "A");

        [Test]
        public void StringOctalEscapeIsDecoded()
            => this.AssertExpressionValue("\"\\101\"", "A");

        [Test]
        public void StringNullEscapeIsDecoded()
            => this.AssertExpressionValue("\"\\0\"", "\0");

        [Test]
        public void SimpleStringEscapesStillWork()
        {
            this.AssertExpressionValue("\"a\\nb\"", "a\nb");
            this.AssertExpressionValue("\"a\\tb\"", "a\tb");
            this.AssertExpressionValue("\"a\\\\b\"", "a\\b");
        }

        [Test]
        public void RawStringPreservesBackslashEscapes()
            => this.AssertExpressionValue("r\"\\n\"", "\\n");

        [Test]
        public void EmptyTupleAndEmptyListProduceDistinctNodes()
        {
            Assert.That(this.ParseExpression("()"), Is.TypeOf<TupleInitNode>());
            Assert.That(this.ParseExpression("[]"), Is.TypeOf<ArrInitExprNode>());
        }

        [Test]
        public void SingleElementTupleDiffersFromGroupedExpression()
        {
            Assert.That(this.ParseExpression("(x)"), Is.TypeOf<IdNode>());

            var tuple = this.ParseExpression("(x,)").As<TupleInitNode>();
            Assert.That(tuple.Expressions.Single().As<IdNode>().Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void MultiElementParenthesizedExpressionBuildsTuple()
        {
            var tuple = this.ParseExpression("(1, 2)").As<TupleInitNode>();

            Assert.That(tuple.Expressions.Count(), Is.EqualTo(2));
        }

        [Test]
        public void ReturnSingleElementTupleBuildsTuple()
        {
            var stat = this.ParseStatement("return (value,)\n").As<JumpStatNode>();

            Assert.That(stat.ReturnExpr, Is.TypeOf<TupleInitNode>());
        }

        private ExprNode ParseExpression(string source)
            => this.builder.BuildFromSource(source, parser => parser.test()).As<ExprNode>();

        private StatNode ParseStatement(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<StatNode>();

        private void AssertExpressionValue<T>(string source, T expected)
        {
            ExprNode expression = this.ParseExpression(source);
            Assert.That(ConstantExpressionEvaluator.TryEvaluateAs(expression, out T result));
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
