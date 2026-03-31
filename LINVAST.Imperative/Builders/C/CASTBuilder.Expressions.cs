using System;
using System.Linq;
using Antlr4.Runtime.Misc;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using static LINVAST.Imperative.Builders.C.CParser;

namespace LINVAST.Imperative.Builders.C
{
    public sealed partial class CASTBuilder : CBaseVisitor<ASTNode>, IASTBuilder<CParser>
    {
        public override ASTNode VisitExpression([NotNull] ExpressionContext ctx)
        {
            if (ctx.expression() is not null)
                throw new NotImplementedException("expression list");

            return this.Visit(ctx.assignmentExpression());
        }

        public override ASTNode VisitAssignmentExpression([NotNull] AssignmentExpressionContext ctx)
        {
            if (ctx.DigitSequence() is not null)
                throw new NotImplementedException("digit sequence");

            if (ctx.conditionalExpression() is not null)
                return this.Visit(ctx.conditionalExpression());

            ExprNode unary = this.Visit(ctx.unaryExpression()).As<ExprNode>();
            string symbol = ctx.children[1].GetText();
            var op = AssignOpNode.FromSymbol(ctx.Start.Line, symbol);
            ExprNode expr = this.Visit(ctx.assignmentExpression()).As<ExprNode>();

            return new AssignExprNode(ctx.Start.Line, unary, op, expr);
        }

        public override ASTNode VisitConditionalExpression([NotNull] ConditionalExpressionContext ctx)
        {
            ExprNode expr = this.Visit(ctx.logicalOrExpression()).As<ExprNode>();
            if (ctx.expression() is null)
                return expr;

            ExprNode thenExpr = this.Visit(ctx.expression()).As<ExprNode>();
            ExprNode elseExpr = this.Visit(ctx.conditionalExpression()).As<ExprNode>();
            return new CondExprNode(ctx.Start.Line, expr, thenExpr, elseExpr);
        }

        public override ASTNode VisitLogicalOrExpression([NotNull] LogicalOrExpressionContext ctx)
        {
            if (ctx.ChildCount > 1) {
                ExprNode left = this.Visit(ctx.logicalOrExpression()).As<ExprNode>();
                ExprNode right = this.Visit(ctx.logicalAndExpression()).As<ExprNode>();
                var op = BinaryLogicOpNode.FromSymbol(ctx.Start.Line, ctx.children[1].GetText());
                return new LogicExprNode(ctx.Start.Line, left, op, right);
            } else {
                return this.Visit(ctx.logicalAndExpression());
            }
        }

        public override ASTNode VisitLogicalAndExpression([NotNull] LogicalAndExpressionContext ctx)
        {
            if (ctx.ChildCount > 1) {
                ExprNode left = this.Visit(ctx.logicalAndExpression()).As<ExprNode>();
                ExprNode right = this.Visit(ctx.inclusiveOrExpression()).As<ExprNode>();
                var op = BinaryLogicOpNode.FromSymbol(ctx.Start.Line, ctx.children[1].GetText());
                return new LogicExprNode(ctx.Start.Line, left, op, right);
            } else {
                return this.Visit(ctx.inclusiveOrExpression());
            }
        }

        public override ASTNode VisitInclusiveOrExpression([NotNull] InclusiveOrExpressionContext ctx)
        {
            if (ctx.ChildCount > 1) {
                ExprNode left = this.Visit(ctx.inclusiveOrExpression()).As<ExprNode>();
                ExprNode right = this.Visit(ctx.exclusiveOrExpression()).As<ExprNode>();
                string symbol = ctx.children[1].GetText();
                var op = new ArithmOpNode(ctx.Start.Line, symbol, BinaryOperations.BitwiseOrPrimitive);
                return new ArithmExprNode(ctx.Start.Line, left, op, right);
            } else {
                return this.Visit(ctx.exclusiveOrExpression());
            }
        }

        public override ASTNode VisitExclusiveOrExpression([NotNull] ExclusiveOrExpressionContext ctx)
        {
            if (ctx.ChildCount > 1) {
                ExprNode left = this.Visit(ctx.exclusiveOrExpression()).As<ExprNode>();
                ExprNode right = this.Visit(ctx.andExpression()).As<ExprNode>();
                string symbol = ctx.children[1].GetText();
                var op = new ArithmOpNode(ctx.Start.Line, symbol, BinaryOperations.BitwiseXorPrimitive);
                return new ArithmExprNode(ctx.Start.Line, left, op, right);
            } else {
                return this.Visit(ctx.andExpression());
            }
        }

        public override ASTNode VisitAndExpression([NotNull] AndExpressionContext ctx)
        {
            if (ctx.ChildCount > 1) {
                ExprNode left = this.Visit(ctx.andExpression()).As<ExprNode>();
                ExprNode right = this.Visit(ctx.equalityExpression()).As<ExprNode>();
                string symbol = ctx.children[1].GetText();
                var op = new ArithmOpNode(ctx.Start.Line, symbol, BinaryOperations.BitwiseAndPrimitive);
                return new ArithmExprNode(ctx.Start.Line, left, op, right);
            } else {
                return this.Visit(ctx.equalityExpression());
            }
        }

        public override ASTNode VisitEqualityExpression([NotNull] EqualityExpressionContext ctx)
        {
            if (ctx.equalityExpression() is null)
                return this.Visit(ctx.relationalExpression());

            ExprNode left = this.Visit(ctx.equalityExpression()).As<ExprNode>();
            ExprNode right = this.Visit(ctx.relationalExpression()).As<ExprNode>();
            var op = RelOpNode.FromSymbol(ctx.Start.Line, ctx.children[1].GetText());
            return new RelExprNode(ctx.Start.Line, left, op, right);
        }

        public override ASTNode VisitRelationalExpression([NotNull] RelationalExpressionContext ctx)
        {
            if (ctx.relationalExpression() is null)
                return this.Visit(ctx.shiftExpression());

            ExprNode left = this.Visit(ctx.relationalExpression()).As<ExprNode>();
            ExprNode right = this.Visit(ctx.shiftExpression()).As<ExprNode>();
            var op = RelOpNode.FromSymbol(ctx.Start.Line, ctx.children[1].GetText());
            return new RelExprNode(ctx.Start.Line, left, op, right);
        }

        public override ASTNode VisitShiftExpression([NotNull] ShiftExpressionContext ctx)
        {
            if (ctx.ChildCount > 1) {
                ExprNode left = this.Visit(ctx.shiftExpression()).As<ExprNode>();
                ExprNode right = this.Visit(ctx.additiveExpression()).As<ExprNode>();
                var op = ArithmOpNode.FromSymbol(ctx.Start.Line, ctx.children[1].GetText());
                return new ArithmExprNode(ctx.Start.Line, left, op, right);
            } else {
                return this.Visit(ctx.additiveExpression());
            }
        }

        public override ASTNode VisitAdditiveExpression([NotNull] AdditiveExpressionContext ctx)
        {
            if (ctx.ChildCount > 1) {
                ExprNode left = this.Visit(ctx.additiveExpression()).As<ExprNode>();
                ExprNode right = this.Visit(ctx.multiplicativeExpression()).As<ExprNode>();
                var op = ArithmOpNode.FromSymbol(ctx.Start.Line, ctx.children[1].GetText());
                return new ArithmExprNode(ctx.Start.Line, left, op, right);
            } else {
                return this.Visit(ctx.multiplicativeExpression());
            }
        }

        public override ASTNode VisitMultiplicativeExpression([NotNull] MultiplicativeExpressionContext ctx)
        {
            if (ctx.ChildCount > 1) {
                ExprNode left = this.Visit(ctx.multiplicativeExpression()).As<ExprNode>();
                ExprNode right = this.Visit(ctx.castExpression()).As<ExprNode>();
                var op = ArithmOpNode.FromSymbol(ctx.Start.Line, ctx.children[1].GetText());
                return new ArithmExprNode(ctx.Start.Line, left, op, right);
            } else {
                return this.Visit(ctx.castExpression());
            }
        }

        public override ASTNode VisitCastExpression([NotNull] CastExpressionContext ctx)
        {
            if (ctx.unaryExpression() is null)
                throw new NotImplementedException("cast operator");
            return this.Visit(ctx.unaryExpression());
        }

        public override ASTNode VisitUnaryExpression([NotNull] UnaryExpressionContext ctx)
        {
            if (ctx.postfixExpression() is not null)
                return this.Visit(ctx.postfixExpression());

            ExprNode expr;
            if (ctx.unaryExpression() is null) {
                if (ctx.castExpression() is null)
                    throw new NotImplementedException("extended unary expressions (sizeof, alignof, gcc extensions");
                expr = this.Visit(ctx.castExpression()).As<ExprNode>();
            } else {
                expr = this.Visit(ctx.unaryExpression()).As<ExprNode>();
            }
            var op = UnaryOpNode.FromSymbol(ctx.Start.Line, ctx.children[0].GetText());
            return new UnaryExprNode(ctx.Start.Line, op, expr);
        }

        public override ASTNode VisitPostfixExpression([NotNull] PostfixExpressionContext ctx)
        {
            if (ctx.primaryExpression() is not null)
                return this.Visit(ctx.primaryExpression());

            if (ctx.typeName() is not null || ctx.initializerList() is not null)
                throw new NotImplementedException("initializers");

            ExprNode expr = this.Visit(ctx.postfixExpression()).As<ExprNode>();
            switch (ctx.children[1].GetText()) {
                case "(":
                    if (expr is IdNode fname) {
                        if (ctx.argumentExpressionList() is not null) {
                            ExprListNode? args = this.Visit(ctx.argumentExpressionList()).As<ExprListNode>();
                            return new FuncCallExprNode(ctx.Start.Line, fname, args);
                        } else {
                            return new FuncCallExprNode(ctx.Start.Line, fname);
                        }
                    } else {
                        throw new NotImplementedException("complex function calls");
                    }
                case "[":
                    ExprNode indexExpr = this.Visit(ctx.expression()).As<ExprNode>();
                    return new ArrAccessExprNode(ctx.Start.Line, expr, indexExpr);
                case "++":
                    return new IncExprNode(ctx.Start.Line, expr);
                case "--":
                    return new DecExprNode(ctx.Start.Line, expr);
                case "->":
                    throw new NotImplementedException("->");
                case ".":
                    throw new NotImplementedException("struct field access");
                default:
                    throw new SyntaxErrorException("Unknown postfix expression", ctx.Start.Line, ctx.Start.Column);
            }
        }

        public override ASTNode VisitPrimaryExpression([NotNull] PrimaryExpressionContext ctx)
        {
            if (ctx.Identifier() is not null) {
                string name = ctx.Identifier().GetText();
                if (name.Equals("null", StringComparison.InvariantCultureIgnoreCase))
                    return new NullLitExprNode(ctx.Start.Line);
                return new IdNode(ctx.Start.Line, name);
            }

            if (ctx.Constant() is not null)
                return LitExprNode.FromString(ctx.Start.Line, ctx.Constant().GetText());

            if (ctx.expression() is not null)
                return this.Visit(ctx.expression());

            if (ctx.StringLiteral() is not null)
                return new LitExprNode(ctx.Start.Line, string.Join("", ctx.StringLiteral().Select(t => t.GetText()[1..^1])));

            throw new NotImplementedException("__*");
        }

        public override ASTNode VisitArgumentExpressionList([NotNull] ArgumentExpressionListContext ctx)
        {
            ExprListNode args;
            ExprNode arg = this.Visit(ctx.assignmentExpression()).As<ExprNode>();

            if (ctx.argumentExpressionList() is null)
                return new ExprListNode(ctx.Start.Line, arg);

            args = this.Visit(ctx.argumentExpressionList()).As<ExprListNode>();
            arg.Parent = args;
            return new ExprListNode(ctx.Start.Line, args.Expressions.Concat(new[] { arg }));
        }
    }
}
