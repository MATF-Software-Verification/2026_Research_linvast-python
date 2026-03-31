using System.Linq;
using Antlr4.Runtime.Misc;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using static LINVAST.Imperative.Builders.C.CParser;

namespace LINVAST.Imperative.Builders.C
{
    public sealed partial class CASTBuilder : CBaseVisitor<ASTNode>, IASTBuilder<CParser>
    {
        public override ASTNode VisitFunctionDefinition([NotNull] FunctionDefinitionContext ctx)
        {
            DeclSpecsNode declSpecs = this.Visit(ctx.declarationSpecifiers()).As<DeclSpecsNode>();
            ASTNode decl = this.Visit(ctx.declarator());
            if (decl is IdNode fname)
                decl = new FuncDeclNode(fname.Line, fname);
            FuncDeclNode fdecl = decl.As<FuncDeclNode>();
            BlockStatNode body = this.Visit(ctx.compoundStatement()).As<BlockStatNode>();
            FuncDeclNode fdef = fdecl.ParametersNode is not null
                ? new FuncDeclNode(fdecl.Line, fdecl.IdentifierNode, fdecl.ParametersNode, body)
                : new FuncDeclNode(fdecl.Line, fdecl.IdentifierNode, body);
            return new FuncNode(ctx.Start.Line, declSpecs, fdef);
        }

        public override ASTNode VisitParameterTypeList([NotNull] ParameterTypeListContext ctx)
        {
            FuncParamsNode @params = this.Visit(ctx.parameterList()).As<FuncParamsNode>();
            if (ctx.ChildCount > 1)
                @params.IsVariadic = true;
            return @params;
        }

        public override ASTNode VisitParameterList([NotNull] ParameterListContext ctx)
        {
            FuncParamsNode @params;
            FuncParamNode param = this.Visit(ctx.parameterDeclaration()).As<FuncParamNode>();

            if (ctx.parameterList() is null)
                return new FuncParamsNode(ctx.Start.Line, param);

            @params = this.Visit(ctx.parameterList()).As<FuncParamsNode>();
            return new FuncParamsNode(ctx.Start.Line, @params.Parameters.Concat(new[] { param }));
        }

        public override ASTNode VisitParameterDeclaration([NotNull] ParameterDeclarationContext ctx)
        {
            DeclSpecsNode declSpecs = this.Visit(ctx.declarationSpecifiers()).As<DeclSpecsNode>();
            DeclNode decl = this.Visit(ctx.declarator()).As<DeclNode>();
            return new FuncParamNode(ctx.Start.Line, declSpecs, decl);
        }
    }
}
