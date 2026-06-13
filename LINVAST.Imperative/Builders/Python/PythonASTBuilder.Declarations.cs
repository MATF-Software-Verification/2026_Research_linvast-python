using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    public sealed partial class PythonASTBuilder : Python3ParserBaseVisitor<ASTNode>, IASTBuilder<Python3Parser>
    {
        // import_stmt: import_name | import_from
        public override ASTNode VisitImport_stmt(Python3Parser.Import_stmtContext ctx) =>
            this.Visit(ctx.children.Single(c => c is ParserRuleContext));

        // import_name: 'import' dotted_as_names
        public override ASTNode VisitImport_name(Python3Parser.Import_nameContext ctx) =>
            this.Visit(ctx.dotted_as_names());

        // import_from: 'from' ... 'import' ...
        public override ASTNode VisitImport_from(Python3Parser.Import_fromContext ctx)
        {
            string leadingDots = string.Concat(ctx.children
                .TakeWhile(c => c.GetText() != "import" && c is not Python3Parser.Dotted_nameContext)
                .Where(c => c.GetText() == "." || c.GetText() == "...")
                .Select(c => c.GetText()));
            string modulePath = ctx.dotted_name() is not null
                ? leadingDots + ctx.dotted_name().GetText()
                : leadingDots;

            if (ctx.STAR() is not null)
                return new ImportListNode(ctx.Start.Line,
                    new ImportNode(ctx.Start.Line, modulePath + ".*"));

            var imports = ctx.import_as_names().import_as_name()
                .Select(ian =>
                {
                    ImportNode baseImport = this.Visit(ian).As<ImportNode>();
                    return new ImportNode(ian.Start.Line, modulePath + "." + baseImport.Directive, baseImport.QualifiedAs);
                });

            return new ImportListNode(ctx.Start.Line, imports);
        }

        // import_as_name: name ('as' name)?
        public override ASTNode VisitImport_as_name(Python3Parser.Import_as_nameContext ctx)
        {
            var names = ctx.name();
            string directive = names[0].GetText();
            string alias = names.Length > 1 ? names[1].GetText() : null;
            return new ImportNode(ctx.Start.Line, directive, alias);
        }

        // dotted_as_name: dotted_name ('as' name)?
        public override ASTNode VisitDotted_as_name(Python3Parser.Dotted_as_nameContext ctx)
        {
            string directive = ctx.dotted_name().GetText();
            string alias = ctx.name() is not null ? ctx.name().GetText() : null;
            return new ImportNode(ctx.Start.Line, directive, alias);
        }

        // import_as_names: import_as_name (',' import_as_name)* ','?
        public override ASTNode VisitImport_as_names(Python3Parser.Import_as_namesContext ctx) =>
            new ImportListNode(ctx.Start.Line,
                ctx.import_as_name().Select(ian => this.Visit(ian).As<ImportNode>()));

        // dotted_as_names: dotted_as_name (',' dotted_as_name)*
        public override ASTNode VisitDotted_as_names(Python3Parser.Dotted_as_namesContext ctx) =>
            new ImportListNode(ctx.Start.Line,
                ctx.dotted_as_name().Select(dan => this.Visit(dan).As<ImportNode>()));

        // dotted_name: name ('.' name)*
        public override ASTNode VisitDotted_name(Python3Parser.Dotted_nameContext ctx) =>
            new IdNode(ctx.Start.Line, ctx.GetText());

        public override ASTNode VisitExpr_stmt(Python3Parser.Expr_stmtContext ctx)
        {
            if (ctx.annassign() is not null) {
                ExprNode target = this.Visit(ctx.testlist_star_expr()).As<ExprNode>();
                Python3Parser.AnnassignContext ann = ctx.annassign();
                string typeName = ann.test()[0].GetText();

                if (target is not IdNode idTarget)
                    return new ExprStatNode(ctx.Start.Line, target);

                ExprNode initializer = ann.test().Length > 1
                    ? this.Visit(ann.test()[1]).As<ExprNode>()
                    : null;
                return MakeVarDecl(ctx.Start.Line, idTarget, initializer, typeName);
            }

            if (ctx.augassign() is not null) {
                ExprNode target = this.Visit(ctx.testlist_star_expr()).As<ExprNode>();
                AssignOpNode op = AssignOpNode.FromSymbol(ctx.augassign().Start.Line, ctx.augassign().GetText());
                Python3Parser.TestlistContext testlistCtx = ctx.testlist();
                ExprNode value = testlistCtx is not null
                    ? this.Visit(testlistCtx).As<ExprNode>()
                    : this.Visit(ctx.yield_expr(0)).As<ExprNode>();
                var assignExpr = new AssignExprNode(ctx.Start.Line, target, op, value);
                return new ExprStatNode(ctx.Start.Line, assignExpr);
            }

            var assignTokens = ctx.children
                .Where(c => c.GetText() == "=")
                .ToList();

            if (assignTokens.Any()) {
                var allExprs = ctx.children
                    .Where(c => c is ParserRuleContext)
                    .ToList();

                ExprNode rhs = this.Visit(allExprs.Last()).As<ExprNode>();

                var assignments = new List<ASTNode>();
                for (int i = 0; i < allExprs.Count - 1; i++) {
                    ExprNode lhs = this.Visit(allExprs[i]).As<ExprNode>();
                    var assignExpr = new AssignExprNode(ctx.Start.Line, lhs, rhs);
                    assignments.Add(new ExprStatNode(ctx.Start.Line, assignExpr));
                }

                if (assignments.Count == 1)
                    return assignments[0];
                return new BlockStatNode(ctx.Start.Line, assignments);
            }

            return new ExprStatNode(ctx.Start.Line,
                this.Visit(ctx.testlist_star_expr()).As<ExprNode>());
        }

        public override ASTNode VisitAnnassign(Python3Parser.AnnassignContext ctx) =>
            throw new SyntaxErrorException("annassign should be handled by VisitExpr_stmt");

        public override ASTNode VisitAugassign(Python3Parser.AugassignContext ctx) =>
            throw new SyntaxErrorException("augassign should be handled by VisitExpr_stmt");

        // del_stmt: 'del' exprlist
        public override ASTNode VisitDel_stmt(Python3Parser.Del_stmtContext ctx) =>
            throw new NotImplementedException("del statement is not supported");

        // global_stmt: 'global' name (',' name)*
        public override ASTNode VisitGlobal_stmt(Python3Parser.Global_stmtContext ctx) =>
            new EmptyStatNode(ctx.Start.Line);

        // nonlocal_stmt: 'nonlocal' name (',' name)*
        public override ASTNode VisitNonlocal_stmt(Python3Parser.Nonlocal_stmtContext ctx) =>
            new EmptyStatNode(ctx.Start.Line);
    }
}
