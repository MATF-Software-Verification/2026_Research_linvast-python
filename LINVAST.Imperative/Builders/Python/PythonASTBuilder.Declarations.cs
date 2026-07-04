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
                    new ImportNode(ctx.Start.Line, modulePath.EndsWith('.') ? modulePath + "*" : modulePath + ".*"));

            var imports = ctx.import_as_names().import_as_name()
                .Select(ian =>
                {
                    ImportNode baseImport = this.Visit(ian).As<ImportNode>();
                    string sep = modulePath.EndsWith('.') ? "" : ".";
                    return new ImportNode(ian.Start.Line, modulePath + sep + baseImport.Directive, baseImport.QualifiedAs);
                });

            return new ImportListNode(ctx.Start.Line, imports);
        }

        // import_as_name: name ('as' name)?
        public override ASTNode VisitImport_as_name(Python3Parser.Import_as_nameContext ctx)
        {
            var names = ctx.name();
            string directive = names[0].GetText();
            string? alias = names.Length > 1 ? names[1].GetText() : null;
            return new ImportNode(ctx.Start.Line, directive, alias);
        }

        // dotted_as_name: dotted_name ('as' name)?
        public override ASTNode VisitDotted_as_name(Python3Parser.Dotted_as_nameContext ctx)
        {
            string directive = ctx.dotted_name().GetText();
            string? alias = ctx.name() is not null ? ctx.name().GetText() : null;
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
                ExprNode target = this.Visit(ctx.testlist_star_expr()[0]).As<ExprNode>();
                Python3Parser.AnnassignContext ann = ctx.annassign();
                string typeName = ann.test()[0].GetText();

                if (target is not IdNode idTarget) {
                    ExprNode? exprInitializer = ann.test().Length > 1
                        ? this.Visit(ann.test()[1]).As<ExprNode>()
                        : null;
                    if (exprInitializer is not null) {
                        if (this.TryCreateTypedTupleUnpackingDeclaration(target, exprInitializer, ctx.Start.Line, typeName, out DeclStatNode? tupleDecl))
                            return tupleDecl!;
                        return new ExprStatNode(ctx.Start.Line, new AssignExprNode(ctx.Start.Line, target, exprInitializer));
                    }
                    return new ExprStatNode(ctx.Start.Line, target);
                }

                ExprNode? initializer = ann.test().Length > 1
                    ? this.Visit(ann.test()[1]).As<ExprNode>()
                    : null;
                DeclStatNode declaration = MakeVarDecl(ctx.Start.Line, idTarget, initializer, typeName);
                IReadOnlyList<PendingComprehension> comprehensions = this.TakePendingComprehensions(0);
                return comprehensions.Count == 0
                    ? declaration
                    : HoistComprehensionsBefore(ctx.Start.Line, comprehensions, declaration);
            }

            if (ctx.augassign() is not null) {
                int line = ctx.Start.Line;
                ExprNode target = this.Visit(ctx.testlist_star_expr()[0]).As<ExprNode>();
                int mark = this.MarkPendingComprehensions();
                Python3Parser.TestlistContext testlistCtx = ctx.testlist();
                ExprNode value = testlistCtx is not null
                    ? this.Visit(testlistCtx).As<ExprNode>()
                    : this.Visit(ctx.yield_expr(0)).As<ExprNode>();
                IReadOnlyList<PendingComprehension> comprehensions = this.TakePendingComprehensions(mark);

                string augSymbol = ctx.augassign().GetText();
                if (augSymbol is "**=" or "//=" or "@=") {
                    // Re-visit for an independent instance; ASTNode re-parents any shared child.
                    ExprNode targetCopy = this.Visit(ctx.testlist_star_expr()[0]).As<ExprNode>();
                    ExprNode rhsCall = augSymbol switch
                    {
                        "**=" => new FuncCallExprNode(line, new IdNode(line, "pow"), new ExprListNode(line, new[] { targetCopy, value })),
                        "//=" => new FuncCallExprNode(line, new IdNode(line, "floor"),
                            new ExprListNode(line, new[] { (ExprNode)new ArithmExprNode(line, targetCopy, ArithmOpNode.FromSymbol(line, "/"), value) })),
                        _ => new FuncCallExprNode(line, new IdNode(line, "matmul"), new ExprListNode(line, new[] { targetCopy, value })),
                    };
                    var specialStatement = new ExprStatNode(line, new AssignExprNode(line, target, rhsCall));
                    return comprehensions.Count == 0
                        ? specialStatement
                        : HoistComprehensionsBefore(line, comprehensions, specialStatement);
                }

                AssignOpNode op = AssignOpNode.FromSymbol(ctx.augassign().Start.Line, augSymbol);
                var assignExpr = new AssignExprNode(line, target, op, value);
                var statement = new ExprStatNode(line, assignExpr);
                return comprehensions.Count == 0
                    ? statement
                    : HoistComprehensionsBefore(line, comprehensions, statement);
            }

            var assignTokens = ctx.children
                .Where(c => c.GetText() == "=")
                .ToList();

            if (assignTokens.Any()) {
                var allExprs = ctx.children
                    .Where(c => c is ParserRuleContext)
                    .ToList();

                int mark = this.MarkPendingComprehensions();
                ExprNode rhs = this.Visit(allExprs.Last()).As<ExprNode>();
                IReadOnlyList<PendingComprehension> comprehensions = this.TakePendingComprehensions(mark);

                var assignments = new List<ASTNode>();
                bool isSingleAssignment = allExprs.Count == 2;
                ExprNode firstLhs = this.Visit(allExprs[0]).As<ExprNode>();
                if (isSingleAssignment
                    && firstLhs is IdNode directTarget
                    && rhs is IdNode rhsId
                    && comprehensions.Count == 1
                    && comprehensions[0].AccumulatorName == rhsId.Identifier) {
                    return ExpandComprehensionAssignment(ctx.Start.Line, directTarget, comprehensions[0]);
                }

                assignments.AddRange(HoistedStatements(comprehensions));
                for (int i = 0; i < allExprs.Count - 1; i++) {
                    ExprNode lhs = i == 0 ? firstLhs : this.Visit(allExprs[i]).As<ExprNode>();
                    var assignExpr = new AssignExprNode(ctx.Start.Line, lhs, rhs);
                    assignments.Add(new ExprStatNode(ctx.Start.Line, assignExpr));
                }

                if (assignments.Count == 1)
                    return assignments[0];
                return new BlockStatNode(ctx.Start.Line, assignments);
            }

            int expressionMark = this.MarkPendingComprehensions();
            ExprNode expression = this.Visit(ctx.testlist_star_expr()[0]).As<ExprNode>();
            var expressionStatement = new ExprStatNode(ctx.Start.Line, expression);
            IReadOnlyList<PendingComprehension> expressionComprehensions = this.TakePendingComprehensions(expressionMark);
            return expressionComprehensions.Count == 0
                ? expressionStatement
                : HoistComprehensionsBefore(ctx.Start.Line, expressionComprehensions, expressionStatement);
        }

        private static BlockStatNode ExpandComprehensionAssignment(
            int line,
            IdNode target,
            PendingComprehension comprehension)
        {
            return new BlockStatNode(
                line,
                comprehension.Expansion.Children
                    .Cast<StatNode>()
                    .Select(stat => ReplaceAccumulator(stat, comprehension.AccumulatorName, target.Identifier)));
        }

        private static StatNode ReplaceAccumulator(StatNode stat, string accumulator, string target)
        {
            switch (stat) {
                case ExprStatNode exprStat:
                    return new ExprStatNode(exprStat.Line, ReplaceAccumulator(exprStat.Expression, accumulator, target));
                case BlockStatNode block:
                    return new BlockStatNode(
                        block.Line,
                        block.Children.Select(child => ReplaceAccumulator(child.As<StatNode>(), accumulator, target)));
                case IfStatNode ifStat:
                    ExprNode cond = ReplaceAccumulator(ifStat.Condition, accumulator, target);
                    StatNode thenStat = ReplaceAccumulator(ifStat.ThenStat, accumulator, target);
                    return ifStat.ElseStat is null
                        ? new IfStatNode(ifStat.Line, cond, thenStat)
                        : new IfStatNode(ifStat.Line, cond, thenStat, ReplaceAccumulator(ifStat.ElseStat, accumulator, target));
                case ForStatNode forStat:
                    return new ForStatNode(
                        forStat.Line,
                        forStat.ForDeclaration!.Copy().As<DeclarationNode>(),
                        ReplaceAccumulator(forStat.Condition, accumulator, target),
                        forStat.IncrExpr is null ? null : ReplaceAccumulator(forStat.IncrExpr, accumulator, target),
                        ReplaceAccumulator(forStat.Statement, accumulator, target));
                case AsyncStatNode asyncStat:
                    return new AsyncStatNode(asyncStat.Line, ReplaceAccumulator(asyncStat.Statement, accumulator, target));
                default:
                    return stat.Copy().As<StatNode>();
            }
        }

        private static ExprNode ReplaceAccumulator(ExprNode expr, string accumulator, string target)
        {
            switch (expr) {
                case IdNode id:
                    if (id.Identifier == accumulator)
                        return new IdNode(id.Line, target);
                    if (id.Identifier.StartsWith(accumulator + "."))
                        return new IdNode(id.Line, target + id.Identifier.Substring(accumulator.Length));
                    return new IdNode(id.Line, id.Identifier);
                case FuncCallExprNode call:
                    return new FuncCallExprNode(
                        call.Line,
                        ReplaceAccumulator(call.Children[0].As<ExprNode>(), accumulator, target).As<IdNode>(),
                        new ExprListNode(
                            call.Line,
                            call.Arguments?.Expressions.Select(arg => ReplaceAccumulator(arg, accumulator, target))
                                ?? Enumerable.Empty<ExprNode>()));
                case AssignExprNode assign:
                    return new AssignExprNode(
                        assign.Line,
                        ReplaceAccumulator(assign.LeftOperand, accumulator, target),
                        AssignOpNode.FromSymbol(assign.Operator.Line, assign.Operator.Symbol),
                        ReplaceAccumulator(assign.RightOperand, accumulator, target));
                case ArrAccessExprNode access:
                    return new ArrAccessExprNode(
                        access.Line,
                        ReplaceAccumulator(access.Array, accumulator, target),
                        ReplaceAccumulator(access.IndexExpression, accumulator, target));
                case ArrInitExprNode arr:
                    return new ArrInitExprNode(
                        arr.Line,
                        arr.Initializers.Select(init => ReplaceAccumulator(init, accumulator, target)));
                case DictInitNode dict:
                    return new DictInitNode(
                        dict.Line,
                        dict.Entries.Select(entry => ReplaceAccumulator(entry, accumulator, target).As<DictEntryNode>()));
                case DictEntryNode entry:
                    return new DictEntryNode(
                        entry.Line,
                        ReplaceAccumulator(entry.Key, accumulator, target).As<IdNode>(),
                        ReplaceAccumulator(entry.Value, accumulator, target));
                case ExprListNode list:
                    return new ExprListNode(
                        list.Line,
                        list.Expressions.Select(item => ReplaceAccumulator(item, accumulator, target)));
                default:
                    return expr.Copy().As<ExprNode>();
            }
        }

        public override ASTNode VisitAnnassign(Python3Parser.AnnassignContext ctx) =>
            throw new SyntaxErrorException("annassign should be handled by VisitExpr_stmt");

        public override ASTNode VisitAugassign(Python3Parser.AugassignContext ctx) =>
            throw new SyntaxErrorException("augassign should be handled by VisitExpr_stmt");

        // del_stmt: 'del' exprlist
        public override ASTNode VisitDel_stmt(Python3Parser.Del_stmtContext ctx)
        {
            ASTNode visited = this.Visit(ctx.exprlist());

            if (visited is ExprListNode exprList)
                return new DeleteStatNode(ctx.Start.Line, exprList.Expressions);

            return new DeleteStatNode(ctx.Start.Line, visited.As<ExprNode>());
        }

        // global_stmt: 'global' name (',' name)*
        public override ASTNode VisitGlobal_stmt(Python3Parser.Global_stmtContext ctx) =>
            new GlobalStatNode(ctx.Start.Line,
                ctx.name().Select(n => new IdNode(n.Start.Line, n.GetText())));

        // nonlocal_stmt: 'nonlocal' name (',' name)*
        public override ASTNode VisitNonlocal_stmt(Python3Parser.Nonlocal_stmtContext ctx) =>
            new NonlocalStatNode(ctx.Start.Line,
                ctx.name().Select(n => new IdNode(n.Start.Line, n.GetText())));
    }
}
