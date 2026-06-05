using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using static LINVAST.Imperative.Builders.Lua.LuaParser;

namespace LINVAST.Imperative.Builders.Lua
{
    public sealed partial class LuaASTBuilder : LuaBaseVisitor<ASTNode>, IASTBuilder<LuaParser>
    {
        public override ASTNode VisitStat([NotNull] StatContext ctx)
        {
            if (ctx.varlist() is not null) {
                ExprListNode vars = this.Visit(ctx.varlist()).As<ExprListNode>();
                ExprListNode inits = this.Visit(ctx.explist()).As<ExprListNode>();
                return CreateAssignmentNode(ctx.Start.Line, vars, inits);
            }

            if (ctx.functioncall() is not null)
                return this.Visit(ctx.functioncall());

            if (ctx.label() is not null)
                return new LabeledStatNode(ctx.Start.Line, ctx.label().NAME().GetText(), new EmptyStatNode(ctx.Start.Line));

            switch (ctx.children.First().GetText()) {
                case ";":
                    return new EmptyStatNode(ctx.Start.Line);
                case "break":
                    return new JumpStatNode(ctx.Start.Line, JumpStatType.Break);
                case "goto":
                    var label = new IdNode(ctx.Start.Line, ctx.NAME().GetText());
                    return new JumpStatNode(ctx.Start.Line, label);
                case "do":
                    return this.Visit(ctx.block().Single());
                case "while":
                    ExprNode cond = this.Visit(ctx.exp().Single()).As<ExprNode>();
                    BlockStatNode body = this.Visit(ctx.block().Single()).As<BlockStatNode>();
                    return new WhileStatNode(ctx.Start.Line, cond, body);
                case "repeat":
                    ExprNode until = this.Visit(ctx.exp().Single()).As<ExprNode>();
                    var negUntil = new UnaryExprNode(until.Line, UnaryOpNode.FromSymbol(until.Line, "not"), until);
                    BlockStatNode repeatBody = this.Visit(ctx.block().Single()).As<BlockStatNode>();
                    BlockStatNode loopBody = this.Visit(ctx.block().Single()).As<BlockStatNode>();
                    return new BlockStatNode(ctx.Start.Line, repeatBody, new WhileStatNode(ctx.Start.Line, negUntil, loopBody));
                case "if":
                    ExprNode[] conds = ctx.exp().Select(e => this.Visit(e).As<ExprNode>()).ToArray();
                    BlockStatNode[] blocks = ctx.block().Select(b => this.Visit(b).As<BlockStatNode>()).ToArray();

                    StatNode? @else = blocks.Length > 1 ? CreateElseIfNode(1) : null;

                    return @else is null
                        ? new IfStatNode(ctx.Start.Line, conds.First(), blocks.First())
                        : new IfStatNode(ctx.Start.Line, conds.First(), blocks.First(), @else);


                    StatNode? CreateElseIfNode(int i)
                    {
                        if (i >= conds.Length)
                            return i >= blocks.Length ? null : blocks.Last();
                        StatNode? @else = CreateElseIfNode(i + 1);
                        return @else is null
                            ? new IfStatNode(conds[i].Line, conds[i], blocks[i])
                            : new IfStatNode(conds[i].Line, conds[i], blocks[i], @else);
                    }
                case "for":
                    return ctx.namelist() is null ? CreateNumericForNode() : CreateGenericForNode();
                case "function":
                    IdNode fname = this.Visit(ctx.funcname()).As<IdNode>();
                    LambdaFuncExprNode lam = this.Visit(ctx.funcbody()).As<LambdaFuncExprNode>();
                    FuncDeclNode fdef = lam.ParametersNode is null
                        ? new FuncDeclNode(ctx.Start.Line, fname, lam.Definition)
                        : new FuncDeclNode(ctx.Start.Line, fname, lam.ParametersNode, lam.Definition);
                    var fdeclSpecs = new DeclSpecsNode(ctx.Start.Line);
                    return new FuncNode(ctx.Start.Line, fdeclSpecs, fdef);
                case "local":
                    if (ctx.children[1].GetText().Equals("function", StringComparison.InvariantCultureIgnoreCase)) {
                        var localFname = new IdNode(ctx.Start.Line, ctx.NAME().GetText());
                        LambdaFuncExprNode localLam = this.Visit(ctx.funcbody()).As<LambdaFuncExprNode>();
                        FuncDeclNode localFdef = localLam.ParametersNode is null
                            ? new FuncDeclNode(ctx.Start.Line, localFname, localLam.Definition)
                            : new FuncDeclNode(ctx.Start.Line, localFname, localLam.ParametersNode, localLam.Definition);
                        var localFdeclSpecs = new DeclSpecsNode(ctx.Start.Line, "local", "object");
                        return new FuncNode(ctx.Start.Line, localFdeclSpecs, localFdef);
                    }

                    IdListNode vars = this.Visit(ctx.namelist()).As<IdListNode>();
                    if (ctx.explist() is not null) {
                        ExprListNode inits = this.Visit(ctx.explist()).As<ExprListNode>();
                        if (inits.Expressions.Count() < vars.Identifiers.Count()) {
                            int missingCount = vars.Identifiers.Count() - inits.Expressions.Count();
                            IEnumerable<ExprNode> missing = Enumerable.Repeat<ExprNode>(new NullLitExprNode(ctx.Start.Line), missingCount);
                            inits = new ExprListNode(ctx.Start.Line, inits.Expressions.Concat(missing));
                        }

                        IEnumerable<DeclNode> varDecls = vars.Identifiers
                            .Zip(inits.Expressions, (identifier, initializer) => CreateDeclarator(identifier, initializer));
                        var declSpecs = new DeclSpecsNode(ctx.Start.Line, "local", "object");
                        var decls = new DeclListNode(ctx.Start.Line, varDecls);
                        return new DeclStatNode(ctx.Start.Line, declSpecs, decls);
                    } else {
                        IEnumerable<VarDeclNode> varDecls = vars.Identifiers
                            .Select(v => new VarDeclNode(ctx.Start.Line, v))
                            ;

                        var declSpecs = new DeclSpecsNode(ctx.Start.Line, "local", "object");
                        var decls = new DeclListNode(ctx.Start.Line, varDecls);
                        return new DeclStatNode(ctx.Start.Line, declSpecs, decls);
                    }
                default:
                    throw new SyntaxErrorException("Invalid statement type.");
            }

            ForStatNode CreateNumericForNode()
            {
                string iteratorName = ctx.NAME().GetText();
                ExprNode start = this.Visit(ctx.exp(0)).As<ExprNode>();
                ExprNode limit = this.Visit(ctx.exp(1)).As<ExprNode>();
                ExprNode step = ctx.exp().Length > 2
                    ? this.Visit(ctx.exp(2)).As<ExprNode>()
                    : new LitExprNode(ctx.Start.Line, 1);

                var init = new AssignExprNode(ctx.Start.Line, CreateIteratorId(), start);
                string comparison = IsNegativeNumericLiteral(step) ? ">=" : "<=";
                var cond = new RelExprNode(
                    ctx.Start.Line,
                    CreateIteratorId(),
                    RelOpNode.FromSymbol(ctx.Start.Line, comparison),
                    limit
                );
                var increment = new AssignExprNode(
                    ctx.Start.Line,
                    CreateIteratorId(),
                    AssignOpNode.FromSymbol(ctx.Start.Line, "+="),
                    step
                );
                BlockStatNode body = this.Visit(ctx.block().Single()).As<BlockStatNode>();
                return new ForStatNode(ctx.Start.Line, init, cond, increment, body);

                IdNode CreateIteratorId() => new(ctx.Start.Line, iteratorName);
            }

            ForStatNode CreateGenericForNode()
            {
                IdListNode vars = this.Visit(ctx.namelist()).As<IdListNode>();
                ExprListNode iterators = this.Visit(ctx.explist()).As<ExprListNode>();
                var init = new AssignExprNode(ctx.Start.Line, vars, iterators);
                BlockStatNode body = this.Visit(ctx.block().Single()).As<BlockStatNode>();
                return new ForStatNode(ctx.Start.Line, init, new LitExprNode(ctx.Start.Line, true), null, body);
            }

            static bool IsNegativeNumericLiteral(ExprNode expr)
            {
                if (expr is LitExprNode lit)
                    return IsNegativeValue(lit.Value);

                return expr is UnaryExprNode unary
                    && unary.Operator.Symbol == "-"
                    && unary.Operand is LitExprNode operand
                    && IsPositiveValue(operand.Value);
            }

            static bool IsNegativeValue(object? value)
                => value switch
                {
                    decimal v => v < 0,
                    double v => v < 0,
                    float v => v < 0,
                    long v => v < 0,
                    int v => v < 0,
                    short v => v < 0,
                    sbyte v => v < 0,
                    _ => false,
                };

            static bool IsPositiveValue(object? value)
                => value switch
                {
                    decimal v => v > 0,
                    double v => v > 0,
                    float v => v > 0,
                    ulong v => v > 0,
                    long v => v > 0,
                    uint v => v > 0,
                    int v => v > 0,
                    ushort v => v > 0,
                    short v => v > 0,
                    byte v => v > 0,
                    sbyte v => v > 0,
                    _ => false,
                };

            static ASTNode CreateAssignmentNode(int line, ExprListNode vars, ExprListNode initializers)
            {
                if (initializers.Children.Count < vars.Children.Count) {
                    int missingCount = vars.Children.Count - initializers.Children.Count;
                    IEnumerable<ExprNode> missing = Enumerable.Repeat<ExprNode>(new NullLitExprNode(line), missingCount);
                    initializers = new ExprListNode(line, initializers.Expressions.Concat(missing));
                }

                IEnumerable<AssignExprNode> assignments = vars.Expressions
                    .Zip(initializers.Expressions, (e1, e2) => (e1, e2))
                    .Select(tup => new AssignExprNode(line, tup.e1, tup.e2))
                    ;

                if (vars.Children.Count == 1)
                    return new ExprStatNode(line, assignments.First());
                return new BlockStatNode(line, assignments);
            }
        }

        public override ASTNode VisitRetstat([NotNull] RetstatContext ctx)
        {
            ExprNode? expr = null;
            if (ctx.explist() is not null)
                expr = this.Visit(ctx.explist()).As<ExprNode>();
            return new JumpStatNode(ctx.Start.Line, expr);
        }

        public override ASTNode VisitVarlist([NotNull] VarlistContext ctx)
            => new ExprListNode(ctx.Start.Line, ctx.var().Select(v => this.Visit(v).As<ExprNode>()));

        public override ASTNode VisitVar([NotNull] VarContext ctx)
        {
            ExprNode expr = ctx.NAME() is not null
                ? new IdNode(ctx.Start.Line, ctx.NAME().GetText())
                : this.Visit(ctx.exp()).As<ExprNode>();

            foreach (VarSuffixContext suffixCtx in ctx.varSuffix()) {
                foreach (NameAndArgsContext nameAndArgsCtx in suffixCtx.nameAndArgs())
                    expr = this.CreateFunctionCall(ctx.Start.Line, expr, nameAndArgsCtx);

                if (suffixCtx.exp() is not null) {
                    ExprNode index = this.Visit(suffixCtx.exp()).As<ExprNode>();
                    expr = new ArrAccessExprNode(suffixCtx.Start.Line, expr, index);
                } else {
                    expr = new IdNode(suffixCtx.Start.Line, $"{expr.GetText()}.{suffixCtx.NAME().GetText()}");
                }
            }

            return expr;
        }

        public override ASTNode VisitVarSuffix([NotNull] VarSuffixContext ctx)
        {
            if (ctx.NAME() is not null)
                return new IdNode(ctx.Start.Line, ctx.NAME().GetText());
            return this.Visit(ctx.exp());
        }

        public override ASTNode VisitNamelist([NotNull] NamelistContext ctx)
            => new IdListNode(ctx.Start.Line, ctx.NAME().Select(v => new IdNode(ctx.Start.Line, v.GetText())));

        public override ASTNode VisitFunctioncall([NotNull] FunctioncallContext ctx)
        {
            ExprNode expr = this.Visit(ctx.varOrExp()).As<ExprNode>();
            foreach (NameAndArgsContext nameAndArgsCtx in ctx.nameAndArgs())
                expr = this.CreateFunctionCall(ctx.Start.Line, expr, nameAndArgsCtx);
            return expr;
        }

        public override ASTNode VisitFuncname([NotNull] FuncnameContext ctx)
            => new IdNode(ctx.Start.Line, ctx.GetText());
    }
}
