using System;
using System.Linq;
using Antlr4.Runtime.Misc;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using Serilog;
using static LINVAST.Imperative.Builders.C.CParser;

namespace LINVAST.Imperative.Builders.C
{
    public sealed partial class CASTBuilder : CBaseVisitor<ASTNode>, IASTBuilder<CParser>
    {
        public override ASTNode VisitDeclaration([NotNull] DeclarationContext ctx)
        {
            if (ctx.staticAssertDeclaration() is not null || ctx.initDeclaratorList() is null)
                throw new NotImplementedException("static assert");

            DeclSpecsNode declSpecs = this.Visit(ctx.declarationSpecifiers()).As<DeclSpecsNode>();
            DeclListNode declList = this.Visit(ctx.initDeclaratorList()).As<DeclListNode>();
            if (declSpecs.TypeName.EndsWith("*")) {
                string pointerFreeType = declSpecs.TypeName.Substring(0, declSpecs.TypeName.IndexOf("*"));
                declSpecs = new DeclSpecsNode(declSpecs.Line, declSpecs.Modifiers.ToString(), pointerFreeType);
                foreach (DeclNode decl in declList.Declarators)
                    decl.PointerLevel = 1;
            }
            return new DeclStatNode(ctx.Start.Line, declSpecs, declList);
        }

        public override ASTNode VisitDeclarator([NotNull] DeclaratorContext ctx)
        {
            DeclNode decl = this.Visit(ctx.directDeclarator()).As<DeclNode>();
            if (ctx.pointer() is not null)
                decl.PointerLevel++;
            return decl;
        }

        public override ASTNode VisitDirectDeclarator([NotNull] DirectDeclaratorContext ctx)
        {
            if (ctx.declarator() is not null)
                return this.Visit(ctx.declarator());

            if (ctx.Identifier() is not null) {
                if (ctx.ChildCount == 1)
                    return new VarDeclNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.Identifier().ToString() ?? "<unknown_name>"));
                else
                    throw new NotImplementedException("bit field");
            }

            if (ctx.typeQualifierList() is not null || ctx.typeSpecifier() is not null || ctx.pointer() is not null)
                throw new NotImplementedException("qualified arrays and function pointers");

            DeclNode decl = this.Visit(ctx.directDeclarator()).As<DeclNode>();
            if (decl is VarDeclNode var) {
                if (AreBracketsTokensPresent(ctx)) {
                    if (ctx.assignmentExpression() is not null) {
                        ExprNode sizeExpr = this.Visit(ctx.assignmentExpression()).As<ExprNode>();
                        return new ArrDeclNode(ctx.Start.Line, var.IdentifierNode, sizeExpr);
                    } else {
                        return new ArrDeclNode(ctx.Start.Line, var.IdentifierNode);
                    }
                } else if (AreParenTokensPresent(ctx)) {
                    if (ctx.parameterTypeList() is not null) {
                        FuncParamsNode @params = this.Visit(ctx.parameterTypeList()).As<FuncParamsNode>();
                        return new FuncDeclNode(ctx.Start.Line, var.IdentifierNode, @params);
                    } else {
                        return new FuncDeclNode(ctx.Start.Line, var.IdentifierNode);
                    }
                } else {
                    return var;
                }
            } else if (decl is ArrDeclNode arr) {
                if (AreBracketsTokensPresent(ctx)) {
                    throw new NotImplementedException("multidimensional arrays");
                } else if (AreParenTokensPresent(ctx)) {
                    Log.Warning("Potential syntax error in line: {Line}. Parsing will continue but results are not guaranteed...", ctx.Start.Line);
                } else {
                    return arr;
                }
            } else {
                Log.Warning("Potential syntax error in line: {Line}. Parsing will continue but results are not guaranteed...", ctx.Start.Line);
            }

            return decl;


            static bool AreParenTokensPresent(DirectDeclaratorContext ctx)
                => ctx.GetToken(LeftParen, 0) is not null && ctx.GetToken(RightParen, 0) is not null;

            static bool AreBracketsTokensPresent(DirectDeclaratorContext ctx)
                => ctx.GetToken(LeftBracket, 0) is not null && ctx.GetToken(RightBracket, 0) is not null;
        }

        public override ASTNode VisitDeclarationSpecifiers([NotNull] DeclarationSpecifiersContext ctx)
        {
            string[] specs = ctx.children.Select(c => c.GetText()).ToArray();
            int unsignedIndex = Array.IndexOf(specs, "unsigned");
            string type = unsignedIndex != -1 ? string.Join(' ', specs[unsignedIndex..]) : specs.Last();
            return new DeclSpecsNode(ctx.Start.Line, string.Join(' ', specs), type);
        }

        public override ASTNode VisitInitDeclaratorList([NotNull] InitDeclaratorListContext ctx)
        {
            DeclNode decl = this.Visit(ctx.initDeclarator()).As<DeclNode>();

            if (ctx.initDeclaratorList() is null)
                return new DeclListNode(ctx.Start.Line, decl);

            DeclListNode list = this.Visit(ctx.initDeclaratorList()).As<DeclListNode>();
            return new DeclListNode(ctx.Start.Line, list.Declarators.Concat(new[] { decl }));
        }

        public override ASTNode VisitInitDeclarator([NotNull] InitDeclaratorContext ctx)
        {
            DeclNode declarator = this.Visit(ctx.declarator()).As<DeclNode>();
            ASTNode? init = null;
            if (ctx.initializer() is not null)
                init = this.Visit(ctx.initializer());

            if (declarator is VarDeclNode var)
                return init is null ? var : new VarDeclNode(ctx.Start.Line, var.IdentifierNode, init.As<ExprNode>());

            if (declarator is ArrDeclNode arr) {
                if (arr.SizeExpression is null) {
                    if (init is null)
                        return new ArrDeclNode(ctx.Start.Line, arr.IdentifierNode);
                    else
                        return new ArrDeclNode(ctx.Start.Line, arr.IdentifierNode, init.As<ArrInitExprNode>());
                } else {
                    if (init is null)
                        return new ArrDeclNode(ctx.Start.Line, arr.IdentifierNode, arr.SizeExpression);
                    else
                        return new ArrDeclNode(ctx.Start.Line, arr.IdentifierNode, arr.SizeExpression, init.As<ArrInitExprNode>());
                }
            }

            return declarator;
        }

        public override ASTNode VisitInitializerList([NotNull] InitializerListContext ctx)
        {
            if (ctx.designation() is not null)
                throw new NotImplementedException("designation");

            ExprNode init = this.Visit(ctx.initializer()).As<ExprNode>();

            if (ctx.initializerList() is null)
                return new ArrInitExprNode(ctx.Start.Line, init);

            ArrInitExprNode list = this.Visit(ctx.initializerList()).As<ArrInitExprNode>();
            return new ArrInitExprNode(ctx.Start.Line, list.Initializers.Concat(new[] { init }));
        }

        public override ASTNode VisitInitializer([NotNull] InitializerContext ctx)
            => ctx.assignmentExpression() is not null ? this.Visit(ctx.assignmentExpression()) : this.Visit(ctx.initializerList());
    }
}
