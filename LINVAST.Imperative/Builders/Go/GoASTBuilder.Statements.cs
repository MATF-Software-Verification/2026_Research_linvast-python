using System;
using System.Collections.Generic;
using System.Linq;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using Serilog;

namespace LINVAST.Imperative.Builders.Go
{
    public sealed partial class GoASTBuilder : GoParserBaseVisitor<ASTNode>, IASTBuilder<GoParser>
    {
        public override ASTNode VisitStatement(GoParser.StatementContext context) =>
            this.Visit(context.children.Single());
        
        public override ASTNode VisitSimpleStmt(GoParser.SimpleStmtContext context) => 
            this.Visit(context.children.Single());
        
        public override ASTNode VisitBlock(GoParser.BlockContext context) =>
            context.statementList() is null
                ? new BlockStatNode(context.Start.Line)
                : this.Visit(context.statementList());

        public override ASTNode VisitStatementList(GoParser.StatementListContext context)
        {
            if (context.statement() is null)
                return new BlockStatNode(context.Start.Line);

            IEnumerable<ASTNode> stmts = context.statement().Select(this.Visit);
            return new BlockStatNode(context.Start.Line, stmts);
        }

        public override ASTNode VisitAssignment(GoParser.AssignmentContext context)
        {
            GoParser.ExpressionListContext[]? exprs = context.expressionList();
            if (exprs is null) {
                return new EmptyStatNode(context.Start.Line);
            }

            AssignOpNode opNode = this.Visit(context.assign_op()).As<AssignOpNode>();
                
            GoParser.ExpressionListContext[] lhs = exprs[..(exprs.Length / 2)];
            GoParser.ExpressionListContext[] rhs = exprs[(exprs.Length / 2)..];

            var assignments = new ASTNode[lhs.Length];
            for (int i = 0; i < assignments.Length; i++) {
                var assignExpr = new AssignExprNode(context.Start.Line, this.Visit(lhs[i]).As<ExprNode>(), opNode,
                    this.Visit(rhs[i]).As<ExprNode>());
                assignments[i] = new ExprStatNode(context.Start.Line, assignExpr);
            }

            return new BlockStatNode(context.Start.Line, assignments); // this is very hacky; todo multi-assignment statement
        }

        public override ASTNode VisitAssign_op(GoParser.Assign_opContext context) => 
            AssignOpNode.FromSymbol(context.Start.Line, context.GetText()); // this should cover most of the cases

        public override ASTNode VisitBreakStmt(GoParser.BreakStmtContext context)  =>
            throw new NotImplementedException("break statement is unsupported");

        public override ASTNode VisitContinueStmt(GoParser.ContinueStmtContext context)  =>
            throw new NotImplementedException("continue statement is unsupported");

        public override ASTNode VisitEmptyStmt(GoParser.EmptyStmtContext context) =>
            new EmptyStatNode(context.Start.Line);

        public override ASTNode VisitForStmt(GoParser.ForStmtContext context)
        {
            BlockStatNode body = this.Visit(context.block()).As<BlockStatNode>();
            if (context.expression() is not null) {
                // while loop
                ExprNode expr = this.Visit(context.expression()).As<ExprNode>();
                return new WhileStatNode(context.Start.Line, expr, body);
            }

            if (context.forClause() is not null) {
                // traditional for loop
                GoParser.ForClauseContext clauseContext = context.forClause();
                
                // todo ForStatNode requires expressions for init and incr, but we have statements
                throw new NotImplementedException("For loops with ForClause are unsupported");
            }

            throw new NotImplementedException("Unsupported for statement form");
        }

        // public override ASTNode VisitForClause(GoParser.ForClauseContext context) => base.VisitForClause(context);

        public override ASTNode VisitRangeClause(GoParser.RangeClauseContext context) =>
            throw new NotImplementedException("range clause in for statement is unsupported");
        
        public override ASTNode VisitGotoStmt(GoParser.GotoStmtContext context) => 
            new JumpStatNode(context.Start.Line, new IdNode(context.Start.Line, context.IDENTIFIER().GetText()));

        public override ASTNode VisitIfStmt(GoParser.IfStmtContext context)
        {
            if (context.simpleStmt() is not null) {
                throw new NotImplementedException("Preceding statements in if statements are not supported");
            }

            ExprNode cond = this.Visit(context.expression()).As<ExprNode>();
            BlockStatNode body = this.Visit(context.block()[0]).As<BlockStatNode>();
            
            StatNode? elseStmt = null;
            if (context.ifStmt() is not null) {
                elseStmt = this.Visit(context.ifStmt()).As<IfStatNode>();
            }

            if (context.block().Length > 1) {
                elseStmt = this.Visit(context.block()[1]).As<BlockStatNode>();
            }

            return elseStmt is null ? 
                new IfStatNode(context.Start.Line, cond, body) : 
                new IfStatNode(context.Start.Line, cond, body, elseStmt);
        }

        public override ASTNode VisitLabeledStmt(GoParser.LabeledStmtContext context)
        {
            string label = context.IDENTIFIER().GetText();
            StatNode statement = this.Visit(context.statement()).As<StatNode>();
            return new LabeledStatNode(context.Start.Line, label, statement);
        }

        public override ASTNode VisitReturnStmt(GoParser.ReturnStmtContext context)
        {
            var exprList = this.Visit(context.expressionList()).As<ExprListNode>();
            return new JumpStatNode(context.Start.Line, exprList);
        }

        public override ASTNode VisitExpressionStmt(GoParser.ExpressionStmtContext context) => 
            new ExprStatNode(context.Start.Line, this.Visit(context.expression()).As<ExprNode>());

        public override ASTNode VisitIncDecStmt(GoParser.IncDecStmtContext context)
        {
            ExprNode exprNode = this.Visit(context.expression()).As<ExprNode>();
            if (context.PLUS_PLUS() is not null) {
                var incExpr = new IncExprNode(context.Start.Line, exprNode);
                return new ExprStatNode(context.Start.Line, incExpr);
            }
            if (context.MINUS_MINUS() is not null) {
                var decExpr = new DecExprNode(context.Start.Line, exprNode);
                return new ExprStatNode(context.Start.Line, decExpr);
            }

            throw new Exception("Invalid IncDecStmtContext: " + context);
        }
        
        public override ASTNode VisitGoStmt(GoParser.GoStmtContext context) =>
            throw new NotImplementedException("go statement is unsupported");
        
        public override ASTNode VisitDeferStmt(GoParser.DeferStmtContext context) =>
            throw new NotImplementedException("defer statement is unsupported");
        
        public override ASTNode VisitSelectStmt(GoParser.SelectStmtContext context) =>
            throw new NotImplementedException("select statement is unsupported");

        public override ASTNode VisitCommCase(GoParser.CommCaseContext context) =>
         throw new NotImplementedException("select statement (comm case) is unsupported");

        public override ASTNode VisitCommClause(GoParser.CommClauseContext context) =>
            throw new NotImplementedException("select statement (comm clause) is unsupported");
        
        public override ASTNode VisitRecvStmt(GoParser.RecvStmtContext context) =>
            throw new NotImplementedException("select statement (recv stmt) is unsupported");

        public override ASTNode VisitSendStmt(GoParser.SendStmtContext context) =>
            throw new NotImplementedException("send statement is unsupported");

        public override ASTNode VisitSwitchStmt(GoParser.SwitchStmtContext context) =>
            throw new NotImplementedException("switch statement is unsupported");

        public override ASTNode VisitTypeSwitchStmt(GoParser.TypeSwitchStmtContext context) =>
            throw new NotImplementedException("type switch statement is unsupported");

        public override ASTNode VisitExprSwitchStmt(GoParser.ExprSwitchStmtContext context) =>
            throw new NotImplementedException("expr switch statement is unsupported");
        
        public override ASTNode VisitExprCaseClause(GoParser.ExprCaseClauseContext context) =>
            throw new NotImplementedException("switch statement is unsupported");

        public override ASTNode VisitExprSwitchCase(GoParser.ExprSwitchCaseContext context) =>
            throw new NotImplementedException("expr switch statement (case) is unsupported");

        public override ASTNode VisitTypeSwitchCase(GoParser.TypeSwitchCaseContext context) =>
            throw new NotImplementedException("type switch statement (case) is unsupported");
        
        public override ASTNode VisitTypeSwitchGuard(GoParser.TypeSwitchGuardContext context) =>
            throw new NotImplementedException("type switch statement (guard) is unsupported");

        public override ASTNode VisitTypeCaseClause(GoParser.TypeCaseClauseContext context) =>
            throw new NotImplementedException("type switch statement (case clause) is unsupported");

        public override ASTNode VisitFallthroughStmt(GoParser.FallthroughStmtContext context) =>
            throw new NotImplementedException("switch statement (fallthrough) is unsupported");
    }
}