using System;
using System.Linq;
using Antlr4.Runtime.Misc;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using static LINVAST.Imperative.Builders.Java.JavaParser;

namespace LINVAST.Imperative.Builders.Java
{
    public sealed partial class JavaASTBuilder : JavaBaseVisitor<ASTNode>, IASTBuilder<JavaParser>
    {
        public override ASTNode VisitMethodCall([NotNull] MethodCallContext ctx)
        {
            if (ctx.IDENTIFIER() is not null && ctx.RPAREN() is not null && ctx.RPAREN() is not null) {
                if (ctx.expressionList() is not null) {
                    ExprListNode param = this.Visit(ctx.expressionList()).As<ExprListNode>();
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText()), new TypeNameListNode(ctx.Start.Line), param);
                } else {
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText()));
                }
            }

            if (ctx.THIS() is not null && ctx.RPAREN() is not null && ctx.RPAREN() is not null) {
                if (ctx.expressionList() is not null) {
                    ExprListNode param = this.Visit(ctx.expressionList()).As<ExprListNode>();
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.THIS().GetText()), new TypeNameListNode(ctx.Start.Line), param);
                } else {
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.THIS().GetText()));
                }
            }

            if (ctx.SUPER() is not null && ctx.RPAREN() is not null && ctx.RPAREN() is not null) {
                if (ctx.expressionList() is not null) {
                    ExprListNode param = this.Visit(ctx.expressionList()).As<ExprListNode>();
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.SUPER().GetText()), new TypeNameListNode(ctx.Start.Line), param);
                } else {
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.SUPER().GetText()));
                }
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitExplicitGenericInvocation([NotNull] ExplicitGenericInvocationContext ctx)
        {
            if (ctx.nonWildcardTypeArguments() is not null && ctx.explicitGenericInvocationSuffix() is not null) {
                TypeNameListNode typeArgs = this.Visit(ctx.nonWildcardTypeArguments()).As<TypeNameListNode>();
                FuncCallExprNode suffix = this.Visit(ctx.explicitGenericInvocationSuffix()).As<FuncCallExprNode>();

                if (suffix.Arguments is null) {
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, suffix.Identifier), typeArgs);
                } else {
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, suffix.Identifier), typeArgs, suffix.Arguments);
                }
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitExplicitGenericInvocationSuffix([NotNull] ExplicitGenericInvocationSuffixContext ctx)
        {
            if (ctx.SUPER() is not null && ctx.superSuffix() is not null) {
                ASTNode? args = this.Visit(ctx.superSuffix());

                if (args is ExprListNode) {
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.SUPER().ToString()), new TypeNameListNode(ctx.Start.Line), args.As<ExprListNode>());
                }

                if (args is FuncCallExprNode) {
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, string.Format("{0}.{1}", ctx.SUPER().ToString(), args.As<FuncCallExprNode>().Identifier)), new TypeNameListNode(ctx.Start.Line), args.As<FuncCallExprNode>().Arguments);
                }
            }

            if (ctx.IDENTIFIER() is not null && ctx.arguments() is not null) {
                ExprListNode args = this.Visit(ctx.arguments()).As<ExprListNode>();
                return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText()), new TypeNameListNode(ctx.Start.Line), args);
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitSuperSuffix([NotNull] SuperSuffixContext ctx)
        {
            if (ctx.DOT() is not null && ctx.IDENTIFIER() is not null && ctx.arguments() is not null) {
                ExprListNode args = this.Visit(ctx.arguments()).As<ExprListNode>();
                return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText()), new TypeNameListNode(ctx.Start.Line), args);
            }

            if (ctx.arguments() is not null) {
                return this.Visit(ctx.arguments()).As<ExprListNode>();
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitFormalParameterList([NotNull] FormalParameterListContext ctx)
        {
            if (ctx.formalParameter().Length > 0) {
                System.Collections.Generic.IEnumerable<FuncParamNode>? p = ctx.formalParameter().Select(v => this.Visit(v).As<FuncParamNode>());

                if (ctx.lastFormalParameter() is not null) {
                    p.Append(this.Visit(ctx.lastFormalParameter()).As<FuncParamNode>());
                }
                return new FuncParamsNode(ctx.Start.Line, p);
            }

            if (ctx.lastFormalParameter() is not null) {
                return new FuncParamsNode(ctx.Start.Line, this.Visit(ctx.lastFormalParameter()).As<FuncParamNode>());
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitFormalParameter([NotNull] FormalParameterContext ctx)
        {
            string modifiers = "";
            TypeNameNode? type = null;
            IdNode? variableDeclaratorId = null;

            if (ctx.variableModifier().Length > 0) {
                modifiers = string.Join(" ", ctx.variableModifier().Select(v => this.Visit(v).As<TagNode>().GetText()));
            }

            if (ctx.typeType() is not null) {
                type = this.Visit(ctx.typeType()).As<TypeNameNode>();
            }

            if (ctx.variableDeclaratorId() is not null) {
                variableDeclaratorId = this.Visit(ctx.variableDeclaratorId()).As<IdNode>();
            }

            if (type is not null && variableDeclaratorId is not null) {
                var declSpecs = new DeclSpecsNode(ctx.Start.Line, modifiers, type.Identifier);
                var decl = new VarDeclNode(ctx.Start.Line, variableDeclaratorId);
                return new FuncParamNode(ctx.Start.Line, declSpecs, decl);
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitLastFormalParameter([NotNull] LastFormalParameterContext ctx)
        {
            string modifiers = "";
            TypeNameNode? type = null;
            IdNode? variableDeclaratorId = null;

            if (ctx.variableModifier().Length > 0) {
                modifiers = string.Join(" ", ctx.variableModifier().Select(v => this.Visit(v).As<TagNode>().GetText()));
            }

            if (ctx.typeType() is not null) {
                type = this.Visit(ctx.typeType()).As<TypeNameNode>();
            }

            if (ctx.variableDeclaratorId() is not null) {
                variableDeclaratorId = this.Visit(ctx.variableDeclaratorId()).As<IdNode>();
            }

            if (type is not null && variableDeclaratorId is not null) {
                var declSpecs = new DeclSpecsNode(ctx.Start.Line, modifiers, type.Identifier);
                var decl = new VarDeclNode(ctx.Start.Line, variableDeclaratorId);

                decl = ctx.annotation() is not null
                    ? new VarDeclNode(ctx.Start.Line, ctx.annotation().Select(v => this.Visit(v).As<TagNode>()), variableDeclaratorId)
                    : new VarDeclNode(ctx.Start.Line, variableDeclaratorId);

                return new FuncParamNode(ctx.Start.Line, declSpecs, decl);
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitVariableModifier([NotNull] VariableModifierContext ctx)
        {
            if (ctx.FINAL() is not null) 
                return new TagNode(ctx.Start.Line, ctx.FINAL().GetText());

            if (ctx.annotation() is not null) 
                return this.Visit(ctx.annotation()).As<TagNode>();

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitInnerCreator([NotNull] InnerCreatorContext ctx)
        {
            if (ctx.IDENTIFIER() is not null && ctx.classCreatorRest() is not null) {
                ExprListNode expr = this.Visit(ctx.classCreatorRest()).As<ExprListNode>();
                var id = new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());

                if (ctx.nonWildcardTypeArgumentsOrDiamond() is not null) {
                    TypeNameListNode type = this.Visit(ctx.nonWildcardTypeArgumentsOrDiamond()).As<TypeNameListNode>();
                    return new FuncCallExprNode(ctx.Start.Line, id, type, expr);
                } else {
                    return new FuncCallExprNode(ctx.Start.Line, id, new TypeNameListNode(ctx.Start.Line), expr);
                }
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitCreator([NotNull] CreatorContext ctx)
        {
            if (ctx.createdName() is not null) {
                TypeNameNode typeNameNode = this.Visit(ctx.createdName()).As<TypeNameNode>();
                var idNode = new IdNode(ctx.Start.Line, typeNameNode.GetText()); ;

                if (ctx.arrayCreatorRest() is not null) {
                    return new ArrAccessExprNode(ctx.Start.Line, idNode, this.Visit(ctx.arrayCreatorRest()).As<ExprNode>());
                }

                if (ctx.classCreatorRest() is not null) {
                    ExprListNode args = this.Visit(ctx.classCreatorRest()).As<ExprListNode>();

                    if (ctx.nonWildcardTypeArguments() is not null) {
                        TypeNameListNode typeArgs = this.Visit(ctx.nonWildcardTypeArguments()).As<TypeNameListNode>();
                        return new FuncCallExprNode(ctx.Start.Line, idNode, typeArgs, args);
                    } else {
                        return new FuncCallExprNode(ctx.Start.Line, idNode, new TypeNameListNode(ctx.Start.Line), args);
                    }
                }
            }

            throw new NotImplementedException("Implementation pending (there are dependencies on statements (blocks), arrays and some type arguments that are not implemented atm)");
        }

        public override ASTNode VisitCreatedName([NotNull] CreatedNameContext ctx)
        {
            if (ctx.IDENTIFIER().Any()) {
                if (ctx.typeArgumentsOrDiamond().Any()) {
                    TypeNameListNode typeName = this.Visit(ctx.typeArgumentsOrDiamond(0)).As<TypeNameListNode>();
                    return new TypeNameNode(ctx.Start.Line, ctx.IDENTIFIER(0).GetText(), typeName.Types);
                } else {
                    return new TypeNameNode(ctx.Start.Line, ctx.IDENTIFIER(0).GetText());
                }
            }

            if (ctx.primitiveType() is not null) {
                return this.Visit(ctx.primitiveType()).As<TypeNameNode>();
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitTypeArgumentsOrDiamond([NotNull] TypeArgumentsOrDiamondContext ctx)
        {
            if (ctx.typeArguments() is not null) {
                return this.Visit(ctx.typeArguments()).As<TypeNameListNode>();
            } else {
                return new TypeNameListNode(ctx.Start.Line);
            }
        }

        public override ASTNode VisitClassCreatorRest([NotNull] ClassCreatorRestContext ctx)
        {
            if (ctx.arguments() is not null) {
                if (ctx.classBody() is not null) {
                    ; //since VisitClassBody is not implemented yet
                }
                return this.Visit(ctx.arguments()).As<ExprListNode>();
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitArrayCreatorRest([NotNull] ArrayCreatorRestContext ctx)
        {
            if (ctx.arrayInitializer() is not null) {
                return this.Visit(ctx.arrayInitializer()).As<ArrInitExprNode>();
            }

            if (ctx.expression().Any()) {
                if (ctx.expression().Length == 1) {
                    return this.Visit(ctx.expression(0)).As<ExprNode>();
                } else {
                    throw new NotImplementedException("Multidimensional array.");
                }
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitArrayInitializer([NotNull] ArrayInitializerContext ctx)
            => new ArrInitExprNode(ctx.Start.Line, ctx.variableInitializer().Select(v => this.Visit(v).As<ExprNode>()));

        public override ASTNode VisitVariableInitializer([NotNull] VariableInitializerContext ctx)
        {
            if (ctx.arrayInitializer() is not null) {
                return this.Visit(ctx.arrayInitializer()).As<ExprNode>();
            }

            if (ctx.expression() is not null) {
                return this.Visit(ctx.expression()).As<ExprNode>();
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitAnnotation([NotNull] AnnotationContext ctx)
        {
            IdNode? nameNode = null;

            if (ctx.AT() is not null && ctx.qualifiedName() is not null) {
                nameNode = this.Visit(ctx.qualifiedName()).As<IdNode>();
            }

            if (ctx.altAnnotationQualifiedName() is not null) {
                nameNode = this.Visit(ctx.altAnnotationQualifiedName()).As<IdNode>();
            }

            if (nameNode is null) {
                throw new SyntaxErrorException("Unknown construct, bad annotation name");
            }

            if (ctx.elementValuePairs() is not null) {
                TagNode pairs = this.Visit(ctx.elementValuePairs()).As<TagNode>();
                return new TagNode(ctx.Start.Line, nameNode.Identifier, pairs.Fields);
            }

            if (ctx.elementValue() is not null) {
                ASTNode? element = this.Visit(ctx.elementValue());
                return new TagNode(ctx.Start.Line, nameNode, new TagFieldNode(ctx.Start.Line, "value", new(ctx.Start.Line, element.GetText())));
            }

            return new TagNode(ctx.Start.Line, nameNode);
        }

        public override ASTNode VisitElementValue([NotNull] ElementValueContext ctx)
        {
            if (ctx.expression() is not null) {
                return this.Visit(ctx.expression()).As<ExprNode>();
            }

            if (ctx.annotation() is not null) {
                return this.Visit(ctx.annotation()).As<TagNode>();
            }

            if (ctx.elementValueArrayInitializer() is not null) {
                return this.Visit(ctx.elementValueArrayInitializer()).As<ArrInitExprNode>();
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitElementValuePairs([NotNull] ElementValuePairsContext ctx)
            => new TagNode(ctx.Start.Line, "tmp", ctx.elementValuePair().Select(v => this.Visit(v).As<TagFieldNode>()));

        public override ASTNode VisitElementValuePair([NotNull] ElementValuePairContext ctx)
        {
            if (ctx.IDENTIFIER() is not null && ctx.elementValue() is not null) {
                ASTNode? element = this.Visit(ctx.elementValue());
                return new TagFieldNode(ctx.Start.Line, ctx.IDENTIFIER().GetText(), new LitExprNode(ctx.Start.Line, element.GetText()));
            }
            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitElementValueArrayInitializer([NotNull] ElementValueArrayInitializerContext ctx)
            => new ArrInitExprNode(ctx.Start.Line, ctx.elementValue().Select(v => this.Visit(v).As<ExprNode>()));

        public override ASTNode VisitAltAnnotationQualifiedName([NotNull] AltAnnotationQualifiedNameContext ctx)
            => new IdNode(ctx.Start.Line, string.Join('.', ctx.IDENTIFIER().Select(id => id.GetText())));

        public override ASTNode VisitArguments([NotNull] ArgumentsContext ctx)
        {
            if (ctx.LPAREN() is not null && ctx.expressionList() is not null && ctx.RPAREN() is not null) {
                return this.Visit(ctx.expressionList()).As<ExprListNode>();
            }

            throw new SyntaxErrorException("Unknown construct");
        }
    }
}
