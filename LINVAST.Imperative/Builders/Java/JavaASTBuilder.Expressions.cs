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
        public override ASTNode VisitExpression([NotNull] ExpressionContext ctx)
        {
            if (ctx.primary() is not null) {
                return this.Visit(ctx.primary()).As<ExprNode>();
            }

            if (ctx.methodCall() is not null) {
                return this.Visit(ctx.methodCall()).As<FuncCallExprNode>();
            }

            if (ctx.lambdaExpression() is not null) {
                return this.Visit(ctx.lambdaExpression()).As<ExprNode>();
            }

            if (ctx.QUESTION() is not null && ctx.COLON() is not null && ctx.expression().Length == 3) {
                ExprNode cond = this.Visit(ctx.expression(0)).As<ExprNode>();
                ExprNode @then = this.Visit(ctx.expression(1)).As<ExprNode>();
                ExprNode @else = this.Visit(ctx.expression(2)).As<ExprNode>();
                return new CondExprNode(ctx.Start.Line, cond, @then, @else);
            }

            if (ctx.NEW() is not null && ctx.creator() is not null) {
                return this.Visit(ctx.creator()).As<ExprNode>();
            }

            if (ctx.ADD() is not null) {
                if (ctx.expression().Length == 2) {
                    return new ArithmExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), ArithmOpNode.FromSymbol(ctx.Start.Line, ctx.ADD().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
                }

                if (ctx.expression().Length == 1) {
                    return new UnaryExprNode(ctx.Start.Line, UnaryOpNode.FromSymbol(ctx.Start.Line, ctx.ADD().GetText()), this.Visit(ctx.expression(0)).As<ExprNode>());
                }
            }

            if (ctx.SUB() is not null) {
                if (ctx.expression().Length == 2) {
                    return new ArithmExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), ArithmOpNode.FromSymbol(ctx.Start.Line, ctx.SUB().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
                }

                if (ctx.expression().Length == 1) {
                    return new UnaryExprNode(ctx.Start.Line, UnaryOpNode.FromSymbol(ctx.Start.Line, ctx.SUB().GetText()), this.Visit(ctx.expression(0)).As<ExprNode>());
                }
            }

            if (ctx.INC() is not null && ctx.expression().Length == 1) {
                return new IncExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>());
            }

            if (ctx.DEC() is not null && ctx.expression().Length == 1) {
                return new DecExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>());
            }

            if (ctx.TILDE() is not null && ctx.expression().Length == 1) {
                return new UnaryExprNode(ctx.Start.Line, UnaryOpNode.FromSymbol(ctx.Start.Line, ctx.TILDE().GetText()), this.Visit(ctx.expression(0)).As<ExprNode>());
            }

            if (ctx.BANG() is not null && ctx.expression().Length == 1) {
                return new UnaryExprNode(ctx.Start.Line, UnaryOpNode.FromSymbol(ctx.Start.Line, ctx.BANG().GetText()), this.Visit(ctx.expression(0)).As<ExprNode>());
            }

            if (ctx.MUL() is not null && ctx.expression().Length == 2) {
                return new ArithmExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), ArithmOpNode.FromSymbol(ctx.Start.Line, ctx.MUL().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.DIV() is not null && ctx.expression().Length == 2) {
                return new ArithmExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), ArithmOpNode.FromSymbol(ctx.Start.Line, ctx.DIV().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.MOD() is not null && ctx.expression().Length == 2) {
                return new ArithmExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), ArithmOpNode.FromSymbol(ctx.Start.Line, ctx.MOD().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.LT().Length == 1 && ctx.expression().Length == 2) {
                return new RelExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), RelOpNode.FromSymbol(ctx.Start.Line, ctx.LT(0).GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.LT().Length == 2 && ctx.expression().Length == 2) {
                return new ArithmExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), ArithmOpNode.FromSymbol(ctx.Start.Line, "<<"), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.GT().Length == 1 && ctx.expression().Length == 2) {
                return new RelExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), RelOpNode.FromSymbol(ctx.Start.Line, ctx.GT(0).GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.GT().Length == 2 && ctx.expression().Length == 2) {
                return new ArithmExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), ArithmOpNode.FromSymbol(ctx.Start.Line, ">>"), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.GT().Length == 3 && ctx.expression().Length == 2) {
                return new ArithmExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), ArithmOpNode.FromSymbol(ctx.Start.Line, ">>>"), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.LE() is not null && ctx.expression().Length == 2) {
                return new RelExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), RelOpNode.FromSymbol(ctx.Start.Line, ctx.LE().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.GE() is not null && ctx.expression().Length == 2) {
                return new RelExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), RelOpNode.FromSymbol(ctx.Start.Line, ctx.GE().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.EQUAL() is not null && ctx.expression().Length == 2) {
                return new RelExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), RelOpNode.FromSymbol(ctx.Start.Line, ctx.EQUAL().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.NOTEQUAL() is not null && ctx.expression().Length == 2) {
                return new RelExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), RelOpNode.FromSymbol(ctx.Start.Line, ctx.NOTEQUAL().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.BITAND() is not null && ctx.expression().Length == 2) {
                return new LogicExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), BinaryLogicOpNode.FromSymbol(ctx.Start.Line, ctx.BITAND().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.CARET() is not null && ctx.expression().Length == 2) {
                return new LogicExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), BinaryLogicOpNode.FromSymbol(ctx.Start.Line, ctx.CARET().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.BITOR() is not null && ctx.expression().Length == 2) {
                return new LogicExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), BinaryLogicOpNode.FromSymbol(ctx.Start.Line, ctx.BITOR().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.AND() is not null && ctx.expression().Length == 2) {
                return new LogicExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), BinaryLogicOpNode.FromSymbol(ctx.Start.Line, ctx.AND().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.OR() is not null && ctx.expression().Length == 2) {
                return new LogicExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), BinaryLogicOpNode.FromSymbol(ctx.Start.Line, ctx.OR().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.ADD_ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.ADD_ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.SUB_ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.SUB_ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.MUL_ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.MUL_ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.DIV_ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.DIV_ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.AND_ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.AND_ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.OR_ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.OR_ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.XOR_ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.XOR_ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.RSHIFT_ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.RSHIFT_ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.URSHIFT_ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.URSHIFT_ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.LSHIFT_ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.LSHIFT_ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.MOD_ASSIGN() is not null && ctx.expression().Length == 2) {
                return new AssignExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), AssignOpNode.FromSymbol(ctx.Start.Line, ctx.MOD_ASSIGN().GetText()), this.Visit(ctx.expression(1)).As<ExprNode>());
            }

            if (ctx.expression().Any() && ctx.INSTANCEOF() is not null && ctx.typeType() is not null) {
                return new RelExprNode(ctx.Start.Line, this.Visit(ctx.expression(0)).As<ExprNode>(), RelOpNode.FromSymbol(ctx.Start.Line, ctx.INSTANCEOF().GetText()), this.Visit(ctx.typeType()).As<ExprNode>());
            }

            if (ctx.LBRACK() is not null && ctx.expression().Any() && ctx.RBRACK() is not null) {
                return this.Visit(ctx.expression(0)).As<ExprNode>();
            }

            if (ctx.NEW() is not null && ctx.innerCreator() is not null) {
                FuncCallExprNode expr = this.Visit(ctx.innerCreator()).As<FuncCallExprNode>();
                var id = new IdNode(ctx.Start.Line, expr.Identifier);

                if (ctx.nonWildcardTypeArguments() is not null) {
                    TypeNameListNode type = this.Visit(ctx.nonWildcardTypeArguments()).As<TypeNameListNode>();
                    return new FuncCallExprNode(ctx.Start.Line, id, type, new ExprListNode(ctx.Start.Line, expr.As<ExprNode>()));
                } else {
                    return new FuncCallExprNode(ctx.Start.Line, id, new ExprListNode(ctx.Start.Line, expr.As<ExprNode>()));
                }
            }

            if (ctx.NEW() is not null && ctx.creator() is not null) {
                return this.Visit(ctx.creator()).As<ExprNode>();
            }

            if (ctx.SUPER() is not null && ctx.superSuffix() is not null) {
                ASTNode? args = this.Visit(ctx.superSuffix());

                if (args is ExprListNode) {
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.SUPER().ToString()), new TypeNameListNode(ctx.Start.Line), args.As<ExprListNode>());
                }

                if (args is FuncCallExprNode) {
                    var name = new IdNode(ctx.Start.Line, string.Format("{0}.{1}", ctx.SUPER().ToString(), args.As<FuncCallExprNode>().Identifier));
                    TypeNameListNode? templateArgs = args.As<FuncCallExprNode>().TemplateArguments;
                    ExprListNode? actualArgs = args.As<FuncCallExprNode>().Arguments;
                    if (templateArgs is null) {
                        return actualArgs is null 
                            ? new FuncCallExprNode(ctx.Start.Line, name)
                            : new FuncCallExprNode(ctx.Start.Line, name, actualArgs);
                    } else {
                        return actualArgs is null
                            ? new FuncCallExprNode(ctx.Start.Line, name, templateArgs)
                            : new FuncCallExprNode(ctx.Start.Line, name, templateArgs, actualArgs);
                    }
                }
            }

            if (ctx.explicitGenericInvocation() is not null) {
                return this.Visit(ctx.explicitGenericInvocation()).As<FuncCallExprNode>();
            }

            if (ctx.THIS() is not null && ctx.DOT() is not null && ctx.IDENTIFIER() is not null) {
                return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, string.Format("{0}.{1}", ctx.THIS().GetText(), ctx.IDENTIFIER().GetText())));
            }

            if (ctx.typeType() is not null && ctx.COLONCOLON() is not null) {
                TypeNameNode type = this.Visit(ctx.typeType()).As<TypeNameNode>();

                if (ctx.IDENTIFIER() is not null) {
                    if (ctx.typeArguments() is not null) {
                        TypeNameListNode types = this.Visit(ctx.typeArguments()).As<TypeNameListNode>();
                        return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText()), types);
                    } else {
                        return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText()));
                    }
                }

                if (ctx.NEW() is not null) {
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, type.GetText()));
                }
            }

            if (ctx.classType() is not null && ctx.COLONCOLON() is not null && ctx.NEW() is not null) {
                TypeDeclNode classType = this.Visit(ctx.classType()).As<TypeDeclNode>();
                var id = new IdNode(ctx.Start.Line, classType.Identifier);

                if (ctx.typeArguments() is not null) {
                    TypeNameListNode types = this.Visit(ctx.typeArguments()).As<TypeNameListNode>();
                    return new FuncCallExprNode(ctx.Start.Line, id, types);
                } else {
                    return new FuncCallExprNode(ctx.Start.Line, id);
                }
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitParExpression([NotNull] ParExpressionContext ctx)
        {
            if (ctx.LPAREN() is not null && ctx.expression() is not null && ctx.RPAREN() is not null) {
                return this.Visit(ctx.expression()).As<ExprNode>();
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitLambdaExpression([NotNull] LambdaExpressionContext ctx)
        {
            if (ctx.lambdaParameters() is not null && ctx.ARROW() is not null && ctx.lambdaBody() is not null) {
                FuncParamsNode param = this.Visit(ctx.lambdaParameters()).As<FuncParamsNode>();
                BlockStatNode def = this.Visit(ctx.lambdaBody()).As<BlockStatNode>();
                return new LambdaFuncExprNode(ctx.Start.Line, param, def);
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitLambdaParameters([NotNull] LambdaParametersContext ctx)
        {
            if (ctx.IDENTIFIER().Length == 1) {
                var declSpecs = new DeclSpecsNode(ctx.Start.Line);
                var decl = new VarDeclNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.IDENTIFIER().First().GetText()));
                return new FuncParamsNode(ctx.Start.Line, new FuncParamNode(ctx.Start.Line, declSpecs, decl));
            }

            if (ctx.LPAREN() is not null && ctx.formalParameterList() is not null && ctx.RPAREN() is not null) {
                return this.Visit(ctx.formalParameterList()).As<FuncParamsNode>();
            }

            if (ctx.LPAREN() is not null && ctx.IDENTIFIER().Length > 1 && ctx.RPAREN() is not null) {
                var declSpecs = new DeclSpecsNode(ctx.Start.Line);
                System.Collections.Generic.IEnumerable<FuncParamNode>? param = ctx.IDENTIFIER().Select(id => new FuncParamNode(ctx.Start.Line, declSpecs, new VarDeclNode(ctx.Start.Line, new IdNode(ctx.Start.Line, id.GetText()))));
                return new FuncParamsNode(ctx.Start.Line, param);
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitLambdaBody([NotNull] LambdaBodyContext ctx)
        {
            if (ctx.expression() is not null) {
                ExprNode exp = this.Visit(ctx.expression()).As<ExprNode>();
                return new BlockStatNode(ctx.Start.Line, exp);
            }

            if (ctx.block() is not null) {
                throw new NotImplementedException("Implementation pending (depends on implementation of statements)");
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitPrimary([NotNull] PrimaryContext ctx)
        {
            if (ctx.LPAREN() is not null && ctx.expression() is not null && ctx.RPAREN() is not null) {
                return this.Visit(ctx.expression()).As<ExprNode>();
            }

            if (ctx.THIS() is not null) {
                return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.THIS().ToString()));
            }

            if (ctx.SUPER() is not null) {
                return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.SUPER().ToString()));
            }

            if (ctx.literal() is not null) {
                return this.Visit(ctx.literal()).As<LitExprNode>();
            }

            if (ctx.IDENTIFIER() is not null) {
                return new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());
            }

            if (ctx.typeTypeOrVoid() is not null && ctx.DOT() is not null && ctx.CLASS() is not null) {
                TypeNameNode type = this.Visit(ctx.typeTypeOrVoid()).As<TypeNameNode>();
                return new IdNode(ctx.Start.Line, string.Format("{0}.class", type.Identifier));
            }

            if (ctx.nonWildcardTypeArguments() is not null) {
                TypeNameListNode types = this.Visit(ctx.nonWildcardTypeArguments()).As<TypeNameListNode>();

                if (ctx.explicitGenericInvocationSuffix() is not null) {
                    return this.Visit(ctx.explicitGenericInvocationSuffix()).As<FuncCallExprNode>();
                }

                if (ctx.THIS() is not null && ctx.arguments() is not null) {
                    ExprListNode args = this.Visit(ctx.arguments()).As<ExprListNode>();
                    return new FuncCallExprNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.THIS().ToString()), types, args);
                }
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitLiteral([NotNull] LiteralContext ctx)
        {
            if (ctx.integerLiteral() is not null) {
                return this.Visit(ctx.integerLiteral());
            }

            if (ctx.floatLiteral() is not null) {
                return this.Visit(ctx.floatLiteral());
            }

            if (ctx.CHAR_LITERAL() is not null) {
                return LitExprNode.FromString(ctx.Start.Line, ctx.CHAR_LITERAL().GetText());
            }

            if (ctx.STRING_LITERAL() is not null) {
                return new LitExprNode(ctx.Start.Line, ctx.STRING_LITERAL().GetText());
            }

            if (ctx.BOOL_LITERAL() is not null) {
                return LitExprNode.FromString(ctx.Start.Line, ctx.BOOL_LITERAL().GetText());
            }

            if (ctx.NULL_LITERAL() is not null) {
                return new NullLitExprNode(ctx.Start.Line);
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitIntegerLiteral([NotNull] IntegerLiteralContext ctx)
        {
            if (ctx.DECIMAL_LITERAL() is not null) {
                return LitExprNode.FromString(ctx.Start.Line, ctx.DECIMAL_LITERAL().GetText());
            }

            if (ctx.HEX_LITERAL() is not null) {
                return LitExprNode.FromString(ctx.Start.Line, ctx.HEX_LITERAL().GetText());
            }

            if (ctx.OCT_LITERAL() is not null) {
                return LitExprNode.FromString(ctx.Start.Line, ctx.OCT_LITERAL().GetText());
            }

            if (ctx.OCT_LITERAL() is not null) {
                return LitExprNode.FromString(ctx.Start.Line, ctx.OCT_LITERAL().GetText());
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitFloatLiteral([NotNull] FloatLiteralContext ctx)
        {
            if (ctx.FLOAT_LITERAL() is not null) {
                return LitExprNode.FromString(ctx.Start.Line, ctx.FLOAT_LITERAL().GetText());
            }

            if (ctx.HEX_FLOAT_LITERAL() is not null) {
                return LitExprNode.FromString(ctx.Start.Line, ctx.HEX_FLOAT_LITERAL().GetText());
            }

            throw new SyntaxErrorException("Unknown construct");
        }

        public override ASTNode VisitExpressionList([NotNull] ExpressionListContext ctx)
            => new ExprListNode(ctx.Start.Line, ctx.expression().Select(v => this.Visit(v).As<ExprNode>()));
    }
}
