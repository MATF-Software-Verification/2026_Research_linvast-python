using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using static LINVAST.Imperative.Builders.Pseudo.PseudoParser;

namespace LINVAST.Imperative.Builders.Pseudo
{
    public sealed partial class PseudoASTBuilder : PseudoBaseVisitor<ASTNode>, IASTBuilder<PseudoParser>
    {
        public override ASTNode VisitDeclaration([NotNull] DeclarationContext ctx)
        {
            switch (ctx.children.First().GetText()) {
                case "declare":
                    var declSpecs = new DeclSpecsNode(ctx.Start.Line, GetTypeName());
                    var name = new IdNode(ctx.Start.Line, ctx.NAME().GetText());
                    DeclNode decl;
                    if (ctx.type().typename().children.Count > 1) {
                        switch (ctx.type().typename().children.Last().GetText()) {
                            case "array":
                            case "list":
                            case "set":
                                if (ctx.exp() is not null) {
                                    ExprNode init = this.Visit(ctx.exp()).As<ExprNode>();
                                    decl = new ArrDeclNode(ctx.Start.Line, name, init);
                                } else {
                                    decl = new ArrDeclNode(ctx.Start.Line, name);
                                }
                                break;
                            default:
                                throw new SyntaxErrorException("Invalid complex type");
                        }
                    } else {
                        if (ctx.exp() is not null) {
                            ExprNode init = this.Visit(ctx.exp()).As<ExprNode>();
                            decl = new VarDeclNode(ctx.Start.Line, name, init);
                        } else {
                            decl = new VarDeclNode(ctx.Start.Line, name);
                        }
                    }
                    var declList = new DeclListNode(ctx.Start.Line, decl);
                    return new DeclStatNode(ctx.Start.Line, declSpecs, declList);
                case "procedure":
                case "function":
                    var fdeclSpecs = new DeclSpecsNode(ctx.Start.Line, GetTypeName());
                    var fname = new IdNode(ctx.Start.Line, ctx.NAME().GetText());
                    FuncParamsNode? fparams = ctx.parlist() is null ? null : this.Visit(ctx.parlist()).As<FuncParamsNode>();
                    BlockStatNode body = this.Visit(ctx.block()).As<BlockStatNode>();
                    FuncDeclNode fdef = fparams is null
                        ? new FuncDeclNode(ctx.Start.Line, fname, body)
                        : new FuncDeclNode(ctx.Start.Line, fname, fparams, body);
                    return new FuncNode(ctx.Start.Line, fdeclSpecs, fdef);
                default:
                    throw new SyntaxErrorException("Invalid statement");
            }


            string GetTypeName() => ctx.type()?.typename().GetText() ?? "void";
        }

        public override ASTNode VisitParlist([NotNull] ParlistContext ctx)
        {
            IEnumerable<FuncParamNode> @params = ctx.NAME().Zip(ctx.type(), (name, type) => {
                var declSpecs = new DeclSpecsNode(type.Start.Line, type.typename().GetText());
                var identifier = new IdNode(ctx.Start.Line, name.GetText());
                DeclNode decl;
                if (type.typename().children.Count > 1) {
                    switch (type.typename().children.Last().GetText()) {
                        case "array":
                        case "list":
                        case "set":
                            decl = new ArrDeclNode(ctx.Start.Line, identifier);
                            break;
                        default:
                            throw new SyntaxErrorException("Invalid complex type");
                    }
                } else {
                    decl = new VarDeclNode(ctx.Start.Line, identifier);
                }
                return new FuncParamNode(type.Start.Line, declSpecs, decl);
            });
            return new FuncParamsNode(ctx.Start.Line, @params);
        }
    }
}
