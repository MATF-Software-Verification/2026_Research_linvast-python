using System.Linq;
using Antlr4.Runtime.Misc;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using static LINVAST.Imperative.Builders.Pseudo.PseudoParser;

namespace LINVAST.Imperative.Builders.Pseudo
{
    public sealed partial class PseudoASTBuilder : PseudoBaseVisitor<ASTNode>, IASTBuilder<PseudoParser>
    {
        public override ASTNode VisitStatement([NotNull] StatementContext ctx)
        {
            if (ctx.declaration() is not null)
                return this.Visit(ctx.declaration());

            if (ctx.assignment() is not null)
                return this.Visit(ctx.assignment());

            if (ctx.cexp() is not null)
                return this.Visit(ctx.cexp());

            switch (ctx.children.First().GetText()) {
                case "pass":
                    return new EmptyStatNode(ctx.Start.Line);
                case "return":
                    return new JumpStatNode(ctx.Start.Line, this.Visit(ctx.exp()).As<ExprNode>());
                case "error":
                    return new ThrowStatNode(ctx.Start.Line, new LitExprNode(ctx.Start.Line, ctx.STRING().GetText()));
                case "if":
                    ExprNode cond = this.Visit(ctx.exp()).As<ExprNode>();
                    BlockStatNode thenBlock = this.Visit(ctx.block().First()).As<BlockStatNode>();
                    if (ctx.block().Length > 1) {
                        BlockStatNode? elseBlock = this.Visit(ctx.block().Last()).As<BlockStatNode>();
                        return new IfStatNode(ctx.Start.Line, cond, thenBlock, elseBlock);
                    } else {
                        return new IfStatNode(ctx.Start.Line, cond, thenBlock);
                    }
                case "while":
                    ExprNode whileCond = this.Visit(ctx.exp()).As<ExprNode>();
                    BlockStatNode whileBlock = this.Visit(ctx.block().Single()).As<BlockStatNode>();
                    return new WhileStatNode(ctx.Start.Line, whileCond, whileBlock);
                case "repeat":
                    ExprNode repeatCond = this.Visit(ctx.exp()).As<ExprNode>();
                    var notOp = new UnaryOpNode(ctx.Start.Line, "not", UnaryOperations.NegatePrimitive);
                    ExprNode negatedCond = new UnaryExprNode(ctx.Start.Line, notOp, repeatCond);
                    BlockStatNode repeatBlock = this.Visit(ctx.block().Single()).As<BlockStatNode>();
                    var loop = new WhileStatNode(ctx.Start.Line, negatedCond, repeatBlock);
                    var block = new BlockStatNode(ctx.Start.Line, repeatBlock, loop);
                    return new WhileStatNode(ctx.Start.Line, repeatCond, repeatBlock);
                case "increment":
                    return new IncExprNode(ctx.Start.Line, this.Visit(ctx.var()).As<ExprNode>());
                case "decrement":
                    return new DecExprNode(ctx.Start.Line, this.Visit(ctx.var()).As<ExprNode>());
                default:
                    throw new SyntaxErrorException("Invalid statement");
            }
        }

        public override ASTNode VisitAssignment([NotNull] AssignmentContext ctx)
        {
            ExprNode left = this.Visit(ctx.var()).As<ExprNode>();
            ExprNode right = this.Visit(ctx.exp()).As<ExprNode>();
            var assignment = new AssignExprNode(ctx.Start.Line, left, right);
            return new ExprStatNode(ctx.Start.Line, assignment);
        }
    }
}
