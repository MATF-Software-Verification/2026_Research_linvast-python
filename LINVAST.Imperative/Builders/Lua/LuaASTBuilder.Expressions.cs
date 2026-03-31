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
                        throw new NotImplementedException("...");
                }

                if (ctx.number() is not null)
                    return LitExprNode.FromString(ctx.Start.Line, ctx.number().GetText());

                if (ctx.@string() is not null) {
                    string str = ctx.@string().GetText()[1..^1];
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

            if (ctx.nameAndArgs().Length > 1)
                throw new NotSupportedException("Multiple nameAndArgs");

            if (varOrExp is IdNode fname) {
                ExprListNode args = this.Visit(ctx.nameAndArgs().Single()).As<ExprListNode>();
                return new FuncCallExprNode(ctx.Start.Line, fname, args);
            } else {
                throw new NotSupportedException("Callable expressions");
            }
        }

        public override ASTNode VisitVarOrExp([NotNull] VarOrExpContext ctx)
            => ctx.exp() is not null ? this.Visit(ctx.exp()) : this.Visit(ctx.var());

        public override ASTNode VisitNameAndArgs([NotNull] NameAndArgsContext ctx)
        {
            if (ctx.NAME() is not null)
                throw new NotImplementedException("NAME");
            return this.Visit(ctx.args());
        }

        public override ASTNode VisitArgs([NotNull] ArgsContext ctx)
        {
            if (ctx.tableconstructor() is not null || ctx.@string() is not null)
                throw new NotImplementedException("tableconstructor or string");
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
            // TODO variadic?

            if (ctx.namelist() is null)
                return new FuncParamsNode(ctx.Start.Line);

            IdListNode nameList = this.Visit(ctx.namelist()).As<IdListNode>();
            IEnumerable<FuncParamNode> @params = nameList.Identifiers.Select(i => {
                var declSpecs = new DeclSpecsNode(i.Line);
                var decl = new VarDeclNode(i.Line, i);
                return new FuncParamNode(ctx.Start.Line, declSpecs, decl);
            });
            return new FuncParamsNode(ctx.Start.Line, @params);
        }

        public override ASTNode VisitTableconstructor([NotNull] TableconstructorContext ctx)
            => ctx.fieldlist() is not null ? this.Visit(ctx.fieldlist()) : new DictInitNode(ctx.Start.Line);

        public override ASTNode VisitFieldlist([NotNull] FieldlistContext ctx)
        {
            if (IsExpressionList(ctx))
                return new ExprListNode(ctx.Start.Line, ctx.field().Select(c => this.Visit(c).As<ExprNode>()));
            else if (IsAssignmentList(ctx))
                return new DictInitNode(ctx.Start.Line, ctx.field().Select(c => this.Visit(c).As<DictEntryNode>()));
            else
                throw new NotSupportedException("Mixed assignment and expressions in table constructor");


            static bool IsAssignmentList(FieldlistContext ctx)
                => ctx.field().All(f => f.children.Count > 1);

            static bool IsExpressionList(FieldlistContext ctx)
                => ctx.field().All(f => f.children.Count == 1);
        }

        public override ASTNode VisitField([NotNull] FieldContext ctx)
        {
            if (ctx.children.Count == 1)
                return this.Visit(ctx.exp().Single());

            if (ctx.exp().Length > 1)
                throw new NotImplementedException("Table assignment expression field");

            var key = new IdNode(ctx.Start.Line, ctx.NAME().GetText());
            ExprNode value = this.Visit(ctx.exp().Single()).As<ExprNode>();
            return new DictEntryNode(ctx.Start.Line, key, value);
        }
    }
}
