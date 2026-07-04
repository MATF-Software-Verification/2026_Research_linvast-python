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
        public void MemberAccessAfterFunctionCallBuildsIdentifier()
        {
            var id = this.ParseExpression("f(1).x").As<IdNode>();

            Assert.That(id.Identifier, Does.EndWith(".x"));
        }

        [Test]
        public void ListComprehensionAssignmentExpandsToInitAndLoop()
        {
            var nodes = this.builder.BuildFromSource("doubles = [x * 2 for x in items]\n")
                .As<SourceNode>().Children.ToArray();
            var decl = nodes[0].As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var loop = nodes[1].As<ForStatNode>();
            var append = loop.Statement.As<BlockStatNode>().Children.Single()
                .As<ExprStatNode>().Expression.As<FuncCallExprNode>();

            Assert.That(decl.Identifier, Is.EqualTo("doubles"));
            Assert.That(decl.Initializer, Is.TypeOf<ArrInitExprNode>());
            Assert.That(loop.ForDeclaration, Is.TypeOf<VarDeclNode>());
            Assert.That(loop.Condition.As<IdNode>().Identifier, Is.EqualTo("items"));
            Assert.That(append.Identifier, Is.EqualTo("doubles.append"));
            Assert.That(append.Arguments!.Expressions.Single(), Is.TypeOf<ArithmExprNode>());
        }

        [Test]
        public void ListComprehensionWithMultipleForClausesBuildsNestedLoops()
        {
            var nodes = this.builder.BuildFromSource("products = [i * j for i in range(4) for j in range(5)]\n")
                .As<SourceNode>().Children.ToArray();
            var decl = nodes[0].As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var outerLoop = nodes[1].As<ForStatNode>();
            var innerLoop = outerLoop.Statement.As<BlockStatNode>().Children.Single().As<ForStatNode>();
            var append = innerLoop.Statement.As<BlockStatNode>().Children.Single()
                .As<ExprStatNode>().Expression.As<FuncCallExprNode>();

            Assert.That(decl.Identifier, Is.EqualTo("products"));
            Assert.That(decl.Initializer, Is.TypeOf<ArrInitExprNode>());
            Assert.That(outerLoop.ForDeclaration!.As<VarDeclNode>().Identifier, Is.EqualTo("i"));
            Assert.That(outerLoop.Condition.As<FuncCallExprNode>().Identifier, Is.EqualTo("range"));
            Assert.That(innerLoop.ForDeclaration!.As<VarDeclNode>().Identifier, Is.EqualTo("j"));
            Assert.That(innerLoop.Condition.As<FuncCallExprNode>().Identifier, Is.EqualTo("range"));
            Assert.That(append.Identifier, Is.EqualTo("products.append"));
            Assert.That(append.Arguments!.Expressions.Single(), Is.TypeOf<ArithmExprNode>());
        }

        [Test]
        public void MultilineListComprehensionAssignmentExpandsToInitAndLoopWithFilter()
        {
            var nodes = this.builder.BuildFromSource(
                "active_users = [\n" +
                "    user.name\n" +
                "    for user in users\n" +
                "    if user.active\n" +
                "]\n").As<SourceNode>().Children.ToArray();
            var decl = nodes[0].As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var loop = nodes[1].As<ForStatNode>();
            var ifStat = loop.Statement.As<BlockStatNode>().Children.Single().As<IfStatNode>();
            var append = ifStat.ThenStat.As<BlockStatNode>().Children.Single()
                .As<ExprStatNode>().Expression.As<FuncCallExprNode>();

            Assert.That(decl.Identifier, Is.EqualTo("active_users"));
            Assert.That(decl.Initializer, Is.TypeOf<ArrInitExprNode>());
            Assert.That(loop.ForDeclaration!.As<VarDeclNode>().Identifier, Is.EqualTo("user"));
            Assert.That(loop.Condition.As<IdNode>().Identifier, Is.EqualTo("users"));
            Assert.That(ifStat.Condition.As<IdNode>().Identifier, Is.EqualTo("user.active"));
            Assert.That(append.Identifier, Is.EqualTo("active_users.append"));
            Assert.That(append.Arguments!.Expressions.Single().As<IdNode>().Identifier, Is.EqualTo("user.name"));
        }

        [Test]
        public void GeneratorExpressionArgumentHoistsTempBeforeCall()
        {
            var nodes = this.builder.BuildFromSource("total = sum(x for x in xs if x > 0)\n")
                .As<SourceNode>().Children.ToArray();
            var tempDecl = nodes[0].As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var loop = nodes[1].As<ForStatNode>();
            var ifStat = loop.Statement.As<BlockStatNode>().Children.Single().As<IfStatNode>();
            var append = loop.Statement.As<BlockStatNode>().Children.Single()
                .As<IfStatNode>().ThenStat.As<BlockStatNode>().Children.Single()
                .As<ExprStatNode>().Expression.As<FuncCallExprNode>();
            var totalDecl = nodes[2].As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var sumCall = totalDecl.Initializer!.As<FuncCallExprNode>();

            Assert.That(tempDecl.Identifier, Does.StartWith("__linvast_comp_"));
            Assert.That(tempDecl.Initializer, Is.TypeOf<ArrInitExprNode>());
            Assert.That(loop.Condition.As<IdNode>().Identifier, Is.EqualTo("xs"));
            Assert.That(ifStat.Condition, Is.TypeOf<RelExprNode>());
            Assert.That(append.Identifier, Does.EndWith(".append"));
            Assert.That(totalDecl.Identifier, Is.EqualTo("total"));
            Assert.That(sumCall.Identifier, Is.EqualTo("sum"));
            Assert.That(sumCall.Arguments!.Expressions.Single().As<IdNode>().Identifier, Is.EqualTo(tempDecl.Identifier));
        }

        [Test]
        public void SetComprehensionAssignmentExpandsToInitAndLoopWithAdd()
        {
            var nodes = this.builder.BuildFromSource("seen = {x for x in items}\n")
                .As<SourceNode>().Children.ToArray();
            var decl = nodes[0].As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var loop = nodes[1].As<ForStatNode>();
            var add = loop.Statement.As<BlockStatNode>().Children.Single()
                .As<ExprStatNode>().Expression.As<FuncCallExprNode>();

            Assert.That(decl.Identifier, Is.EqualTo("seen"));
            Assert.That(decl.Initializer, Is.TypeOf<ArrInitExprNode>());
            Assert.That(add.Identifier, Is.EqualTo("seen.add"));
        }

        [Test]
        public void DictComprehensionExpressionStatementHoistsTempAndDiscardExpression()
        {
            var nodes = this.builder.BuildFromSource("{k: v for k, v in pairs}\n")
                .As<SourceNode>().Children.ToArray();
            var decl = nodes[0].As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var loop = nodes[1].As<ForStatNode>();
            var assign = loop.Statement.As<BlockStatNode>().Children.Single()
                .As<ExprStatNode>().Expression.As<AssignExprNode>();
            var discard = nodes[2].As<ExprStatNode>().Expression.As<IdNode>();

            Assert.That(decl.Identifier, Does.StartWith("__linvast_comp_"));
            Assert.That(decl.Initializer, Is.TypeOf<DictInitNode>());
            Assert.That(loop.ForDeclaration, Is.TypeOf<DeclListNode>());
            Assert.That(assign.LeftOperand, Is.TypeOf<ArrAccessExprNode>());
            Assert.That(assign.RightOperand.As<IdNode>().Identifier, Is.EqualTo("v"));
            Assert.That(discard.Identifier, Is.EqualTo(decl.Identifier));
        }

        [Test]
        public void DictComprehensionAssignmentExpandsToInitAndLoop()
        {
            var nodes = this.builder.BuildFromSource("squares = {x: x ** 2 for x in range(1, 6)}\n")
                .As<SourceNode>().Children.ToArray();
            var decl = nodes[0].As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var loop = nodes[1].As<ForStatNode>();
            var assign = loop.Statement.As<BlockStatNode>().Children.Single()
                .As<ExprStatNode>().Expression.As<AssignExprNode>();

            Assert.That(decl.Identifier, Is.EqualTo("squares"));
            Assert.That(decl.Initializer, Is.TypeOf<DictInitNode>());
            Assert.That(loop.ForDeclaration!.As<VarDeclNode>().Identifier, Is.EqualTo("x"));
            Assert.That(loop.Condition.As<FuncCallExprNode>().Identifier, Is.EqualTo("range"));
            Assert.That(assign.LeftOperand.As<ArrAccessExprNode>().Array.As<IdNode>().Identifier, Is.EqualTo("squares"));
            Assert.That(assign.LeftOperand.As<ArrAccessExprNode>().IndexExpression.As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(assign.RightOperand.As<FuncCallExprNode>().Identifier, Is.EqualTo("pow"));
        }

        [Test]
        public void DictComprehensionFollowedByFStringBuildsBothDeclarations()
        {
            var source =
                "squares = {x: x ** 2 for x in range(1, 6)}\n" +
                "message = f\"count={len(squares)}\"\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children.ToArray();

            Assert.That(nodes.Length, Is.EqualTo(3));

            var squaresDecl = nodes[0].As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            Assert.That(squaresDecl.Identifier, Is.EqualTo("squares"));
            Assert.That(squaresDecl.Initializer, Is.TypeOf<DictInitNode>());
            Assert.That(nodes[1], Is.TypeOf<ForStatNode>());

            var messageDecl = nodes[2].As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var formatCall = messageDecl.Initializer!.As<FuncCallExprNode>();
            ExprNode[] parts = formatCall.Arguments!.Expressions.ToArray();

            Assert.That(messageDecl.Identifier, Is.EqualTo("message"));
            Assert.That(formatCall.Identifier, Is.EqualTo("format"));
            Assert.That(parts[0].As<LitExprNode>().Value, Is.EqualTo("count="));
            Assert.That(parts[1].As<FuncCallExprNode>().Identifier, Is.EqualTo("len"));
            Assert.That(parts[1].As<FuncCallExprNode>().Arguments!.Expressions.Single().As<IdNode>().Identifier,
                Is.EqualTo("squares"));
        }

        [Test]
        public void ListComprehensionInReturnHoistsBeforeReturn()
        {
            var func = this.builder.BuildFromSource("def make():\n    return [x for x in xs]\n")
                .As<SourceNode>().Children.Single().As<FuncNode>();
            var body = func.Definition!.Children.ToArray();
            var decl = body[0].As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var loop = body[1].As<ForStatNode>();
            var ret = body[2].As<JumpStatNode>();

            Assert.That(decl.Identifier, Does.StartWith("__linvast_comp_"));
            Assert.That(decl.Initializer, Is.TypeOf<ArrInitExprNode>());
            Assert.That(loop.Condition.As<IdNode>().Identifier, Is.EqualTo("xs"));
            Assert.That(ret.ReturnExpr!.As<IdNode>().Identifier, Is.EqualTo(decl.Identifier));
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
        public void LeadingNewlineFStringStatementBuildsFormatCall()
        {
            var stat = this.ParseStatement("\nf\"123\"\n").As<ExprStatNode>();
            var call = stat.Expression.As<FuncCallExprNode>();

            Assert.That(call.Line, Is.EqualTo(2));
            Assert.That(call.Identifier, Is.EqualTo("format"));
            Assert.That(call.Arguments!.Expressions.Single().As<LitExprNode>().Value, Is.EqualTo("123"));
        }

        [Test]
        public void FStringAssignmentWithFunctionCallFieldBuildsFormatCall()
        {
            var stat = this.ParseStatement("message = f\"count={len(squares)}\"\n");
            var decl = stat.As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var call = decl.Initializer!.As<FuncCallExprNode>();
            ExprNode[] parts = call.Arguments!.Expressions.ToArray();

            Assert.That(decl.Identifier, Is.EqualTo("message"));
            Assert.That(call.Identifier, Is.EqualTo("format"));
            Assert.That(parts[0].As<LitExprNode>().Value, Is.EqualTo("count="));
            Assert.That(parts[1].As<FuncCallExprNode>().Identifier, Is.EqualTo("len"));
            Assert.That(parts[1].As<FuncCallExprNode>().Arguments!.Expressions.Single().As<IdNode>().Identifier,
                Is.EqualTo("squares"));
        }

        [Test]
        public void MultilineFStringBuildsFormatCall()
        {
            var stat = this.ParseStatement(
                "message = f\"\"\"count={len(squares)}\n" +
                "active={active_count}\"\"\"\n");
            var decl = stat.As<DeclStatNode>().DeclaratorList.Declarators.Single().As<VarDeclNode>();
            var call = decl.Initializer!.As<FuncCallExprNode>();
            ExprNode[] parts = call.Arguments!.Expressions.ToArray();

            Assert.That(decl.Identifier, Is.EqualTo("message"));
            Assert.That(call.Identifier, Is.EqualTo("format"));
            Assert.That(parts[0].As<LitExprNode>().Value, Is.EqualTo("count="));
            Assert.That(parts[1].As<FuncCallExprNode>().Identifier, Is.EqualTo("len"));
            Assert.That(parts[2].As<LitExprNode>().Value, Is.EqualTo("\nactive="));
            Assert.That(parts[3].As<IdNode>().Identifier, Is.EqualTo("active_count"));
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
        public void PowerOperatorBuildsPowCall()
        {
            var call = this.ParseExpression("x ** 2").As<FuncCallExprNode>();
            ExprNode[] args = call.Arguments!.Expressions.ToArray();

            Assert.That(call.Identifier, Is.EqualTo("pow"));
            Assert.That(args[0].As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(args[1].As<LitExprNode>().Value, Is.EqualTo(2L));
        }

        [Test]
        public void FloorDivisionBuildsFloorOfDivision()
        {
            var call = this.ParseExpression("a // b").As<FuncCallExprNode>();
            Assert.That(call.Identifier, Is.EqualTo("floor"));

            var div = call.Arguments!.Expressions.Single().As<ArithmExprNode>();
            Assert.That(div.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("a"));
            Assert.That(div.RightOperand.As<IdNode>().Identifier, Is.EqualTo("b"));
        }

        [Test]
        public void InOperatorBuildsContainsCall()
        {
            var call = this.ParseExpression("x in items").As<FuncCallExprNode>();
            ExprNode[] args = call.Arguments!.Expressions.ToArray();

            Assert.That(call.Identifier, Is.EqualTo("contains"));
            Assert.That(args[0].As<IdNode>().Identifier, Is.EqualTo("items"));
            Assert.That(args[1].As<IdNode>().Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void NotInOperatorBuildsNegatedContainsCall()
        {
            var unary = this.ParseExpression("x not in items").As<UnaryExprNode>();
            var call = unary.Operand.As<FuncCallExprNode>();
            ExprNode[] args = call.Arguments!.Expressions.ToArray();

            Assert.That(call.Identifier, Is.EqualTo("contains"));
            Assert.That(args[0].As<IdNode>().Identifier, Is.EqualTo("items"));
            Assert.That(args[1].As<IdNode>().Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void IsOperatorBuildsIdEqualityComparison()
        {
            var rel = this.ParseExpression("a is b").As<RelExprNode>();
            var left = rel.LeftOperand.As<FuncCallExprNode>();
            var right = rel.RightOperand.As<FuncCallExprNode>();

            Assert.That(left.Identifier, Is.EqualTo("id"));
            Assert.That(left.Arguments!.Expressions.Single().As<IdNode>().Identifier, Is.EqualTo("a"));
            Assert.That(right.Identifier, Is.EqualTo("id"));
            Assert.That(right.Arguments!.Expressions.Single().As<IdNode>().Identifier, Is.EqualTo("b"));
        }

        [Test]
        public void IsNotOperatorBuildsNegatedIdEquality()
        {
            var unary = this.ParseExpression("a is not b").As<UnaryExprNode>();
            var rel = unary.Operand.As<RelExprNode>();

            Assert.That(rel.LeftOperand.As<FuncCallExprNode>().Identifier, Is.EqualTo("id"));
            Assert.That(rel.RightOperand.As<FuncCallExprNode>().Identifier, Is.EqualTo("id"));
        }

        [Test]
        public void MatMulOperatorBuildsMatmulCall()
        {
            var call = this.ParseExpression("x @ y").As<FuncCallExprNode>();
            ExprNode[] args = call.Arguments!.Expressions.ToArray();

            Assert.That(call.Identifier, Is.EqualTo("matmul"));
            Assert.That(args[0].As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(args[1].As<IdNode>().Identifier, Is.EqualTo("y"));
        }

        [Test]
        public void PowerAssignmentBuildsPowCall()
        {
            var stat = this.ParseStatements("x = 1\nx **= 2\n").ElementAt(1).As<ExprStatNode>();
            var assign = stat.Expression.As<AssignExprNode>();
            var call = assign.RightOperand.As<FuncCallExprNode>();
            ExprNode[] args = call.Arguments!.Expressions.ToArray();

            Assert.That(assign.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(call.Identifier, Is.EqualTo("pow"));
            Assert.That(args[0].As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(args[1].As<LitExprNode>().Value, Is.EqualTo(2L));
        }

        [Test]
        public void FloorDivisionAssignmentBuildsFloorOfDivision()
        {
            var stat = this.ParseStatements("x = 1\nx //= 2\n").ElementAt(1).As<ExprStatNode>();
            var assign = stat.Expression.As<AssignExprNode>();
            var call = assign.RightOperand.As<FuncCallExprNode>();

            Assert.That(assign.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(call.Identifier, Is.EqualTo("floor"));

            var div = call.Arguments!.Expressions.Single().As<ArithmExprNode>();
            Assert.That(div.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(div.RightOperand.As<LitExprNode>().Value, Is.EqualTo(2L));
        }

        [Test]
        public void MatMulAssignmentBuildsMatmulCall()
        {
            var stat = this.ParseStatements("x = 1\nx @= m\n").ElementAt(1).As<ExprStatNode>();
            var assign = stat.Expression.As<AssignExprNode>();
            var call = assign.RightOperand.As<FuncCallExprNode>();
            ExprNode[] args = call.Arguments!.Expressions.ToArray();

            Assert.That(assign.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(call.Identifier, Is.EqualTo("matmul"));
            Assert.That(args[0].As<IdNode>().Identifier, Is.EqualTo("x"));
            Assert.That(args[1].As<IdNode>().Identifier, Is.EqualTo("m"));
        }

        private ExprNode ParseExpression(string source)
            => this.builder.BuildFromSource(source, parser => parser.test()).As<ExprNode>();

        private StatNode ParseStatement(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<StatNode>();

        private StatNode[] ParseStatements(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Cast<StatNode>().ToArray();

        private void AssertExpressionValue<T>(string source, T expected)
        {
            ExprNode expression = this.ParseExpression(source);
            Assert.That(ConstantExpressionEvaluator.TryEvaluateAs(expression, out T result));
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
