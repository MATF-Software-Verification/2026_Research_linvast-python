using System.Linq;
using Antlr4.Runtime.Misc;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using static LINVAST.Imperative.Builders.Pseudo.PseudoParser;

namespace LINVAST.Imperative.Builders.Pseudo
{
    public sealed partial class PseudoASTBuilder : PseudoBaseVisitor<ASTNode>, IASTBuilder<PseudoParser>
    {
        public override ASTNode VisitExp([NotNull] ExpContext ctx)
        {
            if (ctx.literal() is not null || ctx.var() is not null || ctx.cexp() is not null)
                return this.Visit(ctx.children.Single());

            if (ctx.aop() is not null)
                return this.VisitArithmeticExpression(ctx.Start.Line, ctx.exp()[0], ctx.aop(), ctx.exp()[1]);

            if (ctx.rop() is not null)
                return this.VisitRelationalExpression(ctx.Start.Line, ctx.exp()[0], ctx.rop(), ctx.exp()[1]);

            if (ctx.lop() is not null)
                return this.VisitLogicExpression(ctx.Start.Line, ctx.exp()[0], ctx.lop(), ctx.exp()[1]);

            if (ctx.uop() is not null)
                return this.VisitUnaryExpression(ctx.Start.Line, ctx.uop(), ctx.exp().Single());

            return this.Visit(ctx.exp().Single());
        }

        public override ASTNode VisitVar([NotNull] VarContext ctx)
        {
            var v = new IdNode(ctx.Start.Line, ctx.NAME().GetText());
            if (ctx.iexp() is null)
                return v;

            ExprNode arrIndex = this.Visit(ctx.iexp()).As<ExprNode>();
            return new ArrAccessExprNode(ctx.Start.Line, v, arrIndex);
        }

        public override ASTNode VisitIexp([NotNull] IexpContext ctx)
            => this.Visit(ctx.children.First());

        public override ASTNode VisitLiteral([NotNull] LiteralContext ctx)
            => LitExprNode.FromString(ctx.Start.Line, ctx.children.Single().GetText());

        public override ASTNode VisitAexp([NotNull] AexpContext ctx)
            => this.VisitArithmeticExpression(ctx.Start.Line, ctx.exp()[0], ctx.aop(), ctx.exp()[1]);

        public override ASTNode VisitCexp([NotNull] CexpContext ctx)
        {
            var fname = new IdNode(ctx.Start.Line, ctx.NAME().GetText());
            if (ctx.explist() is null)
                return new FuncCallExprNode(ctx.Start.Line, fname);

            ExprListNode args = this.Visit(ctx.explist()).As<ExprListNode>();
            return new FuncCallExprNode(ctx.Start.Line, fname, args);
        }

        public override ASTNode VisitExplist([NotNull] ExplistContext ctx)
            => new ExprListNode(ctx.Start.Line, ctx.exp().Select(e => this.Visit(e).As<ExprNode>()));


        private ExprNode VisitArithmeticExpression(int line, ExpContext lexp, AopContext aop, ExpContext rexp)
        {
            ExprNode left = this.Visit(lexp).As<ExprNode>();
            var op = ArithmOpNode.FromSymbol(line, aop.GetText());
            ExprNode right = this.Visit(rexp).As<ExprNode>();
            return new ArithmExprNode(line, left, op, right);
        }

        private ExprNode VisitRelationalExpression(int line, ExpContext lexp, RopContext rop, ExpContext rexp)
        {
            ExprNode left = this.Visit(lexp).As<ExprNode>();
            var op = RelOpNode.FromSymbol(line, rop.GetText());
            ExprNode right = this.Visit(rexp).As<ExprNode>();
            return new RelExprNode(line, left, op, right);
        }

        private ExprNode VisitLogicExpression(int line, ExpContext lexp, LopContext lop, ExpContext rexp)
        {
            ExprNode left = this.Visit(lexp).As<ExprNode>();
            var op = BinaryLogicOpNode.FromSymbol(line, lop.GetText());
            ExprNode right = this.Visit(rexp).As<ExprNode>();
            return new LogicExprNode(line, left, op, right);
        }

        private ExprNode VisitUnaryExpression(int line, UopContext uop, ExpContext exp)
        {
            var op = UnaryOpNode.FromSymbol(line, uop.GetText());
            ExprNode operand = this.Visit(exp).As<ExprNode>();
            return new UnaryExprNode(line, op, operand);
        }
    }
}
