using System;
using System.Collections.Generic;
using System.Linq;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
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
            new JumpStatNode(context.Start.Line, JumpStatType.Break);

        public override ASTNode VisitContinueStmt(GoParser.ContinueStmtContext context)  =>
            new JumpStatNode(context.Start.Line, JumpStatType.Continue);

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
                GoParser.ForClauseContext clauseContext = context.forClause();
                ExprNode? init = this.SimpleStatementExpression(clauseContext.initStmt);
                ExprNode? condition = clauseContext.expression() is null
                    ? null
                    : this.Visit(clauseContext.expression()).As<ExprNode>();
                ExprNode? post = this.SimpleStatementExpression(clauseContext.postStmt);
                return new ForStatNode(context.Start.Line, init, condition, post, body);
            }

            if (context.rangeClause() is not null) {
                ExprNode range = this.Visit(context.rangeClause()).As<ExprNode>();
                return new ForStatNode(context.Start.Line, range, null, null, body);
            }

            return new ForStatNode(context.Start.Line, (ExprNode?)null, null, null, body);
        }

        public override ASTNode VisitRangeClause(GoParser.RangeClauseContext context)
        {
            ExprNode iterable = this.Visit(context.expression()).As<ExprNode>();
            IEnumerable<ExprNode> lhs = Enumerable.Empty<ExprNode>();
            if (context.expressionList() is not null) {
                lhs = this.Visit(context.expressionList()).As<ExprListNode>().Expressions;
            } else if (context.identifierList() is not null) {
                lhs = this.Visit(context.identifierList()).As<IdListNode>().Identifiers;
            }

            string mode = context.DECLARE_ASSIGN() is not null ? ":=" : "=";
            ExprNode[] args = new ExprNode[] { new IdNode(context.Start.Line, mode) }
                .Concat(lhs)
                .Append(iterable)
                .ToArray();
            return this.MarkerExpression(context.Start.Line, "__linvast_range", args);
        }
        
        public override ASTNode VisitGotoStmt(GoParser.GotoStmtContext context) => 
            new JumpStatNode(context.Start.Line, new IdNode(context.Start.Line, context.IDENTIFIER().GetText()));

        public override ASTNode VisitIfStmt(GoParser.IfStmtContext context)
        {
            ExprNode cond = this.Visit(context.expression()).As<ExprNode>();
            BlockStatNode body = this.Visit(context.block()[0]).As<BlockStatNode>();
            
            StatNode? elseStmt = null;
            if (context.ifStmt() is not null) {
                elseStmt = this.Visit(context.ifStmt()).As<IfStatNode>();
            }

            if (context.block().Length > 1) {
                elseStmt = this.Visit(context.block()[1]).As<BlockStatNode>();
            }

            IfStatNode ifStmt = elseStmt is null ?
                new IfStatNode(context.Start.Line, cond, body) :
                new IfStatNode(context.Start.Line, cond, body, elseStmt);

            if (context.simpleStmt() is null)
                return ifStmt;

            return new BlockStatNode(context.Start.Line, this.Visit(context.simpleStmt()), ifStmt);
        }

        public override ASTNode VisitLabeledStmt(GoParser.LabeledStmtContext context)
        {
            string label = context.IDENTIFIER().GetText();
            StatNode statement = context.statement() is null
                ? new EmptyStatNode(context.Start.Line)
                : this.Visit(context.statement()).As<StatNode>();
            return new LabeledStatNode(context.Start.Line, label, statement);
        }

        public override ASTNode VisitReturnStmt(GoParser.ReturnStmtContext context)
        {
            if (context.expressionList() is null)
                return new JumpStatNode(context.Start.Line, (ExprNode?)null);

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
            this.MarkerStatement(context.Start.Line, "__linvast_go", this.Visit(context.expression()).As<ExprNode>());
        
        public override ASTNode VisitDeferStmt(GoParser.DeferStmtContext context) =>
            this.MarkerStatement(context.Start.Line, "__linvast_defer", this.Visit(context.expression()).As<ExprNode>());
        
        public override ASTNode VisitSelectStmt(GoParser.SelectStmtContext context)
        {
            var body = new BlockStatNode(context.Start.Line, context.commClause().Select(this.Visit));
            return new SwitchStatNode(context.Start.Line, new LitExprNode(context.Start.Line, true), body);
        }

        public override ASTNode VisitCommCase(GoParser.CommCaseContext context) =>
            new IdNode(context.Start.Line, this.CommCaseLabel(context));

        public override ASTNode VisitCommClause(GoParser.CommClauseContext context)
        {
            BlockStatNode statements = context.statementList() is null
                ? new BlockStatNode(context.Start.Line)
                : this.Visit(context.statementList()).As<BlockStatNode>();
            return new LabeledStatNode(context.Start.Line, this.CommCaseLabel(context.commCase()), statements);
        }
        
        public override ASTNode VisitRecvStmt(GoParser.RecvStmtContext context)
        {
            ExprNode receive = this.Visit(context.recvExpr).As<ExprNode>();

            if (context.expressionList() is not null) {
                ExprListNode lhs = this.Visit(context.expressionList()).As<ExprListNode>();
                return new ExprStatNode(context.Start.Line, new AssignExprNode(context.Start.Line, lhs, receive));
            }

            if (context.identifierList() is not null) {
                IdListNode ids = this.Visit(context.identifierList()).As<IdListNode>();
                if (context.DECLARE_ASSIGN() is not null) {
                    var decls = new DeclListNode(context.Start.Line, ids.Identifiers.Select(id => new VarDeclNode(id.Line, id, receive)));
                    return new DeclStatNode(context.Start.Line, new DeclSpecsNode(context.Start.Line), decls);
                }

                return new ExprStatNode(context.Start.Line, new AssignExprNode(context.Start.Line, ids, receive));
            }

            return new ExprStatNode(context.Start.Line, receive);
        }

        public override ASTNode VisitSendStmt(GoParser.SendStmtContext context)
        {
            ExprNode channel = this.Visit(context.channel).As<ExprNode>();
            ExprNode value = this.Visit(context.expression().Last()).As<ExprNode>();
            return this.MarkerStatement(context.Start.Line, "__linvast_send", channel, value);
        }

        public override ASTNode VisitSwitchStmt(GoParser.SwitchStmtContext context) =>
            this.Visit(context.children.Single());

        public override ASTNode VisitTypeSwitchStmt(GoParser.TypeSwitchStmtContext context)
        {
            ExprNode condition = this.Visit(context.typeSwitchGuard()).As<ExprNode>();
            var body = new BlockStatNode(context.Start.Line, context.typeCaseClause().Select(this.Visit));
            var switchNode = new SwitchStatNode(context.Start.Line, condition, body);

            if (context.simpleStmt() is null)
                return switchNode;

            return new BlockStatNode(context.Start.Line, this.Visit(context.simpleStmt()), switchNode);
        }

        public override ASTNode VisitExprSwitchStmt(GoParser.ExprSwitchStmtContext context)
        {
            ExprNode condition = context.expression() is null
                ? new LitExprNode(context.Start.Line, true)
                : this.Visit(context.expression()).As<ExprNode>();
            var body = new BlockStatNode(context.Start.Line, context.exprCaseClause().Select(this.Visit));
            var switchNode = new SwitchStatNode(context.Start.Line, condition, body);

            if (context.simpleStmt() is null)
                return switchNode;

            return new BlockStatNode(context.Start.Line, this.Visit(context.simpleStmt()), switchNode);
        }
        
        public override ASTNode VisitExprCaseClause(GoParser.ExprCaseClauseContext context)
        {
            BlockStatNode statements = context.statementList() is null
                ? new BlockStatNode(context.Start.Line)
                : this.Visit(context.statementList()).As<BlockStatNode>();
            return new LabeledStatNode(context.Start.Line, this.ExprSwitchCaseLabel(context.exprSwitchCase()), statements);
        }

        public override ASTNode VisitExprSwitchCase(GoParser.ExprSwitchCaseContext context) =>
            new IdNode(context.Start.Line, this.ExprSwitchCaseLabel(context));

        public override ASTNode VisitTypeSwitchCase(GoParser.TypeSwitchCaseContext context) =>
            new IdNode(context.Start.Line, this.TypeSwitchCaseLabel(context));
        
        public override ASTNode VisitTypeSwitchGuard(GoParser.TypeSwitchGuardContext context)
        {
            ExprNode switched = this.Visit(context.primaryExpr()).As<ExprNode>();
            var args = context.IDENTIFIER() is null
                ? new ExprListNode(context.Start.Line, switched)
                : new ExprListNode(context.Start.Line, new IdNode(context.Start.Line, context.IDENTIFIER().GetText()), switched);
            return new FuncCallExprNode(context.Start.Line, new IdNode(context.Start.Line, "__linvast_type_switch"), args);
        }

        public override ASTNode VisitTypeCaseClause(GoParser.TypeCaseClauseContext context)
        {
            BlockStatNode statements = context.statementList() is null
                ? new BlockStatNode(context.Start.Line)
                : this.Visit(context.statementList()).As<BlockStatNode>();
            return new LabeledStatNode(context.Start.Line, this.TypeSwitchCaseLabel(context.typeSwitchCase()), statements);
        }

        public override ASTNode VisitFallthroughStmt(GoParser.FallthroughStmtContext context) =>
            new ExprStatNode(context.Start.Line, new FuncCallExprNode(context.Start.Line, new IdNode(context.Start.Line, "__linvast_fallthrough")));

        private string ExprSwitchCaseLabel(GoParser.ExprSwitchCaseContext context)
            => context.DEFAULT() is not null
                ? "default"
                : $"case {this.Visit(context.expressionList()).As<ExprListNode>().GetText()}";

        private string TypeSwitchCaseLabel(GoParser.TypeSwitchCaseContext context)
            => context.DEFAULT() is not null
                ? "default"
                : $"case {this.Visit(context.typeList()).As<TypeNameListNode>().GetText()}";

        private string CommCaseLabel(GoParser.CommCaseContext context)
        {
            if (context.DEFAULT() is not null)
                return "default";
            if (context.sendStmt() is not null)
                return $"case {this.VisitSendStmt(context.sendStmt()).As<ExprStatNode>().Expression.GetText()}";
            if (context.recvStmt() is not null)
                return $"case {this.VisitRecvStmt(context.recvStmt()).As<StatNode>().GetText().TrimEnd(';')}";

            return "case";
        }

        private ExprNode? SimpleStatementExpression(GoParser.SimpleStmtContext? context)
        {
            if (context is null)
                return null;

            return this.StatementExpression(this.Visit(context));
        }

        private ExprNode? StatementExpression(ASTNode? node)
        {
            if (node is null || node is EmptyStatNode)
                return null;
            if (node is ExprNode expression)
                return expression;
            if (node is ExprStatNode exprStat)
                return exprStat.Expression;
            if (node is BlockStatNode block) {
                ExprNode[] expressions = block.Children
                    .Select(this.StatementExpression)
                    .Where(e => e is not null)
                    .Cast<ExprNode>()
                    .ToArray();
                return expressions.Length switch
                {
                    0 => null,
                    1 => expressions[0],
                    _ => new ExprListNode(block.Line, expressions),
                };
            }
            if (node is StatNode stat)
                return this.MarkerExpression(stat.Line, "__linvast_stmt", new IdNode(stat.Line, stat.GetText()));

            return this.MarkerExpression(node.Line, "__linvast_node", new IdNode(node.Line, node.GetText()));
        }

        private ExprStatNode MarkerStatement(int line, string marker, params ExprNode[] args) =>
            new ExprStatNode(line, this.MarkerExpression(line, marker, args));

        private FuncCallExprNode MarkerExpression(int line, string marker, params ExprNode[] args) =>
            args.Any()
                ? new FuncCallExprNode(line, new IdNode(line, marker), new ExprListNode(line, args))
                : new FuncCallExprNode(line, new IdNode(line, marker));
    }
}
