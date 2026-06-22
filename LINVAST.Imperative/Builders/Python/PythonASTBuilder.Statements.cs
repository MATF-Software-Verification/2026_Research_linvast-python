using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    public sealed partial class PythonASTBuilder : Python3ParserBaseVisitor<ASTNode>, IASTBuilder<Python3Parser>
    {
        // compound_stmt: if_stmt | while_stmt | for_stmt | try_stmt | with_stmt | funcdef | classdef | decorated | async_stmt | match_stmt
        public override ASTNode VisitCompound_stmt(Python3Parser.Compound_stmtContext ctx) =>
            this.Visit(ctx.children.Single(c => c is ParserRuleContext));

        // block: simple_stmts | NEWLINE INDENT stmt+ DEDENT
        public override ASTNode VisitBlock(Python3Parser.BlockContext ctx)
        {
            if (ctx.simple_stmts() is not null)
                return this.Visit(ctx.simple_stmts());

            if (ctx.stmt() is null || ctx.stmt().Length == 0)
                return new BlockStatNode(ctx.Start.Line);

            var stmts = ctx.stmt().Select(this.Visit).ToArray();
            return new BlockStatNode(ctx.Start.Line, stmts);
        }

        // if_stmt: 'if' test ':' block ('elif' test ':' block)* ('else' ':' block)?
        public override ASTNode VisitIf_stmt(Python3Parser.If_stmtContext ctx)
        {
            Python3Parser.TestContext[] tests = ctx.test();
            Python3Parser.BlockContext[] blocks = ctx.block();

            StatNode? elseStmt = null;
            int elifCount = tests.Length - 1;
            if (ctx.ELSE() is not null)
                elseStmt = this.Visit(blocks[tests.Length]).As<StatNode>();

            for (int i = elifCount; i >= 1; i--) {
                ExprNode cond = this.Visit(tests[i]).As<ExprNode>();
                StatNode body = this.Visit(blocks[i]).As<StatNode>();
                elseStmt = elseStmt is null
                    ? new IfStatNode(tests[i].Start.Line, cond, body)
                    : new IfStatNode(tests[i].Start.Line, cond, body, elseStmt);
            }

            ExprNode ifCond = this.Visit(tests[0]).As<ExprNode>();
            StatNode ifBody = this.Visit(blocks[0]).As<StatNode>();
            return elseStmt is null
                ? new IfStatNode(ctx.Start.Line, ifCond, ifBody)
                : new IfStatNode(ctx.Start.Line, ifCond, ifBody, elseStmt);
        }

        // while_stmt: 'while' test ':' block ('else' ':' block)?
        public override ASTNode VisitWhile_stmt(Python3Parser.While_stmtContext ctx)
        {
            ExprNode cond = this.Visit(ctx.test()).As<ExprNode>();
            BlockStatNode body = this.Visit(ctx.block(0)).As<BlockStatNode>();

            if (ctx.ELSE() is not null)
                body = this.AppendLoopElse(body, this.Visit(ctx.block(1)).As<StatNode>(), ctx.Start.Line);

            return new WhileStatNode(ctx.Start.Line, cond, body);
        }

        // for_stmt: 'for' exprlist 'in' testlist ':' block ('else' ':' block)?
        public override ASTNode VisitFor_stmt(Python3Parser.For_stmtContext ctx)
        {
            DeclarationNode loopVar = this.CreateForLoopDeclaration(ctx);
            ExprNode iterable = this.Visit(ctx.testlist()).As<ExprNode>();
            BlockStatNode body = this.Visit(ctx.block(0)).As<BlockStatNode>();

            if (ctx.ELSE() is not null)
                body = this.AppendLoopElse(body, this.Visit(ctx.block(1)).As<StatNode>(), ctx.Start.Line);

            return new ForStatNode(ctx.Start.Line, loopVar, iterable, null, body);
        }

        // try_stmt: 'try' ':' block ((except_clause ':' block)+ ... | 'finally' ':' block)
        public override ASTNode VisitTry_stmt(Python3Parser.Try_stmtContext ctx)
        {
            StatNode tryBody = this.Visit(ctx.block(0)).As<StatNode>();
            Python3Parser.Except_clauseContext[] exceptClauses = ctx.except_clause();

            if (exceptClauses.Length == 0) {
                StatNode finallyBody = this.Visit(ctx.block(1)).As<StatNode>();
                return new TryStatNode(ctx.Start.Line, tryBody, System.Array.Empty<CatchClauseNode>(), null, finallyBody);
            }

            var catches = new CatchClauseNode[exceptClauses.Length];
            for (int i = 0; i < exceptClauses.Length; i++) {
                StatNode handlerBody = this.Visit(ctx.block(i + 1)).As<StatNode>();
                catches[i] = this.BuildCatchClause(exceptClauses[i], handlerBody);
            }

            int blockIndex = exceptClauses.Length + 1;
            StatNode? elseStat = null;
            if (ctx.ELSE() is not null)
                elseStat = this.Visit(ctx.block(blockIndex++)).As<StatNode>();

            StatNode? finallyStat = null;
            if (ctx.FINALLY() is not null)
                finallyStat = this.Visit(ctx.block(blockIndex)).As<StatNode>();

            return new TryStatNode(ctx.Start.Line, tryBody, catches, elseStat, finallyStat);
        }

        // with_stmt: 'with' with_item (',' with_item)* ':' block
        public override ASTNode VisitWith_stmt(Python3Parser.With_stmtContext ctx)
        {
            Python3Parser.With_itemContext[] items = ctx.with_item();
            StatNode body = this.Visit(ctx.block()).As<StatNode>();

            for (int i = items.Length - 1; i >= 0; i--) {
                (ExprNode context, ExprNode? target) = this.ParseWithItem(items[i]);
                body = target is null
                    ? new WithStatNode(items[i].Start.Line, context, body)
                    : new WithStatNode(items[i].Start.Line, context, target, body);
            }

            return body;
        }

        // with_item: test ('as' expr)?
        public override ASTNode VisitWith_item(Python3Parser.With_itemContext ctx)
        {
            (ExprNode context, ExprNode? target) = this.ParseWithItem(ctx);
            return target is null
                ? new ExprListNode(ctx.Start.Line, context)
                : new ExprListNode(ctx.Start.Line, new[] { context, target });
        }

        // except_clause: 'except' (test ('as' name)?)?
        public override ASTNode VisitExcept_clause(Python3Parser.Except_clauseContext ctx)
        {
            (ExprNode? exceptionType, IdNode? binding) = this.ParseExceptClause(ctx);
            if (exceptionType is null)
                return new ExprListNode(ctx.Start.Line);
            if (binding is null)
                return new ExprListNode(ctx.Start.Line, exceptionType);
            return new ExprListNode(ctx.Start.Line, new[] { exceptionType, binding });
        }

        // async_stmt: ASYNC (funcdef | with_stmt | for_stmt)
        public override ASTNode VisitAsync_stmt(Python3Parser.Async_stmtContext ctx)
        {
            if (ctx.funcdef() is not null)
                return this.CreateFunctionNode(ctx.funcdef(), new[] { new TagNode(ctx.Start.Line, "async") });

            if (ctx.with_stmt() is not null)
                return new AsyncStatNode(ctx.Start.Line, this.Visit(ctx.with_stmt()).As<StatNode>());

            return new AsyncStatNode(ctx.Start.Line, this.Visit(ctx.for_stmt()).As<StatNode>());
        }

        // flow_stmt: break_stmt | continue_stmt | return_stmt | raise_stmt | yield_stmt
        public override ASTNode VisitFlow_stmt(Python3Parser.Flow_stmtContext ctx) =>
            this.Visit(ctx.children.Single(c => c is ParserRuleContext));

        // pass_stmt: 'pass'
        public override ASTNode VisitPass_stmt(Python3Parser.Pass_stmtContext ctx) =>
            new EmptyStatNode(ctx.Start.Line);

        // break_stmt: 'break'
        public override ASTNode VisitBreak_stmt(Python3Parser.Break_stmtContext ctx) =>
            new JumpStatNode(ctx.Start.Line, JumpStatType.Break);

        // continue_stmt: 'continue'
        public override ASTNode VisitContinue_stmt(Python3Parser.Continue_stmtContext ctx) =>
            new JumpStatNode(ctx.Start.Line, JumpStatType.Continue);

        // return_stmt: 'return' testlist?
        public override ASTNode VisitReturn_stmt(Python3Parser.Return_stmtContext ctx)
        {
            if (ctx.testlist() is null)
                return new JumpStatNode(ctx.Start.Line, (ExprNode?)null);

            ExprNode expr = this.Visit(ctx.testlist()).As<ExprNode>();
            return new JumpStatNode(ctx.Start.Line, expr);
        }

        // raise_stmt: 'raise' (test ('from' test)?)?
        public override ASTNode VisitRaise_stmt(Python3Parser.Raise_stmtContext ctx)
        {
            if (ctx.test() is null || ctx.test().Length == 0)
                return new ThrowStatNode(ctx.Start.Line, new NullLitExprNode(ctx.Start.Line));

            ExprNode exc = this.Visit(ctx.test(0)).As<ExprNode>();
            return new ThrowStatNode(ctx.Start.Line, exc);
        }

        // yield_stmt: yield_expr
        public override ASTNode VisitYield_stmt(Python3Parser.Yield_stmtContext ctx)
        {
            ExprNode yieldExpr = this.Visit(ctx.yield_expr()).As<ExprNode>();
            return new ExprStatNode(ctx.Start.Line, yieldExpr);
        }

        // assert_stmt: 'assert' test (',' test)?
        public override ASTNode VisitAssert_stmt(Python3Parser.Assert_stmtContext ctx)
        {
            var args = ctx.test().Select(t => this.Visit(t).As<ExprNode>());
            var assertCall = new FuncCallExprNode(
                ctx.Start.Line,
                new IdNode(ctx.Start.Line, "assert"),
                new ExprListNode(ctx.Start.Line, args));
            return new ExprStatNode(ctx.Start.Line, assertCall);
        }

        // match_stmt: 'match' subject_expr ':' NEWLINE INDENT case_block+ DEDENT
        public override ASTNode VisitMatch_stmt(Python3Parser.Match_stmtContext ctx) =>
            throw new NotImplementedException("match_stmt");

        // case_block: 'case' patterns guard? ':' block
        public override ASTNode VisitCase_block(Python3Parser.Case_blockContext ctx) =>
            throw new NotImplementedException("case_block");

        // subject_expr: star_named_expression ',' star_named_expressions? | test
        public override ASTNode VisitSubject_expr(Python3Parser.Subject_exprContext ctx) =>
            throw new NotImplementedException("subject_expr");

        // star_named_expressions: ',' star_named_expression+ ','?
        public override ASTNode VisitStar_named_expressions(Python3Parser.Star_named_expressionsContext ctx) =>
            throw new NotImplementedException("star_named_expressions");

        // star_named_expression: '*' expr | test
        public override ASTNode VisitStar_named_expression(Python3Parser.Star_named_expressionContext ctx) =>
            throw new NotImplementedException("star_named_expression");

        // guard: 'if' test
        public override ASTNode VisitGuard(Python3Parser.GuardContext ctx) =>
            throw new NotImplementedException("guard");

        // patterns: open_sequence_pattern | pattern
        public override ASTNode VisitPatterns(Python3Parser.PatternsContext ctx) =>
            throw new NotImplementedException("patterns");

        // pattern: as_pattern | or_pattern
        public override ASTNode VisitPattern(Python3Parser.PatternContext ctx)
        {
            if (ctx.as_pattern() is not null)
                return this.Visit(ctx.as_pattern());

            return this.Visit(ctx.or_pattern());
        }

        // as_pattern: or_pattern 'as' pattern_capture_target
        public override ASTNode VisitAs_pattern(Python3Parser.As_patternContext ctx) =>
            throw new NotImplementedException("as_pattern");

        // or_pattern: closed_pattern ('|' closed_pattern)*
        public override ASTNode VisitOr_pattern(Python3Parser.Or_patternContext ctx)
        {
            PatternNode[] alternatives = ctx.closed_pattern()
                .Select(closed => this.Visit(closed).As<PatternNode>())
                .ToArray();

            return alternatives.Length == 1
                ? alternatives[0]
                : new OrPatternNode(ctx.Start.Line, alternatives);
        }

        // closed_pattern: literal_pattern | capture_pattern | wildcard_pattern | value_pattern | group_pattern | sequence_pattern | mapping_pattern | class_pattern
        public override ASTNode VisitClosed_pattern(Python3Parser.Closed_patternContext ctx)
        {
            if (ctx.literal_pattern() is not null)
                return this.Visit(ctx.literal_pattern());
            if (ctx.capture_pattern() is not null)
                return this.Visit(ctx.capture_pattern());
            if (ctx.wildcard_pattern() is not null)
                return this.Visit(ctx.wildcard_pattern());
            if (ctx.value_pattern() is not null)
                return this.Visit(ctx.value_pattern());
            if (ctx.group_pattern() is not null)
                return this.Visit(ctx.group_pattern());
            if (ctx.sequence_pattern() is not null)
                return this.Visit(ctx.sequence_pattern());
            if (ctx.mapping_pattern() is not null)
                throw new NotImplementedException("mapping_pattern");
            if (ctx.class_pattern() is not null)
                throw new NotImplementedException("class_pattern");

            throw new SyntaxErrorException("Unsupported closed pattern", ctx.Start.Line, ctx.Start.Column);
        }

        // literal_pattern: signed_number | complex_number | strings | 'None' | 'True' | 'False'
        public override ASTNode VisitLiteral_pattern(Python3Parser.Literal_patternContext ctx)
        {
            ExprNode value = this.BuildLiteralValue(ctx);
            return new LiteralPatternNode(ctx.Start.Line, value);
        }

        // literal_expr: signed_number | complex_number | strings | 'None' | 'True' | 'False'
        public override ASTNode VisitLiteral_expr(Python3Parser.Literal_exprContext ctx) =>
            this.BuildLiteralValue(ctx);

        // complex_number: signed_real_number ('+' | '-') imaginary_number
        public override ASTNode VisitComplex_number(Python3Parser.Complex_numberContext ctx)
        {
            LitExprNode real = this.Visit(ctx.signed_real_number()).As<LitExprNode>();
            LitExprNode imaginary = this.Visit(ctx.imaginary_number()).As<LitExprNode>();
            System.Numerics.Complex value = ToComplex(real);
            if (ctx.MINUS() is not null)
                value -= ToComplex(imaginary);
            else
                value += ToComplex(imaginary);

            return new LitExprNode(ctx.Start.Line, value);
        }

        // signed_number: NUMBER | '-' NUMBER
        public override ASTNode VisitSigned_number(Python3Parser.Signed_numberContext ctx)
        {
            string text = ctx.NUMBER().GetText();
            if (ctx.MINUS() is not null)
                text = "-" + text;
            return ParseNumber(ctx.Start.Line, text);
        }

        // signed_real_number: real_number | '-' real_number
        public override ASTNode VisitSigned_real_number(Python3Parser.Signed_real_numberContext ctx)
        {
            LitExprNode real = this.Visit(ctx.real_number()).As<LitExprNode>();
            if (ctx.MINUS() is null)
                return real;

            return new LitExprNode(ctx.Start.Line, -Convert.ToDouble(real.Value));
        }

        // real_number: NUMBER
        public override ASTNode VisitReal_number(Python3Parser.Real_numberContext ctx) =>
            ParseNumber(ctx.Start.Line, ctx.NUMBER().GetText());

        // imaginary_number: NUMBER
        public override ASTNode VisitImaginary_number(Python3Parser.Imaginary_numberContext ctx) =>
            ParseImaginaryNumber(ctx.Start.Line, ctx.NUMBER().GetText());

        // capture_pattern: pattern_capture_target
        public override ASTNode VisitCapture_pattern(Python3Parser.Capture_patternContext ctx)
        {
            IdNode target = this.Visit(ctx.pattern_capture_target()).As<IdNode>();
            return new CapturePatternNode(ctx.Start.Line, target);
        }

        // pattern_capture_target: name
        public override ASTNode VisitPattern_capture_target(Python3Parser.Pattern_capture_targetContext ctx) =>
            this.Visit(ctx.name());

        // wildcard_pattern: '_'
        public override ASTNode VisitWildcard_pattern(Python3Parser.Wildcard_patternContext ctx) =>
            new WildcardPatternNode(ctx.Start.Line);

        // value_pattern: attr
        public override ASTNode VisitValue_pattern(Python3Parser.Value_patternContext ctx)
        {
            IdNode value = this.Visit(ctx.attr()).As<IdNode>();
            return new ValuePatternNode(ctx.Start.Line, value);
        }

        // attr: name ('.' name)+
        public override ASTNode VisitAttr(Python3Parser.AttrContext ctx) =>
            new IdNode(ctx.Start.Line, ctx.GetText());

        // name_or_attr: attr | name
        public override ASTNode VisitName_or_attr(Python3Parser.Name_or_attrContext ctx)
        {
            if (ctx.attr() is not null)
                return this.Visit(ctx.attr());

            return this.Visit(ctx.name());
        }

        // group_pattern: '(' pattern ')'
        public override ASTNode VisitGroup_pattern(Python3Parser.Group_patternContext ctx)
        {
            PatternNode pattern = this.Visit(ctx.pattern()).As<PatternNode>();
            return new GroupPatternNode(ctx.Start.Line, pattern);
        }

        // sequence_pattern: '[' maybe_sequence_pattern? ']' | '(' open_sequence_pattern? ')'
        public override ASTNode VisitSequence_pattern(Python3Parser.Sequence_patternContext ctx)
        {
            if (ctx.OPEN_BRACK() is not null) {
                PatternNode[] elements = ctx.maybe_sequence_pattern() is null
                    ? System.Array.Empty<PatternNode>()
                    : this.ParseMaybeSequencePattern(ctx.maybe_sequence_pattern());
                return new SequencePatternNode(ctx.Start.Line, SequencePatternKind.Bracket, elements);
            }

            if (ctx.open_sequence_pattern() is null)
                return new SequencePatternNode(ctx.Start.Line, SequencePatternKind.Paren, System.Array.Empty<PatternNode>());

            PatternNode[] openElements = this.ParseOpenSequencePattern(ctx.open_sequence_pattern());
            return new SequencePatternNode(ctx.Start.Line, SequencePatternKind.OpenParen, openElements);
        }

        // open_sequence_pattern: maybe_star_pattern ',' maybe_sequence_pattern?
        public override ASTNode VisitOpen_sequence_pattern(Python3Parser.Open_sequence_patternContext ctx)
        {
            PatternNode[] elements = this.ParseOpenSequencePattern(ctx);
            return new SequencePatternNode(ctx.Start.Line, SequencePatternKind.OpenParen, elements);
        }

        // maybe_sequence_pattern: maybe_star_pattern (',' maybe_star_pattern)* ','?
        public override ASTNode VisitMaybe_sequence_pattern(Python3Parser.Maybe_sequence_patternContext ctx)
        {
            PatternNode[] elements = this.ParseMaybeSequencePattern(ctx);
            return new SequencePatternNode(ctx.Start.Line, SequencePatternKind.Bracket, elements);
        }

        // maybe_star_pattern: star_pattern | pattern
        public override ASTNode VisitMaybe_star_pattern(Python3Parser.Maybe_star_patternContext ctx)
        {
            if (ctx.star_pattern() is not null)
                return this.Visit(ctx.star_pattern());

            return this.Visit(ctx.pattern());
        }

        // star_pattern: '*' pattern_capture_target | '*' wildcard_pattern
        public override ASTNode VisitStar_pattern(Python3Parser.Star_patternContext ctx)
        {
            if (ctx.wildcard_pattern() is not null) {
                WildcardPatternNode wildcard = this.Visit(ctx.wildcard_pattern()).As<WildcardPatternNode>();
                return new StarPatternNode(ctx.Start.Line, wildcard);
            }

            IdNode target = this.Visit(ctx.pattern_capture_target()).As<IdNode>();
            if (target.Identifier == "_")
                return new StarPatternNode(ctx.Start.Line, new WildcardPatternNode(ctx.Start.Line));

            return new StarPatternNode(ctx.Start.Line, target);
        }

        // mapping_pattern: '{' ... '}'
        public override ASTNode VisitMapping_pattern(Python3Parser.Mapping_patternContext ctx) =>
            throw new NotImplementedException("mapping_pattern");

        // items_pattern: key_value_pattern (',' key_value_pattern)*
        public override ASTNode VisitItems_pattern(Python3Parser.Items_patternContext ctx) =>
            throw new NotImplementedException("items_pattern");

        // key_value_pattern: (literal_expr | attr) ':' pattern
        public override ASTNode VisitKey_value_pattern(Python3Parser.Key_value_patternContext ctx) =>
            throw new NotImplementedException("key_value_pattern");

        // double_star_pattern: '**' pattern_capture_target
        public override ASTNode VisitDouble_star_pattern(Python3Parser.Double_star_patternContext ctx) =>
            throw new NotImplementedException("double_star_pattern");

        // class_pattern: name_or_attr '(' ... ')'
        public override ASTNode VisitClass_pattern(Python3Parser.Class_patternContext ctx) =>
            throw new NotImplementedException("class_pattern");

        // positional_patterns: pattern (',' pattern)*
        public override ASTNode VisitPositional_patterns(Python3Parser.Positional_patternsContext ctx) =>
            throw new NotImplementedException("positional_patterns");

        // keyword_patterns: keyword_pattern (',' keyword_pattern)*
        public override ASTNode VisitKeyword_patterns(Python3Parser.Keyword_patternsContext ctx) =>
            throw new NotImplementedException("keyword_patterns");

        // keyword_pattern: name '=' pattern
        public override ASTNode VisitKeyword_pattern(Python3Parser.Keyword_patternContext ctx) =>
            throw new NotImplementedException("keyword_pattern");


        private DeclarationNode CreateForLoopDeclaration(Python3Parser.For_stmtContext ctx)
        {
            ASTNode visited = this.Visit(ctx.exprlist());
            if (visited is ExprListNode exprList) {
                VarDeclNode[] varDecls = exprList.Expressions
                    .Select(e => new VarDeclNode(e.Line, e.As<IdNode>()))
                    .ToArray();
                return varDecls.Length == 1 ? varDecls[0] : new DeclListNode(ctx.Start.Line, varDecls);
            }

            return new VarDeclNode(ctx.Start.Line, visited.As<IdNode>());
        }

        private BlockStatNode AppendLoopElse(BlockStatNode body, StatNode elseBlock, int line)
        {
            if (elseBlock is BlockStatNode elseBody)
                return new BlockStatNode(line, body.Children.Concat(elseBody.Children));

            return new BlockStatNode(line, body.Children.Append(elseBlock));
        }

        private (ExprNode context, ExprNode? target) ParseWithItem(Python3Parser.With_itemContext ctx)
        {
            ExprNode context = this.Visit(ctx.test()).As<ExprNode>();
            ExprNode? target = ctx.expr() is null ? null : this.Visit(ctx.expr()).As<ExprNode>();
            return (context, target);
        }

        private (ExprNode? exceptionType, IdNode? binding) ParseExceptClause(Python3Parser.Except_clauseContext ctx)
        {
            if (ctx.test() is null)
                return (null, null);

            ExprNode exceptionType = this.Visit(ctx.test()).As<ExprNode>();
            IdNode? binding = ctx.name() is null ? null : this.Visit(ctx.name()).As<IdNode>();
            return (exceptionType, binding);
        }

        private CatchClauseNode BuildCatchClause(Python3Parser.Except_clauseContext ctx, StatNode body)
        {
            (ExprNode? exceptionType, IdNode? binding) = this.ParseExceptClause(ctx);
            return new CatchClauseNode(ctx.Start.Line, body, exceptionType, binding);
        }

        private ExprNode BuildLiteralValue(Python3Parser.Literal_patternContext ctx)
        {
            if (ctx.signed_number() is not null)
                return this.Visit(ctx.signed_number()).As<ExprNode>();
            if (ctx.complex_number() is not null)
                return this.Visit(ctx.complex_number()).As<ExprNode>();
            if (ctx.strings() is not null)
                return this.Visit(ctx.strings()).As<ExprNode>();
            if (ctx.NONE() is not null)
                return new NullLitExprNode(ctx.Start.Line);
            if (ctx.TRUE() is not null)
                return new LitExprNode(ctx.Start.Line, true);
            if (ctx.FALSE() is not null)
                return new LitExprNode(ctx.Start.Line, false);

            throw new SyntaxErrorException("Unsupported literal pattern", ctx.Start.Line, ctx.Start.Column);
        }

        private ExprNode BuildLiteralValue(Python3Parser.Literal_exprContext ctx)
        {
            if (ctx.signed_number() is not null)
                return this.Visit(ctx.signed_number()).As<ExprNode>();
            if (ctx.complex_number() is not null)
                return this.Visit(ctx.complex_number()).As<ExprNode>();
            if (ctx.strings() is not null)
                return this.Visit(ctx.strings()).As<ExprNode>();
            if (ctx.NONE() is not null)
                return new NullLitExprNode(ctx.Start.Line);
            if (ctx.TRUE() is not null)
                return new LitExprNode(ctx.Start.Line, true);
            if (ctx.FALSE() is not null)
                return new LitExprNode(ctx.Start.Line, false);

            throw new SyntaxErrorException("Unsupported literal expression", ctx.Start.Line, ctx.Start.Column);
        }

        private PatternNode[] ParseMaybeSequencePattern(Python3Parser.Maybe_sequence_patternContext ctx) =>
            ctx.maybe_star_pattern()
                .Select(maybeStar => this.Visit(maybeStar).As<PatternNode>())
                .ToArray();

        private PatternNode[] ParseOpenSequencePattern(Python3Parser.Open_sequence_patternContext ctx)
        {
            var elements = new List<PatternNode> { this.Visit(ctx.maybe_star_pattern()).As<PatternNode>() };
            if (ctx.maybe_sequence_pattern() is not null)
                elements.AddRange(this.ParseMaybeSequencePattern(ctx.maybe_sequence_pattern()));
            return elements.ToArray();
        }
    }
}
