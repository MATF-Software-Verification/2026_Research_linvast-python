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
                    return new BlockStatNode(ctx.Start.Line, repeatBody, new WhileStatNode(ctx.Start.Line, negUntil, repeatBody));
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
                    return new EmptyStatNode(ctx.Start.Line); // TODO
                case "function":
                    IdNode fname = this.Visit(ctx.funcname()).As<IdNode>();
                    LambdaFuncExprNode lam = this.Visit(ctx.funcbody()).As<LambdaFuncExprNode>();
                    FuncDeclNode fdef = lam.ParametersNode is null
                        ? new FuncDeclNode(ctx.Start.Line, fname, lam.Definition)
                        : new FuncDeclNode(ctx.Start.Line, fname, lam.ParametersNode, lam.Definition);
                    var fdeclSpecs = new DeclSpecsNode(ctx.Start.Line);
                    return new FuncNode(ctx.Start.Line, fdeclSpecs, fdef);
                case "local":
                    if (ctx.children[1].GetText().Equals("function", StringComparison.InvariantCultureIgnoreCase))
                        return new EmptyStatNode(ctx.Start.Line);  // TODO
                    IdListNode vars = this.Visit(ctx.namelist()).As<IdListNode>();
                    if (ctx.explist() is not null) {
                        // TODO 'local' info is lost here
                        ExprListNode inits = this.Visit(ctx.explist()).As<ExprListNode>();
                        return CreateAssignmentNode(ctx.Start.Line, new ExprListNode(vars.Line, vars.Identifiers), inits);
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
            if (ctx.NAME() is not null) {
                var id = new IdNode(ctx.Start.Line, ctx.NAME().GetText());
                if (ctx.varSuffix() is not null && ctx.varSuffix().Any()) {
                    // NOTE will require update once VisitVarSuffix is enhanced
                    ExprNode index = this.Visit(ctx.varSuffix().First()).As<ExprNode>();
                    return new ArrAccessExprNode(ctx.Start.Line, id, index);
                }
            }
            return new IdNode(ctx.Start.Line, ctx.NAME().GetText());
        }

        public override ASTNode VisitVarSuffix([NotNull] VarSuffixContext ctx)
        {
            if (ctx.nameAndArgs()?.Any() ?? false)
                throw new NotImplementedException("nameAndArgs*");
            if (ctx.NAME() is not null)
                throw new NotImplementedException("Field access");
            return this.Visit(ctx.exp());
        }

        public override ASTNode VisitNamelist([NotNull] NamelistContext ctx)
            => new IdListNode(ctx.Start.Line, ctx.NAME().Select(v => new IdNode(ctx.Start.Line, v.GetText())));

        public override ASTNode VisitFunctioncall([NotNull] FunctioncallContext ctx)
        {
            IdNode fname = this.Visit(ctx.varOrExp()).As<IdNode>();
            if (ctx.nameAndArgs().Length > 1)
                throw new NotImplementedException("Multiple nameAndArgs");
            ExprListNode args = this.Visit(ctx.nameAndArgs().Single()).As<ExprListNode>();
            return new FuncCallExprNode(ctx.Start.Line, fname, args);
        }

        public override ASTNode VisitFuncname([NotNull] FuncnameContext ctx)
        {
            if (ctx.NAME().Length > 1)
                throw new NotImplementedException("Function name*");
            return new IdNode(ctx.Start.Line, ctx.NAME().Single().GetText());
        }
    }
}
