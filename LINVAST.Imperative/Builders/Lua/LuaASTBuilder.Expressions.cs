using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using static LINVAST.Imperative.Builders.Lua.LuaParser;

namespace LINVAST.Imperative.Builders.Lua
{
    public sealed partial class LuaASTBuilder : LuaBaseVisitor<ASTNode>, IASTBuilder<LuaParser>
    {
        public override ASTNode VisitExplist([NotNull] ExplistContext ctx)
            => new ExprListNode(ctx.Start.Line, ctx.exp().Select(v => this.Visit(v).As<ExprNode>()));

        public override ASTNode VisitExp([NotNull] ExpContext ctx)
        {
            if (!ctx.exp()?.Any() ?? true) {
                string firstToken = ctx.children.First().GetText();
                switch (firstToken) {
                    case "nil":
                        return new NullLitExprNode(ctx.Start.Line);
                    case "true":
                    case "false":
                        return LitExprNode.FromString(ctx.Start.Line, firstToken);
                    case "...":
                        return new IdNode(ctx.Start.Line, "...");
                }

                if (ctx.number() is not null)
                    return LitExprNode.FromString(ctx.Start.Line, ctx.number().GetText());

                if (ctx.@string() is not null) {
                    string str = ParseString(ctx.@string().GetText());
                    return new LitExprNode(ctx.Start.Line, str);
                }

                if (ctx.prefixexp() is not null)
                    return this.Visit(ctx.prefixexp());

                if (ctx.functiondef() is not null)
                    return this.Visit(ctx.functiondef());
            }

            if (ctx.operatorComparison() is not null) {
                (ExprNode left, string symbol, ExprNode right) = ParseBinaryExpression();
                var op = RelOpNode.FromSymbol(ctx.Start.Line, symbol);
                return new RelExprNode(ctx.Start.Line, left, op, right);
            }

            if (IsArithmeticExpressionContext(ctx)) {
                (ExprNode left, string symbol, ExprNode right) = ParseBinaryExpression();
                ArithmOpNode op = ctx.operatorBitwise() is not null
                    ? ArithmOpNode.FromBitwiseSymbol(ctx.Start.Line, symbol)
                    : ArithmOpNode.FromSymbol(ctx.Start.Line, symbol);
                return new ArithmExprNode(ctx.Start.Line, left, op, right);
            }

            if (IsLogicExpressionContext(ctx, out string? logicOp)) {
                if (ctx.operatorUnary() is not null) {
                    ExprNode notOperand = this.Visit(ctx.exp().First()).As<ExprNode>();
                    var notOp = UnaryOpNode.FromSymbol(ctx.Start.Line, "not");
                    return new UnaryExprNode(ctx.Start.Line, notOp, notOperand);
                }

                (ExprNode left, string symbol, ExprNode right) = ParseBinaryExpression();
                var op = BinaryLogicOpNode.FromSymbol(ctx.Start.Line, symbol);
                return new LogicExprNode(ctx.Start.Line, left, op, right);
            }

            if (ctx.operatorUnary() is not null) {
                ExprNode unaryOperand = this.Visit(ctx.exp().First()).As<ExprNode>();
                var unaryOp = UnaryOpNode.FromSymbol(ctx.Start.Line, ctx.children[0].GetText());
                return new UnaryExprNode(ctx.Start.Line, unaryOp, unaryOperand);
            }

            if (ctx.tableconstructor() is not null)
                return this.Visit(ctx.tableconstructor());

            // TODO
            throw new NotImplementedException("Unsupported expression type");


            (ExprNode left, string symbol, ExprNode right) ParseBinaryExpression()
            {
                ExprNode left = this.Visit(ctx.exp()[0]).As<ExprNode>();
                string op = ctx.children[1].GetText();
                if (op == "..")
                    op = "+";
                if (op == "~")
                    op = "^";
                ExprNode right = this.Visit(ctx.exp()[1]).As<ExprNode>();
                return (left, op, right);
            }

            static bool IsArithmeticExpressionContext(ExpContext ctx)
            {
                return ctx.operatorAddSub() is not null || ctx.operatorMulDivMod() is not null || ctx.operatorBitwise() is not null || ctx.operatorStrcat() is not null || ctx.operatorPower() is not null;
            }

            static bool IsLogicExpressionContext(ExpContext ctx, out string? op)
            {
                op = null;

                if (ctx.operatorAnd() is not null || ctx.operatorOr() is not null) {
                    op = ctx.children[1].GetText();
                    return true;
                }

                if (ctx.operatorUnary()?.GetText() == "not") {
                    op = "not";
                    return true;
                }

                return false;
            }
        }

        public override ASTNode VisitPrefixexp([NotNull] PrefixexpContext ctx)
        {
            ASTNode varOrExp = this.Visit(ctx.varOrExp());
            if (!ctx.nameAndArgs()?.Any() ?? true)
                return varOrExp;

            ExprNode expr = varOrExp.As<ExprNode>();
            foreach (NameAndArgsContext nameAndArgsCtx in ctx.nameAndArgs())
                expr = this.CreateFunctionCall(ctx.Start.Line, expr, nameAndArgsCtx);
            return expr;
        }

        public override ASTNode VisitVarOrExp([NotNull] VarOrExpContext ctx)
            => ctx.exp() is not null ? this.Visit(ctx.exp()) : this.Visit(ctx.var());

        public override ASTNode VisitNameAndArgs([NotNull] NameAndArgsContext ctx)
            => this.Visit(ctx.args());

        public override ASTNode VisitArgs([NotNull] ArgsContext ctx)
        {
            if (ctx.tableconstructor() is not null) {
                ExprNode table = this.Visit(ctx.tableconstructor()).As<ExprNode>();
                return new ExprListNode(ctx.Start.Line, table);
            }

            if (ctx.@string() is not null) {
                string str = ParseString(ctx.@string().GetText());
                return new ExprListNode(ctx.Start.Line, new LitExprNode(ctx.Start.Line, str));
            }

            return ctx.explist() is not null
                ? this.Visit(ctx.explist())
                : new ExprListNode(ctx.Start.Line);
        }

        public override ASTNode VisitFunctiondef([NotNull] FunctiondefContext ctx)
            => this.Visit(ctx.funcbody());

        public override ASTNode VisitFuncbody([NotNull] FuncbodyContext ctx)
        {
            FuncParamsNode? @params = null;
            if (ctx.parlist() is not null)
                @params = this.Visit(ctx.parlist()).As<FuncParamsNode>();
            BlockStatNode def = this.Visit(ctx.block()).As<BlockStatNode>();
            return @params is null
                ? new LambdaFuncExprNode(ctx.Start.Line, def)
                : new LambdaFuncExprNode(ctx.Start.Line, @params, def);
        }

        public override ASTNode VisitParlist([NotNull] ParlistContext ctx)
        {
            bool isVariadic = ctx.GetText().Contains("...", StringComparison.Ordinal);

            if (ctx.namelist() is null)
                return new FuncParamsNode(ctx.Start.Line) { IsVariadic = isVariadic };

            IdListNode nameList = this.Visit(ctx.namelist()).As<IdListNode>();
            IEnumerable<FuncParamNode> @params = nameList.Identifiers.Select(i => {
                var declSpecs = new DeclSpecsNode(i.Line);
                var decl = new VarDeclNode(i.Line, i);
                return new FuncParamNode(ctx.Start.Line, declSpecs, decl);
            });
            return new FuncParamsNode(ctx.Start.Line, @params) { IsVariadic = isVariadic };
        }

        public override ASTNode VisitTableconstructor([NotNull] TableconstructorContext ctx)
            => ctx.fieldlist() is not null ? this.Visit(ctx.fieldlist()) : new DictInitNode(ctx.Start.Line);

        public override ASTNode VisitFieldlist([NotNull] FieldlistContext ctx)
        {
            if (IsExpressionList(ctx))
                return new ExprListNode(ctx.Start.Line, ctx.field().Select(c => this.Visit(c).As<ExprNode>()));
            else if (IsAssignmentList(ctx))
                return new DictInitNode(ctx.Start.Line, ctx.field().Select(c => this.Visit(c).As<DictEntryNode>()));

            var entries = new List<DictEntryNode>();
            int arrayIndex = 1;
            foreach (FieldContext field in ctx.field()) {
                if (field.children.Count == 1) {
                    ExprNode value = this.Visit(field.exp().Single()).As<ExprNode>();
                    entries.Add(new DictEntryNode(field.Start.Line, new IdNode(field.Start.Line, arrayIndex.ToString()), value));
                    arrayIndex++;
                } else {
                    entries.Add(this.Visit(field).As<DictEntryNode>());
                }
            }

            return new DictInitNode(ctx.Start.Line, entries);


            static bool IsAssignmentList(FieldlistContext ctx)
                => ctx.field().All(f => f.children.Count > 1);

            static bool IsExpressionList(FieldlistContext ctx)
                => ctx.field().All(f => f.children.Count == 1);
        }

        public override ASTNode VisitField([NotNull] FieldContext ctx)
        {
            if (ctx.children.Count == 1)
                return this.Visit(ctx.exp().Single());

            if (ctx.exp().Length > 1) {
                ExprNode keyExpr = this.Visit(ctx.exp(0)).As<ExprNode>();
                string computedKey = keyExpr is LitExprNode { Value: string stringKey } ? stringKey : keyExpr.GetText();
                ExprNode computedValue = this.Visit(ctx.exp(1)).As<ExprNode>();
                return new DictEntryNode(ctx.Start.Line, new IdNode(ctx.Start.Line, computedKey), computedValue);
            }

            var namedKey = new IdNode(ctx.Start.Line, ctx.NAME().GetText());
            ExprNode namedValue = this.Visit(ctx.exp().Single()).As<ExprNode>();
            return new DictEntryNode(ctx.Start.Line, namedKey, namedValue);
        }

        private FuncCallExprNode CreateFunctionCall(int line, ExprNode target, NameAndArgsContext ctx)
        {
            string identifier = target.GetText();
            if (ctx.NAME() is not null)
                identifier = $"{identifier}:{ctx.NAME().GetText()}";

            var fname = new IdNode(line, identifier);
            ExprListNode args = this.Visit(ctx.args()).As<ExprListNode>();
            return args.Expressions.Any()
                ? new FuncCallExprNode(line, fname, args)
                : new FuncCallExprNode(line, fname);
        }

        private static string ParseString(string text)
        {
            if (!text.StartsWith("[", StringComparison.Ordinal))
                return text[1..^1];

            int contentStart = text.IndexOf('[', 1) + 1;
            int contentEnd = text.Length - contentStart;
            if (contentStart <= 0 || contentEnd < contentStart)
                return text[1..^1];

            return text[contentStart..contentEnd];
        }
    }
}
