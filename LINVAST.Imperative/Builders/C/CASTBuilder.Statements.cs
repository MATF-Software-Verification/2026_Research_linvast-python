using System;
using System.Linq;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
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
        public override ASTNode VisitCompoundStatement([NotNull] CompoundStatementContext ctx)
        {
            return ctx.blockItemList() is null
                ? new BlockStatNode(ctx.Start.Line)
                : this.Visit(ctx.blockItemList());
        }

        public override ASTNode VisitBlockItemList([NotNull] BlockItemListContext ctx)
        {
            BlockStatNode block;
            ASTNode item = this.Visit(ctx.blockItem());

            if (ctx.blockItemList() is null)
                return new BlockStatNode(ctx.Start.Line, item);

            block = this.Visit(ctx.blockItemList()).As<BlockStatNode>();
            return new BlockStatNode(ctx.Start.Line, block.Children.Concat(new[] { item }));
        }

        public override ASTNode VisitBlockItem([NotNull] BlockItemContext ctx)
            => this.Visit(ctx.children.Single());

        public override ASTNode VisitStatement([NotNull] StatementContext ctx)
            => this.Visit(ctx.children.Single());

        public override ASTNode VisitLabeledStatement([NotNull] LabeledStatementContext ctx)
        {
            if (ctx.Identifier() is null)
                throw new NotImplementedException();

            string label = ctx.Identifier().GetText();
            StatNode statement = this.Visit(ctx.statement()).As<StatNode>();
            return new LabeledStatNode(ctx.Start.Line, label, statement);
        }

        public override ASTNode VisitExpressionStatement([NotNull] ExpressionStatementContext ctx)
        {
            if (ctx.expression() is null)
                return new EmptyStatNode(ctx.Start.Line);

            ExprNode expr = this.Visit(ctx.expression()).As<ExprNode>();
            return new ExprStatNode(ctx.Start.Line, expr);
        }

        public override ASTNode VisitSelectionStatement([NotNull] SelectionStatementContext ctx)
        {
            switch (ctx.children.First().GetText()) {
                case "if":
                    ExprNode condition = this.Visit(ctx.expression()).As<ExprNode>();
                    StatementContext[] statements = ctx.statement();
                    StatNode thenStatement = this.Visit(statements.First()).As<StatNode>();
                    StatNode? elseStatement = statements.Length > 1 ? this.Visit(statements.Last()).As<StatNode>() : null;
                    return elseStatement is null
                        ? new IfStatNode(ctx.Start.Line, condition, thenStatement)
                        : new IfStatNode(ctx.Start.Line, condition, thenStatement, elseStatement);
                case "switch":
                    throw new NotImplementedException("switch");
                default:
                    throw new SyntaxErrorException("Unknown construct", ctx.Start.Line, ctx.Start.Column);
            }
        }

        public override ASTNode VisitIterationStatement([NotNull] IterationStatementContext ctx)
        {
            IterStatNode it;
            StatNode statement = this.Visit(ctx.statement()).As<StatNode>();

            if (ctx.For() is not null) {
                ForConditionContext forCondition = ctx.forCondition();
                if (forCondition.forDeclaration() is not null) {
                    DeclarationNode decl = this.Visit(forCondition.forDeclaration()).As<DeclarationNode>();
                    GetForExpressions(forCondition, out ExprNode? cond, out ExprNode? inc);
                    return new ForStatNode(ctx.Start.Line, decl, cond, inc, statement);
                } else {
                    ExprNode? initExpr = forCondition.expression() is not null ? this.Visit(forCondition.expression()).As<ExprNode>() : null;
                    GetForExpressions(forCondition, out ExprNode? cond, out ExprNode? inc);
                    return new ForStatNode(ctx.Start.Line, initExpr, cond, inc, statement);
                }
            }

            ExprNode condition = this.Visit(ctx.expression()).As<ExprNode>();
            it = new WhileStatNode(ctx.Start.Line, condition, statement);

            return ctx.Do() is not null
                ? throw new NotImplementedException("do-while")
                : it;

            void GetForExpressions(ForConditionContext fctx, out ExprNode? cond, out ExprNode? inc)
            {
                cond = null;
                inc = null;

                int colonCount = 0;
                foreach (IParseTree child in fctx.children) {
                    if (child is ITerminalNode)
                        colonCount++;
                    else if (colonCount > 1)
                        inc = this.Visit(child).As<ExprNode>();
                    else if (colonCount > 0)
                        cond = this.Visit(child).As<ExprNode>();
                }
            }
        }

        public override ASTNode VisitForExpression([NotNull] ForExpressionContext ctx)
        {
            ExprListNode exprs;
            ExprNode expr = this.Visit(ctx.assignmentExpression()).As<ExprNode>();

            if (ctx.forExpression() is null)
                return new ExprListNode(ctx.Start.Line, expr);

            exprs = this.Visit(ctx.forExpression()).As<ExprListNode>();
            expr.Parent = exprs;
            return new ExprListNode(ctx.Start.Line, exprs.Expressions.Concat(new[] { expr }));
        }

        public override ASTNode VisitJumpStatement([NotNull] JumpStatementContext ctx)
        {
            JumpStatType type = JumpStatementTypeConverter.FromString(ctx.children.First().GetText());
            switch (type) {
                case JumpStatType.Return:
                    ExprNode? expr = ctx.expression() is not null ? this.Visit(ctx.expression()).As<ExprNode>() : null;
                    return new JumpStatNode(ctx.Start.Line, expr);
                case JumpStatType.Goto:
                    var label = new IdNode(ctx.Start.Line, ctx.Identifier().GetText());
                    return new JumpStatNode(ctx.Start.Line, label);
                default:
                    return new JumpStatNode(ctx.Start.Line, type);
            }
        }
    }
}
