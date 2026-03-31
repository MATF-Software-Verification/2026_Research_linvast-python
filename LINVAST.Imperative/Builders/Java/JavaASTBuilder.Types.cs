using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime.Misc;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using static LINVAST.Imperative.Builders.Java.JavaParser;

namespace LINVAST.Imperative.Builders.Java
{
    public sealed partial class JavaASTBuilder : JavaBaseVisitor<ASTNode>, IASTBuilder<JavaParser>
    {
        public override ASTNode VisitTypeDeclaration([NotNull] TypeDeclarationContext ctx)
        {
            int ctxStartLine = ctx.Start.Line;
            string? modifiers = "";
            if (ctx.classOrInterfaceModifier() is not null) {
                ctxStartLine = ctx.classOrInterfaceModifier().First().Start.Line;
                modifiers = string.Join(" ", ctx.classOrInterfaceModifier().Select(c => c.GetText()));
            }

            if (ctx.annotationTypeDeclaration() is not null)
                throw new NotImplementedException("annotations");

            if (ctx.classDeclaration() is not null) {
                TypeDeclNode? classDecl = this.Visit(ctx.classDeclaration()).As<TypeDeclNode>();
                int declSpecsLine = ctxStartLine;
                return new ClassNode(ctx.Start.Line, new DeclSpecsNode(declSpecsLine, modifiers, classDecl.Identifier), classDecl);
            }

            if (ctx.enumDeclaration() is not null) {
                EnumDeclNode? enumDecl = this.Visit(ctx.enumDeclaration()).As<EnumDeclNode>();
                int declSpecsLine = ctxStartLine;
                return new EnumNode(ctx.Start.Line, new DeclSpecsNode(declSpecsLine, modifiers, enumDecl.Identifier), enumDecl);
            }

            if (ctx.interfaceDeclaration() is not null) {
                TypeDeclNode? interfaceDecl = this.Visit(ctx.interfaceDeclaration()).As<TypeDeclNode>();
                int declSpecsLine = ctxStartLine;
                return new InterfaceNode(ctx.Start.Line, new DeclSpecsNode(declSpecsLine, modifiers, interfaceDecl.Identifier), interfaceDecl);
            }

            return new EmptyStatNode(ctx.Start.Line);
        }

        public override ASTNode VisitClassOrInterfaceModifier([NotNull] ClassOrInterfaceModifierContext ctx)
        {
            if (ctx.annotation() is not null)
                throw new NotImplementedException("annotations");

            return new DeclSpecsNode(ctx.Start.Line, ctx.children.First().GetText());
        }

        public override ASTNode VisitTypeType([NotNull] TypeTypeContext ctx)
        {
            if (ctx.annotation().Any())
                throw new NotImplementedException("annotations");

            if (ctx.primitiveType() is not null)
                return this.Visit(ctx.primitiveType());

            return this.Visit(ctx.classOrInterfaceType());
        }


        public override ASTNode VisitTypeList([NotNull] TypeListContext ctx)
        {
            IEnumerable<TypeNameNode>? typeNameNodes = ctx.typeType().Select(c => this.Visit(c).As<TypeNameNode>());
            return new TypeNameListNode(ctx.Start.Line, typeNameNodes);
        }

        public override ASTNode VisitTypeParameters([NotNull] TypeParametersContext ctx)
        {
            IEnumerable<TypeNameNode>? typeNameNodes = ctx.typeParameter().Select(c => this.Visit(c).As<TypeNameNode>());
            return new TypeNameListNode(ctx.Start.Line, typeNameNodes);
        }

        public override ASTNode VisitTypeParameter([NotNull] TypeParameterContext ctx)
        {
            if (ctx.annotation().Any())
                throw new NotImplementedException("annotations");

            TypeNameListNode baseList = ctx.typeBound() is not null ? this.Visit(ctx.typeBound()).As<TypeNameListNode>() : new TypeNameListNode(ctx.Start.Line);
            return new TypeNameNode(ctx.Start.Line, ctx.IDENTIFIER().GetText(), baseList.Types);
        }

        public override ASTNode VisitTypeBound([NotNull] TypeBoundContext ctx)
        {
            IEnumerable<TypeNameNode>? typeNameNodes = ctx.typeType().Select(c => this.Visit(c).As<TypeNameNode>());
            return new TypeNameListNode(ctx.Start.Line, typeNameNodes);
        }


        public override ASTNode VisitPrimitiveType([NotNull] PrimitiveTypeContext ctx)
            => new TypeNameNode(ctx.Start.Line, ctx.children.First().GetText());

        public override ASTNode VisitClassType([NotNull] ClassTypeContext ctx)
        {
            if (ctx.annotation().Any())
                throw new NotImplementedException("annotations");

            int ctxStartLine = ctx.Start.Line;

            TypeNameListNode templlist = new(ctxStartLine), baselist = new(ctxStartLine);
            if (ctx.classOrInterfaceType() is not null) {
                TypeNameNode typeName = this.Visit(ctx.classOrInterfaceType()).As<TypeNameNode>();

                ctxStartLine = ctx.classOrInterfaceType().Start.Line;

                baselist = new TypeNameListNode(ctxStartLine, typeName);
            }

            if (ctx.typeArguments() is not null)
                templlist = this.Visit(ctx.typeArguments()).As<TypeNameListNode>();

            var identifier = new IdNode(ctxStartLine, ctx.IDENTIFIER().GetText());
            return new TypeDeclNode(ctxStartLine, identifier, templlist, baselist, new ArrayList<DeclStatNode>());
        }

        public override ASTNode VisitClassOrInterfaceType([NotNull] ClassOrInterfaceTypeContext ctx)
        {
            var typeNames = new TypeNameListNode(ctx.Start.Line);
            if (ctx.typeArguments()?.Any() ?? false)
                typeNames = this.Visit(ctx.typeArguments().First()).As<TypeNameListNode>();
            
            string identifier = string.Join(".", ctx.IDENTIFIER().Select(id => id.GetText()));
            return new TypeNameNode(ctx.Start.Line, identifier, typeNames.Types);
        }
        public override ASTNode VisitTypeTypeOrVoid([NotNull] TypeTypeOrVoidContext ctx)
        {
            if (ctx.typeType() is not null)
                return this.Visit(ctx.typeType());

            return new TypeNameNode(ctx.Start.Line, ctx.children.First().GetText());
        }

        public override ASTNode VisitNonWildcardTypeArguments([NotNull] NonWildcardTypeArgumentsContext ctx)
            => this.Visit(ctx.typeList());

        public override ASTNode VisitNonWildcardTypeArgumentsOrDiamond([NotNull] NonWildcardTypeArgumentsOrDiamondContext ctx)
        {
            if (ctx.nonWildcardTypeArguments() is null)
                throw new NotImplementedException("<>");

            return this.Visit(ctx.nonWildcardTypeArguments());
        }

        public override ASTNode VisitTypeArgument([NotNull] TypeArgumentContext ctx)
        {
            if (ctx.annotation().Any())
                throw new NotImplementedException("annotations");

            if (ctx.EXTENDS() is not null || ctx.SUPER() is not null) {
                //TODO EXTENDS/SUPER
                return this.Visit(ctx.typeType());
            }

            return this.Visit(ctx.typeType());
        }

        public override ASTNode VisitTypeArguments([NotNull] TypeArgumentsContext ctx)
        {
            IEnumerable<TypeNameNode>? typeNameNodes = ctx.typeArgument().Select(c => this.Visit(c).As<TypeNameNode>());
            return new TypeNameListNode(ctx.Start.Line, typeNameNodes);
        }
    }
}