using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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

            // A chained comparison such as `a < f() < b` desugars structurally
            // into `a < f() and f() < b`, so every interior operand participates
            // in two comparisons. The two RelExprNodes must NOT share the same
            // child instance: ASTNode's constructor wires `child.Parent = this`,
            // so a shared operand would end up parented only to the last
            // comparison, breaking the AST parent invariant. We therefore build
            // a fresh, independent subtree for each appearance by re-visiting the
            // parse tree. This builder only constructs nodes (it does not execute
            // the program), so re-visiting has no runtime "single evaluation"
            // implications -- it simply yields a structurally-equal, fully
            // parented duplicate.
            ExprNode Operand(int index)
                => this.Visit(exprs[index]).As<ExprNode>();

            if (ops.Length == 1)
                return new RelExprNode(ctx.Start.Line, first, this.CreateRelOp(ctx.Start.Line, ops[0]), Operand(1));

            ExprNode result = new RelExprNode(
                exprs[0].Start.Line,
                first,
                this.CreateRelOp(exprs[0].Start.Line, ops[0]),
                Operand(1));

            for (int i = 1; i < ops.Length; i++) {
                var comparison = new RelExprNode(
                    exprs[i].Start.Line,
                    Operand(i),
                    this.CreateRelOp(exprs[i].Start.Line, ops[i]),
                    Operand(i + 1));
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
            return new AssignExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, "*"), inner);
        }

        // atom_expr: AWAIT? atom trailer*
        public override ASTNode VisitAtom_expr(Python3Parser.Atom_exprContext ctx)
        {
            ExprNode expr = this.Visit(ctx.atom()).As<ExprNode>();
            if (ctx.trailer() is not null) {
                foreach (Python3Parser.TrailerContext trailer in ctx.trailer())
                    expr = this.ApplyTrailer(expr, trailer);
            }

            // `await x` is represented as a call to the synthetic `await` builtin,
            // mirroring how other Python-specific expressions are lowered.
            if (ctx.AWAIT() is not null)
                return new FuncCallExprNode(
                    ctx.Start.Line,
                    new IdNode(ctx.Start.Line, "await"),
                    new ExprListNode(ctx.Start.Line, expr));

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
                if (ctx.testlist_comp() is not null) {
                    return ctx.testlist_comp().comp_for() is not null
                        ? this.BuildComprehension(ctx.testlist_comp(), "list")
                        : this.Visit(ctx.testlist_comp());
                }
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
                return new EllipsisLitExprNode(ctx.Start.Line);

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
            // A bare testlist_comp wrapped in parentheses (or used directly) is a
            // generator expression; bracketed list comprehensions are handled in VisitAtom.
            if (ctx.comp_for() is not null)
                return this.BuildComprehension(ctx, "generator");

            return this.VisitExpressionSequence(ctx);
        }

        // dictorsetmaker: dict literal | set literal (+ optional comprehension)
        public override ASTNode VisitDictorsetmaker(Python3Parser.DictorsetmakerContext ctx)
        {
            if (ctx.comp_for() is not null) {
                if (ctx.COLON() is { Length: > 0 }) {
                    DictEntryNode entry = this.CreateDictEntry(
                        ctx.Start.Line,
                        this.Visit(ctx.test(0)).As<ExprNode>(),
                        this.Visit(ctx.test(1)).As<ExprNode>());
                    return this.BuildComprehension(ctx.Start.Line, "dict", entry, ctx.comp_for());
                }

                ExprNode element = ctx.test().Length > 0
                    ? this.Visit(ctx.test(0)).As<ExprNode>()
                    : this.Visit(ctx.star_expr(0)).As<ExprNode>();
                return this.BuildComprehension(ctx.Start.Line, "set", element, ctx.comp_for());
            }

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
            // `f(x for x in xs)` is a generator expression passed as the argument.
            if (ctx.comp_for() is not null) {
                ExprNode element = this.Visit(ctx.test(0)).As<ExprNode>();
                return this.BuildComprehension(ctx.Start.Line, "generator", element, ctx.comp_for());
            }

            if (ctx.POWER() is not null) {
                ExprNode value = this.Visit(ctx.test()[0]).As<ExprNode>();
                return new AssignExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, "**"), value);
            }

            if (ctx.STAR() is not null) {
                ExprNode value = this.Visit(ctx.test()[0]).As<ExprNode>();
                return new AssignExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, "*"), value);
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
            ctx.comp_for() is not null
                ? this.Visit(ctx.comp_for())
                : this.Visit(ctx.comp_if());

        // comp_for: ASYNC? 'for' exprlist 'in' or_test comp_iter?
        public override ASTNode VisitComp_for(Python3Parser.Comp_forContext ctx) =>
            new ExprListNode(ctx.Start.Line, this.CollectComprehensionClauses(ctx));

        // comp_if: 'if' test_nocond comp_iter?
        public override ASTNode VisitComp_if(Python3Parser.Comp_ifContext ctx)
        {
            var clauses = new List<ExprNode>();
            Python3Parser.Comp_iterContext? next = this.AddIfClause(clauses, ctx);
            this.CollectClausesFromIter(clauses, next);
            return new ExprListNode(ctx.Start.Line, clauses);
        }

        // yield_expr: 'yield' yield_arg?
        public override ASTNode VisitYield_expr(Python3Parser.Yield_exprContext ctx)
        {
            if (ctx.yield_arg() is null)
                return new YieldExprNode(ctx.Start.Line);

            return this.Visit(ctx.yield_arg());
        }

        // yield_arg: 'from' test | testlist
        public override ASTNode VisitYield_arg(Python3Parser.Yield_argContext ctx)
        {
            if (ctx.FROM() is not null) {
                ExprNode value = this.Visit(ctx.test()).As<ExprNode>();
                return new YieldExprNode(ctx.Start.Line, value, isDelegating: true);
            }

            ExprNode yielded = this.Visit(ctx.testlist()).As<ExprNode>();
            return new YieldExprNode(ctx.Start.Line, yielded);
        }

        // strings: STRING+
        public override ASTNode VisitStrings(Python3Parser.StringsContext ctx) =>
            this.BuildStringLiteral(ctx.Start.Line, ctx.STRING());


        private ASTNode BuildStringLiteral(int line, ITerminalNode[] tokens)
        {
            // Implicitly concatenated literals are merged into one node. If any
            // piece is an f-string the whole thing becomes a format(...) call.
            if (tokens.Any(t => IsFormatStringToken(t.GetText())))
                return this.BuildFormatString(line, tokens);

            var value = new StringBuilder();
            foreach (ITerminalNode token in tokens)
                value.Append(DecodePythonString(token.GetText()));

            return new LitExprNode(line, value.ToString());
        }

        // An f-string is lowered to a synthetic call:
        //   format(part0, part1, ...)
        // where each part is either a string literal (the text between fields, with
        // {{/}} unescaped) or a replacement field. A plain field is the parsed
        // expression itself; a field carrying a conversion and/or a format spec
        // becomes format_field(expr, conversion?, spec?) with null literals for the
        // missing pieces. A format spec that itself contains replacement fields is
        // represented recursively as a nested format(...) call.
        private ASTNode BuildFormatString(int line, ITerminalNode[] tokens)
        {
            var parts = new List<ExprNode>();
            var literal = new StringBuilder();

            foreach (ITerminalNode token in tokens) {
                (string content, bool isRaw, bool isFormat) = ExtractStringContent(token.GetText());
                if (!isFormat) {
                    literal.Append(isRaw ? content : UnescapePythonStringContent(content));
                    continue;
                }

                this.ScanFormatContent(line, content, isRaw, literal, parts);
            }

            FlushLiteral(line, literal, parts);
            if (parts.Count == 0)
                parts.Add(new LitExprNode(line, string.Empty));

            return new FuncCallExprNode(line, new IdNode(line, "format"), new ExprListNode(line, parts));
        }

        private void ScanFormatContent(int line, string content, bool isRaw, StringBuilder literal, List<ExprNode> parts)
        {
            int i = 0;
            while (i < content.Length) {
                char c = content[i];
                if (c == '{') {
                    if (i + 1 < content.Length && content[i + 1] == '{') {
                        literal.Append('{');
                        i += 2;
                        continue;
                    }
                    FlushLiteral(line, literal, parts);
                    i = this.ParseFormatField(line, content, i + 1, parts);
                    continue;
                }

                if (c == '}') {
                    // '}}' is an escaped brace; a lone '}' is tolerated as text.
                    literal.Append('}');
                    i += (i + 1 < content.Length && content[i + 1] == '}') ? 2 : 1;
                    continue;
                }

                if (!isRaw && c == '\\') {
                    i = AppendEscape(content, i, literal);
                    continue;
                }

                literal.Append(c);
                i++;
            }
        }

        // Parses a replacement field starting just after '{' and returns the index
        // immediately after the field's closing '}'.
        private int ParseFormatField(int line, string content, int start, List<ExprNode> parts)
        {
            int exprStart = start;
            int i = start;
            int depth = 0;
            int exprEnd = -1;

            while (i < content.Length) {
                char c = content[i];
                // A replacement field can hold an arbitrary expression, including
                // string literals such as f"{'}'}" or f"{'a:b'}". Skip over them
                // wholesale so that delimiters ({ } : !) appearing inside a quoted
                // literal are not mistaken for the structure of the field itself.
                if (c is '\'' or '"') {
                    i = SkipStringLiteral(content, i);
                    continue;
                }
                if (c is '(' or '[' or '{') {
                    depth++;
                    i++;
                    continue;
                }
                if (depth > 0) {
                    if (c is ')' or ']' or '}')
                        depth--;
                    i++;
                    continue;
                }
                if (c is ')' or ']') {
                    i++;
                    continue;
                }
                if (c == '}'
                    || (c == '!' && !(i + 1 < content.Length && content[i + 1] == '='))
                    || c == ':'
                    || IsDebugEquals(content, i)) {
                    exprEnd = i;
                    break;
                }
                i++;
            }

            if (exprEnd < 0)
                exprEnd = content.Length;

            string exprSrc = content[exprStart..exprEnd];
            i = exprEnd;

            bool debug = false;
            if (i < content.Length && content[i] == '=' && IsDebugEquals(content, i)) {
                debug = true;
                i++;
            }

            string? conversion = null;
            if (i < content.Length && content[i] == '!' && !(i + 1 < content.Length && content[i + 1] == '=')) {
                conversion = i + 1 < content.Length ? "!" + content[i + 1] : "!";
                i += conversion.Length;
            }

            ExprNode? specNode = null;
            if (i < content.Length && content[i] == ':') {
                int specStart = i + 1;
                int specDepth = 0;
                int j = specStart;
                while (j < content.Length) {
                    char c = content[j];
                    if (c is '\'' or '"') {
                        j = SkipStringLiteral(content, j);
                        continue;
                    }
                    if (c == '{')
                        specDepth++;
                    else if (c == '}') {
                        if (specDepth == 0)
                            break;
                        specDepth--;
                    }
                    j++;
                }
                specNode = this.BuildFormatSpec(line, content[specStart..j]);
                i = j;
            }

            // Consume up to and including the field's closing brace.
            while (i < content.Length && content[i] != '}')
                i++;
            if (i < content.Length)
                i++;

            if (debug)
                parts.Add(new LitExprNode(line, exprSrc + "="));

            // Python uses repr by default for the value of a `{x=}` debug field.
            if (debug && conversion is null && specNode is null)
                conversion = "!r";

            ExprNode valueExpr = this.ParseEmbeddedExpression(line, exprSrc);
            if (conversion is null && specNode is null) {
                parts.Add(valueExpr);
            } else {
                var fieldArgs = new ExprNode[] {
                    valueExpr,
                    conversion is null ? new NullLitExprNode(line) : new LitExprNode(line, conversion),
                    specNode ?? new NullLitExprNode(line),
                };
                parts.Add(new FuncCallExprNode(line, new IdNode(line, "format_field"), new ExprListNode(line, fieldArgs)));
            }

            return i;
        }

        private ExprNode BuildFormatSpec(int line, string specSrc)
        {
            var parts = new List<ExprNode>();
            var literal = new StringBuilder();
            // Format specs do not process backslash escapes, hence isRaw: true.
            this.ScanFormatContent(line, specSrc, isRaw: true, literal, parts);
            FlushLiteral(line, literal, parts);

            if (parts.Count == 0)
                return new LitExprNode(line, string.Empty);
            if (parts.Count == 1 && parts[0] is LitExprNode lit)
                return lit;
            return new FuncCallExprNode(line, new IdNode(line, "format"), new ExprListNode(line, parts));
        }

        private ExprNode ParseEmbeddedExpression(int line, string exprSrc)
        {
            exprSrc = exprSrc.Trim();
            if (exprSrc.Length == 0)
                return new LitExprNode(line, string.Empty);

            try {
                return this.BuildFromSource(exprSrc, p => p.testlist()).As<ExprNode>();
            } catch {
                return new LitExprNode(line, exprSrc);
            }
        }

        private static void FlushLiteral(int line, StringBuilder literal, List<ExprNode> parts)
        {
            if (literal.Length == 0)
                return;
            parts.Add(new LitExprNode(line, literal.ToString()));
            literal.Clear();
        }

        // Returns the index immediately past a string literal that begins at the
        // quote character at index i. Handles single/double quotes, triple-quoted
        // literals, and backslash escapes. If the literal is unterminated the end
        // of the content is returned. This lets the f-string field scanner treat an
        // embedded literal (e.g. the '}' in f"{'}'}") as opaque text.
        private static int SkipStringLiteral(string content, int i)
        {
            char quote = content[i];
            bool triple = i + 2 < content.Length && content[i + 1] == quote && content[i + 2] == quote;
            int j = i + (triple ? 3 : 1);
            while (j < content.Length) {
                char c = content[j];
                if (c == '\\') {
                    j += 2;
                    continue;
                }
                if (c == quote) {
                    if (!triple)
                        return j + 1;
                    if (j + 2 < content.Length && content[j + 1] == quote && content[j + 2] == quote)
                        return j + 3;
                }
                j++;
            }
            return content.Length;
        }

        // A single '=' acts as a debug specifier only when it is not part of an
        // operator (==, !=, <=, >=, :=) and is immediately followed (ignoring
        // spaces) by the end of the field, a conversion, or a format spec.
        private static bool IsDebugEquals(string content, int i)
        {
            if (content[i] != '=')
                return false;
            if (i + 1 < content.Length && content[i + 1] == '=')
                return false;
            if (i > 0 && content[i - 1] is '=' or '!' or '<' or '>' or ':')
                return false;

            int j = i + 1;
            while (j < content.Length && content[j] == ' ')
                j++;
            return j >= content.Length || content[j] is '}' or ':' or '!';
        }

        private static (string Content, bool IsRaw, bool IsFormat) ExtractStringContent(string token)
        {
            (int prefixEnd, bool isRaw, bool isFormat) = ParseStringPrefixes(token);
            if (prefixEnd >= token.Length || !IsStringDelimiter(token, prefixEnd))
                return (token, isRaw, isFormat);

            int quoteLength = GetQuoteLength(token, prefixEnd);
            int contentStart = prefixEnd + quoteLength;
            int contentEnd = token.Length - quoteLength;
            if (contentStart > contentEnd)
                return (string.Empty, isRaw, isFormat);

            return (token[contentStart..contentEnd], isRaw, isFormat);
        }


        private ExprNode FoldLogicExpr<TContext>(int line, TContext[] operands, string symbol)
            where TContext : ParserRuleContext
        {
            ExprNode[] exprs = operands.Select(o => this.Visit(o).As<ExprNode>()).ToArray();
            if (exprs.Length == 1)
                return exprs[0];

            // Each LogicExprNode must own a distinct operator instance: ASTNode's
            // constructor wires `child.Parent = this`, so a shared operator node
            // would be re-parented on every iteration, breaking the parent
            // invariant for all but the last binary logic expression.
            ExprNode result = exprs[0];
            for (int i = 1; i < exprs.Length; i++) {
                var op = BinaryLogicOpNode.FromSymbol(exprs[i].Line, symbol);
                result = new LogicExprNode(exprs[i].Line, result, op, exprs[i]);
            }
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
            // subscript_: test | test? ':' test? sliceop?
            if (ctx.COLON() is null)
                return this.Visit(ctx.test(0)).As<ExprNode>();

            // A slice such as a[start:stop:step] is lowered to a call to the
            // synthetic `slice` builtin, mirroring Python's slice(start, stop, step).
            // Omitted bounds are represented by null literals (Python's None).
            int line = ctx.Start.Line;
            ExprNode? start = null;
            ExprNode? stop = null;
            ExprNode? step = null;
            bool afterColon = false;
            foreach (IParseTree child in ctx.children) {
                switch (child) {
                    case ITerminalNode term when term.Symbol.Type == Python3Parser.COLON:
                        afterColon = true;
                        break;
                    case Python3Parser.TestContext test when !afterColon:
                        start = this.Visit(test).As<ExprNode>();
                        break;
                    case Python3Parser.TestContext test:
                        stop = this.Visit(test).As<ExprNode>();
                        break;
                    case Python3Parser.SliceopContext sliceop:
                        step = sliceop.test() is null ? null : this.Visit(sliceop.test()).As<ExprNode>();
                        break;
                }
            }

            var args = new ExprListNode(line, new ExprNode[] {
                start ?? new NullLitExprNode(line),
                stop ?? new NullLitExprNode(line),
                step ?? new NullLitExprNode(line),
            });
            return new FuncCallExprNode(line, new IdNode(line, "slice"), args);
        }

        // Builds a comprehension/generator from a testlist_comp whose element
        // precedes the comp_for clause (list, set and generator forms).
        private ASTNode BuildComprehension(Python3Parser.Testlist_compContext ctx, string kind)
        {
            ExprNode element = ctx.test().Length > 0
                ? this.Visit(ctx.test(0)).As<ExprNode>()
                : this.Visit(ctx.star_expr(0)).As<ExprNode>();
            return this.BuildComprehension(ctx.Start.Line, kind, element, ctx.comp_for());
        }

        // Represents a comprehension as a synthetic call: <kind>(element, clauses...)
        // where each clause is itself a call (for/async_for/if). This keeps the
        // builder consistent with how other Python-specific constructs are lowered.
        private ASTNode BuildComprehension(int line, string kind, ExprNode element, Python3Parser.Comp_forContext compFor)
        {
            var args = new List<ExprNode> { element };
            args.AddRange(this.CollectComprehensionClauses(compFor));
            return new FuncCallExprNode(line, new IdNode(line, kind), new ExprListNode(line, args));
        }

        private List<ExprNode> CollectComprehensionClauses(Python3Parser.Comp_forContext compFor)
        {
            var clauses = new List<ExprNode>();
            Python3Parser.Comp_iterContext? next = this.AddForClause(clauses, compFor);
            this.CollectClausesFromIter(clauses, next);
            return clauses;
        }

        private void CollectClausesFromIter(List<ExprNode> clauses, Python3Parser.Comp_iterContext? iter)
        {
            while (iter is not null) {
                iter = iter.comp_for() is not null
                    ? this.AddForClause(clauses, iter.comp_for())
                    : this.AddIfClause(clauses, iter.comp_if());
            }
        }

        private Python3Parser.Comp_iterContext? AddForClause(List<ExprNode> clauses, Python3Parser.Comp_forContext ctx)
        {
            ExprNode targets = this.Visit(ctx.exprlist()).As<ExprNode>();
            ExprNode iterable = this.Visit(ctx.or_test()).As<ExprNode>();
            string id = ctx.ASYNC() is not null ? "async_for" : "for";
            clauses.Add(new FuncCallExprNode(
                ctx.Start.Line,
                new IdNode(ctx.Start.Line, id),
                new ExprListNode(ctx.Start.Line, new[] { targets, iterable })));
            return ctx.comp_iter();
        }

        private Python3Parser.Comp_iterContext? AddIfClause(List<ExprNode> clauses, Python3Parser.Comp_ifContext ctx)
        {
            ExprNode cond = this.Visit(ctx.test_nocond()).As<ExprNode>();
            clauses.Add(new FuncCallExprNode(
                ctx.Start.Line,
                new IdNode(ctx.Start.Line, "if"),
                new ExprListNode(ctx.Start.Line, cond)));
            return ctx.comp_iter();
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
                return ParseImaginaryNumber(line, text);

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

        private static LitExprNode ParseImaginaryNumber(int line, string text)
        {
            text = text.Replace("_", string.Empty);
            string magnitude = text[..^1];
            if (magnitude is "+" or "-")
                magnitude += "1";
            double imaginary = double.Parse(magnitude, CultureInfo.InvariantCulture);
            return new LitExprNode(line, new System.Numerics.Complex(0, imaginary));
        }

        private static System.Numerics.Complex ToComplex(LitExprNode literal)
        {
            if (literal.Value is System.Numerics.Complex complex)
                return complex;

            return new System.Numerics.Complex(Convert.ToDouble(literal.Value), 0);
        }

        private static bool IsFormatStringToken(string token)
        {
            (int prefixEnd, _, bool isFormat) = ParseStringPrefixes(token);
            return isFormat && prefixEnd < token.Length && IsStringDelimiter(token, prefixEnd);
        }

        private static string DecodePythonString(string token)
        {
            (int prefixEnd, bool isRaw, _) = ParseStringPrefixes(token);
            if (prefixEnd >= token.Length || !IsStringDelimiter(token, prefixEnd))
                return token;

            int quoteLength = GetQuoteLength(token, prefixEnd);
            int contentStart = prefixEnd + quoteLength;
            int contentEnd = token.Length - quoteLength;
            if (contentStart > contentEnd)
                return token;

            string content = token[contentStart..contentEnd];
            return isRaw ? content : UnescapePythonStringContent(content);
        }

        private static (int PrefixEnd, bool IsRaw, bool IsFormat) ParseStringPrefixes(string token)
        {
            int index = 0;
            bool isRaw = false;
            bool isFormat = false;
            while (index < token.Length) {
                switch (token[index]) {
                    case 'r':
                    case 'R':
                        isRaw = true;
                        index++;
                        break;
                    case 'u':
                    case 'U':
                        index++;
                        break;
                    case 'b':
                    case 'B':
                        index++;
                        break;
                    case 'f':
                    case 'F':
                        isFormat = true;
                        index++;
                        break;
                    default:
                        return (index, isRaw, isFormat);
                }
            }
            return (index, isRaw, isFormat);
        }

        private static bool IsStringDelimiter(string token, int index) =>
            index < token.Length && token[index] is '"' or '\'';

        private static int GetQuoteLength(string token, int index)
        {
            if (index + 2 < token.Length
                && token[index] == token[index + 1]
                && token[index] == token[index + 2])
                return 3;
            return 1;
        }

        private static string UnescapePythonStringContent(string content)
        {
            var result = new StringBuilder(content.Length);
            int i = 0;
            while (i < content.Length) {
                if (content[i] == '\\')
                    i = AppendEscape(content, i, result);
                else
                    result.Append(content[i++]);
            }
            return result.ToString();
        }

        // Decodes a single escape sequence beginning at the backslash at index i
        // and returns the index just past the consumed characters.
        private static int AppendEscape(string content, int i, StringBuilder result)
        {
            if (++i >= content.Length) {
                result.Append('\\');
                return i;
            }
            char c = content[i];
            switch (c) {
                case 'n': result.Append('\n'); return i + 1;
                case 'r': result.Append('\r'); return i + 1;
                case 't': result.Append('\t'); return i + 1;
                case '\\': result.Append('\\'); return i + 1;
                case '\'': result.Append('\''); return i + 1;
                case '"': result.Append('"'); return i + 1;
                case 'a': result.Append('\a'); return i + 1;
                case 'b': result.Append('\b'); return i + 1;
                case 'f': result.Append('\f'); return i + 1;
                case 'v': result.Append('\v'); return i + 1;
                case '0': case '1': case '2': case '3':
                case '4': case '5': case '6': case '7':
                    return AppendOctalEscape(content, i, result);
                case 'x':
                    return AppendHexEscape(content, i + 1, result, 2, 'x');
                case 'u':
                    return AppendHexEscape(content, i + 1, result, 4, 'u');
                case 'U':
                    return AppendHexEscape(content, i + 1, result, 8, 'U');
                default:
                    // Unrecognized escapes (including `\N{name}`, which we cannot
                    // resolve without a Unicode name database) are preserved as a
                    // literal backslash followed by the character, matching
                    // Python's tolerant handling of unknown escapes.
                    result.Append('\\');
                    result.Append(c);
                    return i + 1;
            }
        }

        // Consumes up to three octal digits starting at index i (the first octal
        // digit) and appends the corresponding character.
        private static int AppendOctalEscape(string content, int i, StringBuilder result)
        {
            int end = i;
            int value = 0;
            while (end < content.Length && end - i < 3 && content[end] is >= '0' and <= '7') {
                value = (value * 8) + (content[end] - '0');
                end++;
            }
            result.Append((char)value);
            return end;
        }

        // Consumes exactly `digits` hexadecimal characters starting at index i and
        // appends the corresponding code point. `marker` is the escape letter
        // (x/u/U) so an invalid sequence can be preserved verbatim.
        private static int AppendHexEscape(string content, int i, StringBuilder result, int digits, char marker)
        {
            if (i + digits <= content.Length) {
                string hex = content.Substring(i, digits);
                if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint code)
                    && code <= 0x10FFFF
                    && !(code is >= 0xD800 and <= 0xDFFF)) {
                    result.Append(char.ConvertFromUtf32((int)code));
                    return i + digits;
                }
            }

            result.Append('\\');
            result.Append(marker);
            return i;
        }
    }
}
