using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
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
                int mark = this.MarkPendingComprehensions();
                ExprNode cond = this.Visit(tests[i]).As<ExprNode>();
                IReadOnlyList<PendingComprehension> comprehensions = this.TakePendingComprehensions(mark);
                StatNode body = this.Visit(blocks[i]).As<StatNode>();
                StatNode elifStat = elseStmt is null
                    ? new IfStatNode(tests[i].Start.Line, cond, body)
                    : new IfStatNode(tests[i].Start.Line, cond, body, elseStmt);
                elseStmt = comprehensions.Count == 0
                    ? elifStat
                    : HoistComprehensionsBefore(tests[i].Start.Line, comprehensions, elifStat);
            }

            int ifMark = this.MarkPendingComprehensions();
            ExprNode ifCond = this.Visit(tests[0]).As<ExprNode>();
            IReadOnlyList<PendingComprehension> ifComprehensions = this.TakePendingComprehensions(ifMark);
            StatNode ifBody = this.Visit(blocks[0]).As<StatNode>();
            StatNode ifStat = elseStmt is null
                ? new IfStatNode(ctx.Start.Line, ifCond, ifBody)
                : new IfStatNode(ctx.Start.Line, ifCond, ifBody, elseStmt);
            return ifComprehensions.Count == 0
                ? ifStat
                : HoistComprehensionsBefore(ctx.Start.Line, ifComprehensions, ifStat);
        }

        // while_stmt: 'while' test ':' block ('else' ':' block)?
        public override ASTNode VisitWhile_stmt(Python3Parser.While_stmtContext ctx)
        {
            int mark = this.MarkPendingComprehensions();
            ExprNode cond = this.Visit(ctx.test()).As<ExprNode>();
            IReadOnlyList<PendingComprehension> comprehensions = this.TakePendingComprehensions(mark);
            BlockStatNode body = this.Visit(ctx.block(0)).As<BlockStatNode>();

            if (ctx.ELSE() is not null)
                body = this.AppendLoopElse(body, this.Visit(ctx.block(1)).As<StatNode>(), ctx.Start.Line);

            var whileStat = new WhileStatNode(ctx.Start.Line, cond, body);
            return comprehensions.Count == 0
                ? whileStat
                : HoistComprehensionsBefore(ctx.Start.Line, comprehensions, whileStat);
        }

        // for_stmt: 'for' exprlist 'in' testlist ':' block ('else' ':' block)?
        public override ASTNode VisitFor_stmt(Python3Parser.For_stmtContext ctx)
        {
            DeclarationNode loopVar = this.CreateForLoopDeclaration(ctx);
            int mark = this.MarkPendingComprehensions();
            ExprNode iterable = this.Visit(ctx.testlist()).As<ExprNode>();
            IReadOnlyList<PendingComprehension> comprehensions = this.TakePendingComprehensions(mark);
            BlockStatNode body = this.Visit(ctx.block(0)).As<BlockStatNode>();

            if (ctx.ELSE() is not null)
                body = this.AppendLoopElse(body, this.Visit(ctx.block(1)).As<StatNode>(), ctx.Start.Line);

            var forStat = new ForStatNode(ctx.Start.Line, loopVar, iterable, null, body);
            return comprehensions.Count == 0
                ? forStat
                : HoistComprehensionsBefore(ctx.Start.Line, comprehensions, forStat);
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

            int mark = this.MarkPendingComprehensions();
            ExprNode expr = this.Visit(ctx.testlist()).As<ExprNode>();
            IReadOnlyList<PendingComprehension> comprehensions = this.TakePendingComprehensions(mark);
            var returnStat = new JumpStatNode(ctx.Start.Line, expr);
            return comprehensions.Count == 0
                ? returnStat
                : HoistComprehensionsBefore(ctx.Start.Line, comprehensions, returnStat);
        }

        // raise_stmt: 'raise' (test ('from' test)?)?
        public override ASTNode VisitRaise_stmt(Python3Parser.Raise_stmtContext ctx)
        {
            if (ctx.test() is null || ctx.test().Length == 0)
                return new ThrowStatNode(ctx.Start.Line, new NullLitExprNode(ctx.Start.Line));

            int mark = this.MarkPendingComprehensions();
            ExprNode exc = this.Visit(ctx.test(0)).As<ExprNode>();
            IReadOnlyList<PendingComprehension> comprehensions = this.TakePendingComprehensions(mark);
            var raiseStat = new ThrowStatNode(ctx.Start.Line, exc);
            return comprehensions.Count == 0
                ? raiseStat
                : HoistComprehensionsBefore(ctx.Start.Line, comprehensions, raiseStat);
        }

        // yield_stmt: yield_expr
        public override ASTNode VisitYield_stmt(Python3Parser.Yield_stmtContext ctx)
        {
            int mark = this.MarkPendingComprehensions();
            ExprNode yieldExpr = this.Visit(ctx.yield_expr()).As<ExprNode>();
            IReadOnlyList<PendingComprehension> comprehensions = this.TakePendingComprehensions(mark);
            var yieldStat = new ExprStatNode(ctx.Start.Line, yieldExpr);
            return comprehensions.Count == 0
                ? yieldStat
                : HoistComprehensionsBefore(ctx.Start.Line, comprehensions, yieldStat);
        }

        // assert_stmt: 'assert' test (',' test)?
        public override ASTNode VisitAssert_stmt(Python3Parser.Assert_stmtContext ctx)
        {
            int mark = this.MarkPendingComprehensions();
            var args = ctx.test().Select(t => this.Visit(t).As<ExprNode>()).ToArray();
            IReadOnlyList<PendingComprehension> comprehensions = this.TakePendingComprehensions(mark);
            var assertCall = new FuncCallExprNode(
                ctx.Start.Line,
                new IdNode(ctx.Start.Line, "assert"),
                new ExprListNode(ctx.Start.Line, args));
            var assertStat = new ExprStatNode(ctx.Start.Line, assertCall);
            return comprehensions.Count == 0
                ? assertStat
                : HoistComprehensionsBefore(ctx.Start.Line, comprehensions, assertStat);
        }

        // match_stmt: 'match' subject_expr ':' NEWLINE INDENT case_block+ DEDENT
        public override ASTNode VisitMatch_stmt(Python3Parser.Match_stmtContext ctx)
        {
            ExprNode subject = this.Visit(ctx.subject_expr()).As<ExprNode>();
            LabeledStatNode[] cases = ctx.case_block()
                .Select(caseBlock => this.Visit(caseBlock).As<LabeledStatNode>())
                .ToArray();
            return new SwitchStatNode(ctx.Start.Line, subject, new BlockStatNode(ctx.Start.Line, cases));
        }

        // case_block: 'case' patterns guard? ':' block
        public override ASTNode VisitCase_block(Python3Parser.Case_blockContext ctx)
        {
            string patternText = SourceText(ctx.patterns());
            string label = patternText == "_" && ctx.guard() is null
                ? "default"
                : $"case {patternText}";
            StatNode body = this.Visit(ctx.block()).As<StatNode>();

            if (ctx.guard() is not null)
                label += $" if {SourceText(ctx.guard().test())}";

            return new LabeledStatNode(ctx.Start.Line, label, body);
        }

        // subject_expr: star_named_expression ',' star_named_expressions? | test
        public override ASTNode VisitSubject_expr(Python3Parser.Subject_exprContext ctx)
        {
            if (ctx.test() is not null)
                return this.Visit(ctx.test());

            var items = new List<ExprNode> { this.Visit(ctx.star_named_expression()).As<ExprNode>() };
            if (ctx.star_named_expressions() is not null) {
                ExprListNode rest = this.Visit(ctx.star_named_expressions()).As<ExprListNode>();
                items.AddRange(rest.Expressions);
            }

            return new ArrInitExprNode(ctx.Start.Line, items);
        }

        // star_named_expressions: ',' star_named_expression+ ','?
        public override ASTNode VisitStar_named_expressions(Python3Parser.Star_named_expressionsContext ctx)
        {
            IEnumerable<ExprNode> expressions = ctx.star_named_expression()
                .Select(expression => this.Visit(expression).As<ExprNode>());
            return new ExprListNode(ctx.Start.Line, expressions);
        }

        // star_named_expression: '*' expr | test
        public override ASTNode VisitStar_named_expression(Python3Parser.Star_named_expressionContext ctx)
        {
            if (ctx.expr() is not null) {
                ExprNode value = this.Visit(ctx.expr()).As<ExprNode>();
                return new AssignExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, "*"), value);
            }

            return this.Visit(ctx.test());
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

        private DeclarationNode CreateForLoopDeclaration(Python3Parser.For_stmtContext ctx)
        {
            ASTNode visited = this.Visit(ctx.exprlist());
            return this.CreateForLoopDeclaration(ctx.Start.Line, visited);
        }

        private DeclarationNode CreateForLoopDeclaration(int line, ASTNode visited)
        {
            if (visited is ExprListNode exprList) {
                VarDeclNode[] varDecls = exprList.Expressions
                    .Select(e => new VarDeclNode(e.Line, e.As<IdNode>()))
                    .ToArray();
                return varDecls.Length == 1 ? varDecls[0] : new DeclListNode(line, varDecls);
            }

            return new VarDeclNode(line, visited.As<IdNode>());
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

        private static string SourceText(ParserRuleContext ctx)
        {
            var interval = new Interval(ctx.Start.StartIndex, ctx.Stop.StopIndex);
            return ctx.Start.InputStream.GetText(interval).Trim();
        }
    }
}
