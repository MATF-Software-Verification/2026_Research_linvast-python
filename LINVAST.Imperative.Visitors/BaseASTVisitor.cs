using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Visitors
{
    public abstract class BaseASTVisitor<TResult>
    {
        private const int MAX_ITER = 1000;
        private int repeatCount = 0;

        public virtual TResult VisitChildren(ASTNode node)
        {
            TResult result = this.DefaultResult;
            foreach (ASTNode child in node.Children) {
                if (!this.ShouldVisitNextChild(node, result))
                    break;
                TResult childResult = this.Visit(child);
                result = this.AggregateResult(result, childResult);
            }
            return result;
        }

        public virtual TResult Visit(ASTNode node)
        {
            repeatCount++;
            if (repeatCount > MAX_ITER)
                throw new EvaluationException($"Evaluator reached {MAX_ITER} iterations. Perhaps a specialization for this node type does not exist?");
            return this.Visit((dynamic)node);
        }

        public virtual TResult Visit(ArithmExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(ArithmOpNode node) => this.VisitChildren(node);
        public virtual TResult Visit(ArrAccessExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(ArrDeclNode node) => this.VisitChildren(node);
        public virtual TResult Visit(ArrInitExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(AssignExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(AssignOpNode node) => this.VisitChildren(node);
        public virtual TResult Visit(BinaryLogicOpNode node) => this.VisitChildren(node);
        public virtual TResult Visit(BlockStatNode node) => this.VisitChildren(node);
        public virtual TResult Visit(CondExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(DeclSpecsNode node) => this.VisitChildren(node);
        public virtual TResult Visit(DeclStatNode node) => this.VisitChildren(node);
        public virtual TResult Visit(DeclListNode node) => this.VisitChildren(node);
        public virtual TResult Visit(DecExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(DictDeclNode node) => this.VisitChildren(node);
        public virtual TResult Visit(DictEntryNode node) => this.VisitChildren(node);
        public virtual TResult Visit(DictInitNode node) => this.VisitChildren(node);
        public virtual TResult Visit(EmptyStatNode node) => this.VisitChildren(node);
        public virtual TResult Visit(ExprListNode node) => this.VisitChildren(node);
        public virtual TResult Visit(ExprStatNode node) => this.VisitChildren(node);
        public virtual TResult Visit(ForStatNode node) => this.VisitChildren(node);
        public virtual TResult Visit(FuncCallExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(FuncDeclNode node) => this.VisitChildren(node);
        public virtual TResult Visit(FuncNode node) => this.VisitChildren(node);
        public virtual TResult Visit(FuncParamNode node) => this.VisitChildren(node);
        public virtual TResult Visit(FuncParamsNode node) => this.VisitChildren(node);
        public virtual TResult Visit(IdNode node) => this.VisitChildren(node);
        public virtual TResult Visit(IdListNode node) => this.VisitChildren(node);
        public virtual TResult Visit(IfStatNode node) => this.VisitChildren(node);
        public virtual TResult Visit(IncExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(JumpStatNode node) => this.VisitChildren(node);
        public virtual TResult Visit(LabeledStatNode node) => this.VisitChildren(node);
        public virtual TResult Visit(LambdaFuncExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(LitExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(LogicExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(NullLitExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(RelExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(RelOpNode node) => this.VisitChildren(node);
        public virtual TResult Visit(ThrowStatNode node) => this.VisitChildren(node);
        public virtual TResult Visit(SourceNode node) => this.VisitChildren(node);
        public virtual TResult Visit(UnaryExprNode node) => this.VisitChildren(node);
        public virtual TResult Visit(UnaryOpNode node) => this.VisitChildren(node);
        public virtual TResult Visit(VarDeclNode node) => this.VisitChildren(node);
        public virtual TResult Visit(WhileStatNode node) => this.VisitChildren(node);

        protected virtual TResult DefaultResult => default!;
        protected virtual TResult AggregateResult(TResult aggregate, TResult nextResult) => nextResult;
        protected virtual bool ShouldVisitNextChild(ASTNode node, TResult currentResult) => true;
    }
}
