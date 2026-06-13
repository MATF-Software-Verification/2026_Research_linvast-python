using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        // test: or_test ('if' or_test 'else' test)? | lambdef
        public override ASTNode VisitTest(Python3Parser.TestContext ctx)
        {
            if (ctx.lambdef() is not null)
                return this.Visit(ctx.lambdef());

            if (ctx.IF() is not null) {
                ExprNode thenExpr = this.Visit(ctx.or_test(0)).As<ExprNode>();
                ExprNode cond = this.Visit(ctx.or_test(1)).As<ExprNode>();
                ExprNode elseExpr = this.Visit(ctx.test()).As<ExprNode>();
                return new CondExprNode(ctx.Start.Line, cond, thenExpr, elseExpr);
            }

            return this.Visit(ctx.or_test(0));
        }

        // test_nocond: or_test | lambdef_nocond
        public override ASTNode VisitTest_nocond(Python3Parser.Test_nocondContext ctx) =>
            ctx.lambdef_nocond() is not null
                ? this.Visit(ctx.lambdef_nocond())
                : this.Visit(ctx.or_test());

        // or_test: and_test ('or' and_test)*
        public override ASTNode VisitOr_test(Python3Parser.Or_testContext ctx) =>
            this.FoldLogicExpr(ctx.Start.Line, ctx.and_test(), "or");

        // and_test: not_test ('and' not_test)*
        public override ASTNode VisitAnd_test(Python3Parser.And_testContext ctx) =>
            this.FoldLogicExpr(ctx.Start.Line, ctx.not_test(), "and");

        // not_test: 'not' not_test | comparison
        public override ASTNode VisitNot_test(Python3Parser.Not_testContext ctx)
        {
            if (ctx.NOT() is not null) {
                ExprNode operand = this.Visit(ctx.not_test()).As<ExprNode>();
                var op = UnaryOpNode.FromSymbol(ctx.Start.Line, "not");
                return new UnaryExprNode(ctx.Start.Line, op, operand);
            }

            return this.Visit(ctx.comparison());
        }

        // comparison: expr (comp_op expr)*
        public override ASTNode VisitComparison(Python3Parser.ComparisonContext ctx)
        {
            Python3Parser.ExprContext[] exprs = ctx.expr();
            ExprNode first = this.Visit(exprs[0]).As<ExprNode>();

            if (ctx.comp_op() is null || ctx.comp_op().Length == 0)
                return first;

            Python3Parser.Comp_opContext[] ops = ctx.comp_op();
            if (exprs.Length == 2 && ops.Length == 1) {
                RelOpNode op = this.CreateRelOp(ctx.Start.Line, ops[0]);
                ExprNode second = this.Visit(exprs[1]).As<ExprNode>();
                return new RelExprNode(ctx.Start.Line, first, op, second);
            }

            ExprNode result = new RelExprNode(
                ctx.Start.Line,
                first,
                this.CreateRelOp(exprs[0].Start.Line, ops[0]),
                this.Visit(exprs[1]).As<ExprNode>());

            for (int i = 1; i < ops.Length; i++) {
                var comparison = new RelExprNode(
                    exprs[i].Start.Line,
                    this.Visit(exprs[i]).As<ExprNode>(),
                    this.CreateRelOp(exprs[i].Start.Line, ops[i]),
                    this.Visit(exprs[i + 1]).As<ExprNode>());
                result = new LogicExprNode(
                    ctx.Start.Line,
                    result,
                    BinaryLogicOpNode.FromSymbol(ctx.Start.Line, "and"),
                    comparison);
            }

            return result;
        }

        // comp_op: '<' | '>' | '==' | '>=' | '<=' | '!=' | 'in' | 'not' 'in' | 'is' | 'is' 'not'
        public override ASTNode VisitComp_op(Python3Parser.Comp_opContext ctx) =>
            this.CreateRelOp(ctx.Start.Line, ctx);

        // expr: atom_expr | expr op expr | unary expr
        public override ASTNode VisitExpr(Python3Parser.ExprContext ctx)
        {
            if (ctx.atom_expr() is not null)
                return this.Visit(ctx.atom_expr());

            Python3Parser.ExprContext[] exprs = ctx.expr();
            if (exprs.Length == 1) {
                ExprNode operand = this.Visit(exprs[0]).As<ExprNode>();
                foreach (IParseTree child in ctx.children) {
                    if (child is not ITerminalNode)
                        continue;
                    string symbol = child.GetText();
                    if (symbol is not ("+" or "-" or "~"))
                        continue;
                    UnaryOpNode op = symbol == "~"
                        ? new UnaryOpNode(ctx.Start.Line, symbol, UnaryOperations.BitwiseNotPrimitive)
                        : UnaryOpNode.FromSymbol(ctx.Start.Line, symbol);
                    operand = new UnaryExprNode(ctx.Start.Line, op, operand);
                }
                return operand;
            }

            ExprNode lhs = this.Visit(exprs[0]).As<ExprNode>();
            ExprNode rhs = this.Visit(exprs[1]).As<ExprNode>();
            return this.CreateBinaryArithmExpr(ctx, lhs, rhs);
        }

        // star_expr: '*' expr
        public override ASTNode VisitStar_expr(Python3Parser.Star_exprContext ctx)
        {
            ExprNode inner = this.Visit(ctx.expr()).As<ExprNode>();
            var op = UnaryOpNode.FromSymbol(ctx.Start.Line, "*");
            return new UnaryExprNode(ctx.Start.Line, op, inner);
        }

        // atom_expr: AWAIT? atom trailer*
        public override ASTNode VisitAtom_expr(Python3Parser.Atom_exprContext ctx)
        {
            if (ctx.AWAIT() is not null)
                throw new NotImplementedException("await expressions");

            ExprNode expr = this.Visit(ctx.atom()).As<ExprNode>();
            if (ctx.trailer() is null)
                return expr;

            foreach (Python3Parser.TrailerContext trailer in ctx.trailer())
                expr = this.ApplyTrailer(expr, trailer);

            return expr;
        }

        // atom: '(' ... ')' | '[' ... ']' | '{' ... '}' | name | NUMBER | STRING+ | '...' | 'None' | 'True' | 'False'
        public override ASTNode VisitAtom(Python3Parser.AtomContext ctx)
        {
            if (ctx.OPEN_PAREN() is not null) {
                if (ctx.yield_expr() is not null)
                    return this.Visit(ctx.yield_expr());
                if (ctx.testlist_comp() is not null)
                    return this.Visit(ctx.testlist_comp());
                return new ArrInitExprNode(ctx.Start.Line);
            }

            if (ctx.OPEN_BRACK() is not null) {
                if (ctx.testlist_comp() is not null)
                    return this.Visit(ctx.testlist_comp());
                return new ArrInitExprNode(ctx.Start.Line);
            }

            if (ctx.OPEN_BRACE() is not null) {
                if (ctx.dictorsetmaker() is not null)
                    return this.Visit(ctx.dictorsetmaker());
                return new DictInitNode(ctx.Start.Line);
            }

            if (ctx.name() is not null)
                return this.Visit(ctx.name());

            if (ctx.NUMBER() is not null)
                return ParseNumber(ctx.Start.Line, ctx.NUMBER().GetText());

            if (ctx.STRING() is not null && ctx.STRING().Length > 0)
                return this.BuildStringLiteral(ctx.Start.Line, ctx.STRING());

            if (ctx.ELLIPSIS() is not null)
                return new IdNode(ctx.Start.Line, "...");

            if (ctx.NONE() is not null)
                return new NullLitExprNode(ctx.Start.Line);

            if (ctx.TRUE() is not null)
                return new LitExprNode(ctx.Start.Line, true);

            if (ctx.FALSE() is not null)
                return new LitExprNode(ctx.Start.Line, false);

            throw new SyntaxErrorException("Unsupported atom", ctx.Start.Line, ctx.Start.Column);
        }

        // name: NAME | '_' | 'match'
        public override ASTNode VisitName(Python3Parser.NameContext ctx) =>
            new IdNode(ctx.Start.Line, ctx.GetText());

        // trailer: '(' arglist? ')' | '[' subscriptlist ']' | '.' name
        public override ASTNode VisitTrailer(Python3Parser.TrailerContext ctx) =>
            throw new NotImplementedException("trailer is handled via atom_expr");

        // subscriptlist: subscript_ (',' subscript_)* ','?
        public override ASTNode VisitSubscriptlist(Python3Parser.SubscriptlistContext ctx)
        {
            Python3Parser.Subscript_Context[] subscripts = ctx.subscript_();
            if (subscripts.Length == 1)
                return this.VisitSubscriptIndex(subscripts[0]);

            return new ArrInitExprNode(
                ctx.Start.Line,
                subscripts.Select(s => this.VisitSubscriptIndex(s).As<ExprNode>()));
        }

        // subscript_: test | test? ':' test? sliceop?
        public override ASTNode VisitSubscript_(Python3Parser.Subscript_Context ctx) =>
            this.VisitSubscriptIndex(ctx);

        // sliceop: ':' test?
        public override ASTNode VisitSliceop(Python3Parser.SliceopContext ctx) =>
            throw new NotImplementedException("sliceop is handled via subscript_");

        // testlist_star_expr: (test | star_expr) (',' (test | star_expr))* ','?
        public override ASTNode VisitTestlist_star_expr(Python3Parser.Testlist_star_exprContext ctx) =>
            this.VisitExpressionSequence(ctx);

        // testlist: test (',' test)* ','?
        public override ASTNode VisitTestlist(Python3Parser.TestlistContext ctx)
        {
            Python3Parser.TestContext[] tests = ctx.test();
            if (tests.Length == 1 && (ctx.COMMA() is null || ctx.COMMA().Length == 0))
                return this.Visit(tests[0]);

            return new ArrInitExprNode(
                ctx.Start.Line,
                tests.Select(t => this.Visit(t).As<ExprNode>()));
        }

        // exprlist: (expr | star_expr) (',' (expr | star_expr))* ','?
        public override ASTNode VisitExprlist(Python3Parser.ExprlistContext ctx) =>
            this.VisitExpressionSequence(ctx);

        // testlist_comp: (test | star_expr) (comp_for | (',' (test | star_expr))* ','?)
        public override ASTNode VisitTestlist_comp(Python3Parser.Testlist_compContext ctx)
        {
            if (ctx.comp_for() is not null)
                throw new NotImplementedException("comprehensions");

            return this.VisitExpressionSequence(ctx);
        }

        // dictorsetmaker: dict literal | set literal (+ optional comprehension)
        public override ASTNode VisitDictorsetmaker(Python3Parser.DictorsetmakerContext ctx)
        {
            if (ctx.comp_for() is not null)
                throw new NotImplementedException("dict/set comprehensions");

            if (ctx.COLON() is { Length: > 0 } || ctx.POWER() is { Length: > 0 }) {
                var entries = new List<DictEntryNode>();
                if (ctx.POWER() is not null) {
                    foreach (Python3Parser.ExprContext spread in ctx.expr())
                        entries.Add(this.CreateDictSpreadEntry(ctx.Start.Line, spread));
                }

                Python3Parser.TestContext[] tests = ctx.test();
                for (int i = 0; i + 1 < tests.Length; i += 2) {
                    entries.Add(this.CreateDictEntry(
                        ctx.Start.Line,
                        this.Visit(tests[i]).As<ExprNode>(),
                        this.Visit(tests[i + 1]).As<ExprNode>()));
                }

                return new DictInitNode(ctx.Start.Line, entries);
            }

            return new ArrInitExprNode(ctx.Start.Line, this.CollectOrderedExpressions(ctx));
        }

        // arglist: argument (',' argument)* ','?
        public override ASTNode VisitArglist(Python3Parser.ArglistContext ctx) =>
            new ExprListNode(
                ctx.Start.Line,
                ctx.argument().Select(a => this.Visit(a).As<ExprNode>()));

        // argument: test comp_for? | test '=' test | '**' test | '*' test
        public override ASTNode VisitArgument(Python3Parser.ArgumentContext ctx)
        {
            if (ctx.comp_for() is not null)
                throw new NotImplementedException("comprehensions in arguments");

            if (ctx.POWER() is not null) {
                ExprNode value = this.Visit(ctx.test()[0]).As<ExprNode>();
                var op = UnaryOpNode.FromSymbol(ctx.Start.Line, "**");
                return new UnaryExprNode(ctx.Start.Line, op, value);
            }

            if (ctx.STAR() is not null) {
                ExprNode value = this.Visit(ctx.test()[0]).As<ExprNode>();
                var op = UnaryOpNode.FromSymbol(ctx.Start.Line, "*");
                return new UnaryExprNode(ctx.Start.Line, op, value);
            }

            if (ctx.ASSIGN() is not null) {
                ExprNode key = this.Visit(ctx.test(0)).As<ExprNode>();
                ExprNode value = this.Visit(ctx.test(1)).As<ExprNode>();
                return new AssignExprNode(ctx.Start.Line, key, value);
            }

            return this.Visit(ctx.test(0));
        }

        // comp_iter: comp_for | comp_if
        public override ASTNode VisitComp_iter(Python3Parser.Comp_iterContext ctx) =>
            throw new NotImplementedException("comp_iter");

        // comp_for: ASYNC? 'for' exprlist 'in' or_test comp_iter?
        public override ASTNode VisitComp_for(Python3Parser.Comp_forContext ctx) =>
            throw new NotImplementedException("comp_for");

        // comp_if: 'if' test_nocond comp_iter?
        public override ASTNode VisitComp_if(Python3Parser.Comp_ifContext ctx) =>
            throw new NotImplementedException("comp_if");

        // yield_expr: 'yield' yield_arg?
        public override ASTNode VisitYield_expr(Python3Parser.Yield_exprContext ctx) =>
            throw new NotImplementedException("yield_expr");

        // yield_arg: 'from' test | testlist
        public override ASTNode VisitYield_arg(Python3Parser.Yield_argContext ctx) =>
            throw new NotImplementedException("yield_arg");

        // strings: STRING+
        public override ASTNode VisitStrings(Python3Parser.StringsContext ctx) =>
            this.BuildStringLiteral(ctx.Start.Line, ctx.STRING());


        private ASTNode BuildStringLiteral(int line, ITerminalNode[] tokens)
        {
            if (tokens.Any(t => IsFormatStringToken(t.GetText()))) {
                IEnumerable<ExprNode> parts = tokens.Select(t => new LitExprNode(line, t.GetText()));
                return new FuncCallExprNode(
                    line,
                    new IdNode(line, "format"),
                    new ExprListNode(line, parts));
            }

            var value = new StringBuilder();
            foreach (ITerminalNode token in tokens)
                value.Append(DecodePythonString(token.GetText()));

            return new LitExprNode(line, value.ToString());
        }


        private ExprNode FoldLogicExpr<TContext>(int line, TContext[] operands, string symbol)
            where TContext : ParserRuleContext
        {
            ExprNode[] exprs = operands.Select(o => this.Visit(o).As<ExprNode>()).ToArray();
            if (exprs.Length == 1)
                return exprs[0];

            var op = BinaryLogicOpNode.FromSymbol(line, symbol);
            ExprNode result = exprs[0];
            for (int i = 1; i < exprs.Length; i++)
                result = new LogicExprNode(exprs[i].Line, result, op, exprs[i]);
            return result;
        }

        private ExprNode CreateBinaryArithmExpr(Python3Parser.ExprContext ctx, ExprNode lhs, ExprNode rhs)
        {
            int line = ctx.Start.Line;
            if (ctx.POWER() is not null)
                return new ArithmExprNode(line, lhs, CreatePowerOp(line), rhs);
            if (ctx.STAR() is not null)
                return new ArithmExprNode(line, lhs, ArithmOpNode.FromSymbol(line, "*"), rhs);
            if (ctx.AT() is not null)
                return new ArithmExprNode(line, lhs, new ArithmOpNode(line, "@", BinaryOperations.MultiplyPrimitive), rhs);
            if (ctx.DIV() is not null)
                return new ArithmExprNode(line, lhs, ArithmOpNode.FromSymbol(line, "/"), rhs);
            if (ctx.MOD() is not null)
                return new ArithmExprNode(line, lhs, ArithmOpNode.FromSymbol(line, "%"), rhs);
            if (ctx.IDIV() is not null)
                return new ArithmExprNode(line, lhs, CreateFloorDivOp(line), rhs);
            if (ctx.ADD() is not null)
                return new ArithmExprNode(line, lhs, ArithmOpNode.FromSymbol(line, "+"), rhs);
            if (ctx.MINUS() is not null)
                return new ArithmExprNode(line, lhs, ArithmOpNode.FromSymbol(line, "-"), rhs);
            if (ctx.LEFT_SHIFT() is not null)
                return new ArithmExprNode(line, lhs, ArithmOpNode.FromBitwiseSymbol(line, "<<"), rhs);
            if (ctx.RIGHT_SHIFT() is not null)
                return new ArithmExprNode(line, lhs, ArithmOpNode.FromBitwiseSymbol(line, ">>"), rhs);
            if (ctx.AND_OP() is not null)
                return new ArithmExprNode(line, lhs, ArithmOpNode.FromBitwiseSymbol(line, "&"), rhs);
            if (ctx.XOR() is not null)
                return new ArithmExprNode(line, lhs, ArithmOpNode.FromBitwiseSymbol(line, "^"), rhs);
            if (ctx.OR_OP() is not null)
                return new ArithmExprNode(line, lhs, ArithmOpNode.FromBitwiseSymbol(line, "|"), rhs);

            throw new SyntaxErrorException("Unsupported binary expression", line, ctx.Start.Column);
        }

        private static ArithmOpNode CreatePowerOp(int line) =>
            new(line, "**", (x, y) => Math.Pow(Convert.ToDouble(x), Convert.ToDouble(y)));

        private static ArithmOpNode CreateFloorDivOp(int line) =>
            new(line, "//", (x, y) => Math.Floor(Convert.ToDouble(x) / Convert.ToDouble(y)));

        private RelOpNode CreateRelOp(int line, Python3Parser.Comp_opContext ctx)
        {
            string symbol = ctx.GetText().Replace(" ", string.Empty).ToLowerInvariant();
            return symbol switch
            {
                "in" => new RelOpNode(line, "in", (a, b) => ContainsValue(b, a)),
                "notin" => new RelOpNode(line, "not in", (a, b) => !ContainsValue(b, a)),
                "is" => new RelOpNode(line, "is", (a, b) => ReferenceEquals(a, b) || Equals(a, b)),
                "isnot" => new RelOpNode(line, "is not", (a, b) => !ReferenceEquals(a, b) && !Equals(a, b)),
                _ => RelOpNode.FromSymbol(line, ctx.GetText()),
            };
        }

        private static bool ContainsValue(object container, object item)
        {
            if (container is string str)
                return str.Contains(item?.ToString() ?? string.Empty);
            if (container is IEnumerable sequence and not string) {
                foreach (object? value in sequence) {
                    if (Equals(value, item))
                        return true;
                }
            }
            return false;
        }

        private ExprNode ApplyTrailer(ExprNode expr, Python3Parser.TrailerContext trailer)
        {
            int line = trailer.Start.Line;
            if (trailer.OPEN_PAREN() is not null) {
                ExprListNode args = trailer.arglist() is not null
                    ? this.Visit(trailer.arglist()).As<ExprListNode>()
                    : new ExprListNode(line);
                IdNode callee = this.ToCallableId(expr, line);
                return args.Expressions.Any()
                    ? new FuncCallExprNode(line, callee, args)
                    : new FuncCallExprNode(line, callee);
            }

            if (trailer.OPEN_BRACK() is not null) {
                ExprNode index = this.Visit(trailer.subscriptlist()).As<ExprNode>();
                return new ArrAccessExprNode(line, expr, index);
            }

            if (trailer.DOT() is not null) {
                string member = trailer.name().GetText();
                return expr is IdNode id
                    ? new IdNode(line, $"{id.Identifier}.{member}")
                    : new IdNode(line, $"{expr.GetText()}.{member}");
            }

            throw new SyntaxErrorException("Unsupported trailer", line, trailer.Start.Column);
        }

        private ExprNode VisitSubscriptIndex(Python3Parser.Subscript_Context ctx)
        {
            if (ctx.COLON() is null)
                return this.Visit(ctx.test(0)).As<ExprNode>();

            return new LitExprNode(ctx.Start.Line, ctx.GetText());
        }

        private ASTNode VisitExpressionSequence(ParserRuleContext ctx)
        {
            List<ExprNode> items = this.CollectOrderedExpressions(ctx);
            if (items.Count == 1)
                return items[0];

            return new ArrInitExprNode(ctx.Start.Line, items);
        }

        private List<ExprNode> CollectOrderedExpressions(ParserRuleContext ctx)
        {
            var items = new List<ExprNode>();
            foreach (IParseTree child in ctx.children) {
                switch (child) {
                    case Python3Parser.TestContext test:
                        items.Add(this.Visit(test).As<ExprNode>());
                        break;
                    case Python3Parser.ExprContext expr:
                        items.Add(this.Visit(expr).As<ExprNode>());
                        break;
                    case Python3Parser.Star_exprContext starExpr:
                        items.Add(this.Visit(starExpr).As<ExprNode>());
                        break;
                }
            }
            return items;
        }

        private DictEntryNode CreateDictEntry(int line, ExprNode keyExpr, ExprNode valueExpr) =>
            new(line, this.ToDictKeyId(keyExpr, line), valueExpr);

        private DictEntryNode CreateDictSpreadEntry(int line, Python3Parser.ExprContext spread) =>
            new(line, new IdNode(line, "**"), this.Visit(spread).As<ExprNode>());

        private IdNode ToCallableId(ExprNode expr, int line) =>
            expr as IdNode ?? new IdNode(line, expr.GetText());

        private IdNode ToDictKeyId(ExprNode keyExpr, int line) =>
            keyExpr as IdNode ?? new IdNode(line, keyExpr.GetText());

        private static LitExprNode ParseNumber(int line, string text)
        {
            text = text.Replace("_", string.Empty);
            if (text.EndsWith("j", StringComparison.OrdinalIgnoreCase))
                throw new NotImplementedException("complex number literals");

            if (text.Contains('.') || text.Contains('e') || text.Contains('E'))
                return new LitExprNode(line, double.Parse(text, CultureInfo.InvariantCulture));

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return new LitExprNode(line, Convert.ToInt64(text, 16));
            if (text.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
                return new LitExprNode(line, Convert.ToInt64(text[2..], 8));
            if (text.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                return new LitExprNode(line, Convert.ToInt64(text[2..], 2));

            return new LitExprNode(line, long.Parse(text, CultureInfo.InvariantCulture));
        }

        private static bool IsFormatStringToken(string token)
        {
            int index = 0;
            while (index < token.Length && "fFrRbBuU".Contains(token[index]))
                index++;
            return index > 0 && token[index] is '"' or '\'';
        }

        private static string DecodePythonString(string token)
        {
            int index = 0;
            while (index < token.Length && "fFrRbBuU".Contains(token[index]))
                index++;

            if (index >= token.Length - 1)
                return token;

            char quote = token[index];
            string content = token[(index + 1)..^1];
            if (quote == '\'')
                return content.Replace("\\'", "'").Replace("\\\\", "\\");
            return Regex.Unescape(content);
        }
    }
}
