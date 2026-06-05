using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Go
{
    public sealed partial class GoASTBuilder : GoParserBaseVisitor<ASTNode>, IASTBuilder<GoParser>
    {
        public override ASTNode VisitExpression(GoParser.ExpressionContext context)
        {
            if (context.primaryExpr() is not null) {
                return this.Visit(context.primaryExpr()).As<ExprNode>();
            }
            
            // antlr grammar is not exactly consistent with official spec when it comes to expressions, esp. unary ones
            // https://go.dev/ref/spec#Expression
            if (context.unary_op is not null) {
                ExprNode expr = this.Visit(context.expression()[0]).As<ExprNode>();
                if (context.unary_op.Text == "<-") {
                    return this.MarkerExpression(context.Start.Line, "__linvast_recv", expr);
                }

                UnaryOpNode op;
                if (context.unary_op.Text == "^") { // go uses unary ^ as bitwise not (binary ^ is still a XOR)
                    op = new UnaryOpNode(context.Start.Line, "^", UnaryOperations.BitwiseNotPrimitive);
                } else {
                    op = UnaryOpNode.FromSymbol(context.Start.Line, context.unary_op.Text);
                }
                return new UnaryExprNode(context.Start.Line, op, expr);
            }

            if (context.expression().Length != 2) {
                throw new SyntaxErrorException("Binary expression requires exactly two operands");
            }
            
            ExprNode lhs = this.Visit(context.expression()[0]).As<ExprNode>();
            ExprNode rhs = this.Visit(context.expression()[1]).As<ExprNode>();
            
            if (context.mul_op is not null) {
                if (context.mul_op.Text == "&^") {
                    return this.MarkerExpression(context.Start.Line, "__linvast_bit_clear", lhs, rhs);
                }

                ArithmOpNode op;
                if (context.mul_op.Text == "&") {
                    op = ArithmOpNode.FromBitwiseSymbol(context.Start.Line, context.mul_op.Text);
                } else {
                    op = ArithmOpNode.FromSymbol(context.Start.Line, context.mul_op.Text);
                }

                return new ArithmExprNode(context.Start.Line, lhs, op, rhs);
            }

            if (context.add_op is not null) {
                ArithmOpNode op;
                if (context.add_op.Text is "|" or "^") {
                    op = ArithmOpNode.FromBitwiseSymbol(context.Start.Line, context.add_op.Text);
                } else {
                    op = ArithmOpNode.FromSymbol(context.Start.Line, context.add_op.Text);
                }
                
                return new ArithmExprNode(context.Start.Line, lhs, op, rhs);
            }

            if (context.rel_op is not null) {
                var op = RelOpNode.FromSymbol(context.Start.Line, context.rel_op.Text);
                return new RelExprNode(context.Start.Line, lhs, op, rhs);
            }

            if (context.LOGICAL_AND() is not null) {
                var op = BinaryLogicOpNode.FromSymbol(context.Start.Line, "&&");
                return new LogicExprNode(context.Start.Line, lhs, op, rhs);
            }
            
            if (context.LOGICAL_OR() is not null) {
                var op = BinaryLogicOpNode.FromSymbol(context.Start.Line, "||");
                return new LogicExprNode(context.Start.Line, lhs, op, rhs);
            }

            throw new NotSupportedException("Unsupported expression: " + context);
        }

        public override ASTNode VisitExpressionList(GoParser.ExpressionListContext context) =>
            new ExprListNode(context.Start.Line, 
                context.expression().Select(e => this.Visit(e).As<ExprNode>()));

        public override ASTNode VisitPrimaryExpr(GoParser.PrimaryExprContext context)
        {
            if (context.operand() is not null) {
                return this.Visit(context.operand()).As<ExprNode>();
            }

            if (context.conversion() is not null) {
                return this.Visit(context.conversion()).As<ExprNode>();
            }

            if (context.methodExpr() is not null) {
                return this.Visit(context.methodExpr()).As<ExprNode>();
            }

            if (context.DOT() is not null && context.IDENTIFIER() is not null) {
                return new IdNode(context.Start.Line, context.GetText());
            }

            if (context.index() is not null) {
                ExprNode array = this.Visit(context.primaryExpr()).As<ExprNode>();
                ExprNode index = this.Visit(context.index()).As<ExprNode>();
                return new ArrAccessExprNode(context.Start.Line, array, index);
            }

            if (context.slice_() is not null) {
                ExprNode array = this.Visit(context.primaryExpr()).As<ExprNode>();
                ExprNode slice = this.Visit(context.slice_()).As<ExprNode>();
                return new FuncCallExprNode(
                    context.Start.Line,
                    new IdNode(context.Start.Line, "__linvast_slice"),
                    new ExprListNode(context.Start.Line, array, slice));
            }

            if (context.typeAssertion() is not null) {
                ExprNode asserted = this.Visit(context.primaryExpr()).As<ExprNode>();
                ExprNode type = new IdNode(context.Start.Line, this.Visit(context.typeAssertion()).As<TypeNameNode>().GetText());
                return new FuncCallExprNode(
                    context.Start.Line,
                    new IdNode(context.Start.Line, "__linvast_type_assert"),
                    new ExprListNode(context.Start.Line, asserted, type));
            }

            if (context.arguments() is not null) {
                // todo this doesn't look right: function can be an expression (e.g. f.p[i].x(), not only an identifier)
                IdNode fn = new IdNode(context.Start.Line, context.primaryExpr().GetText());
                ExprListNode args = this.Visit(context.arguments()).As<ExprListNode>();
                return !args.Expressions.Any() ? new FuncCallExprNode(context.Start.Line, fn) : new FuncCallExprNode(context.Start.Line, fn, args);
            }

            throw new NotSupportedException("Invalid primary expression: " + context);
        }
        
        public override ASTNode VisitArguments(GoParser.ArgumentsContext context)
        {
            if (context.ELLIPSIS() is not null) {
                ExprListNode args = context.expressionList() is null
                    ? new ExprListNode(context.Start.Line)
                    : this.Visit(context.expressionList()).As<ExprListNode>();
                return new ExprListNode(context.Start.Line, args.Expressions.Concat(new ExprNode[] { new IdNode(context.Start.Line, "...") }));
            }

            if (context.nonNamedType() is not null) {
                ExprNode type = new IdNode(context.Start.Line, context.nonNamedType().GetText());
                if (context.expressionList() is null)
                    return new ExprListNode(context.Start.Line, type);

                ExprListNode expressions = this.Visit(context.expressionList()).As<ExprListNode>();
                return new ExprListNode(context.Start.Line, new[] { type }.Concat(expressions.Expressions));
            }

            if (context.expressionList() is null) {
                return new ExprListNode(context.Start.Line);
            }

            return this.Visit(context.expressionList()).As<ExprListNode>();
        }
        
        public override ASTNode VisitMethodExpr(GoParser.MethodExprContext context) =>
            new IdNode(context.Start.Line, context.GetText());

        public override ASTNode VisitReceiverType(GoParser.ReceiverTypeContext context) => 
            new TypeNameNode(context.Start.Line, context.GetText());

        public override ASTNode VisitLiteral(GoParser.LiteralContext context) =>
            this.Visit(context.children.Single());

        public override ASTNode VisitOperand(GoParser.OperandContext context)
        {
            if (context.literal() is not null) return this.Visit(context.literal());
            if (context.operandName() is not null) return this.Visit(context.operandName());
            if (context.expression() is not null) return this.Visit(context.expression());
            throw new NotSupportedException("Invalid operand context: " + context);
        }

        public override ASTNode VisitOperandName(GoParser.OperandNameContext context) =>
            new IdNode(context.Start.Line, context.IDENTIFIER().GetText());

        public override ASTNode VisitIndex(GoParser.IndexContext context) =>
            this.Visit(context.expression()).As<ExprNode>();
        
        public override ASTNode VisitBasicLit(GoParser.BasicLitContext context)
        {
            if (context.NIL_LIT() is not null) {
                return new NullLitExprNode(context.Start.Line);
            }

            if (context.FLOAT_LIT() is not null) {
                return new LitExprNode(context.Start.Line, double.Parse(context.FLOAT_LIT().GetText()));
            }

            if (context.integer() is not null) {
                return this.Visit(context.integer()).As<LitExprNode>();
            }

            if (context.string_() is not null) {
                return this.Visit(context.string_()).As<LitExprNode>();
            }

            throw new NotSupportedException("Unsupported basic literal: " + context);
        }

        public override ASTNode VisitCompositeLit(GoParser.CompositeLitContext context)
        {
            string typeName = context.literalType().GetText();
            ExprListNode values = this.Visit(context.literalValue()).As<ExprListNode>();
            return values.Expressions.Any()
                ? new ConsExprNode(context.Start.Line, new IdNode(context.Start.Line, typeName), values)
                : new ConsExprNode(context.Start.Line, new IdNode(context.Start.Line, typeName));
        }

        public override ASTNode VisitElementList(GoParser.ElementListContext context) => 
            new ExprListNode(context.Start.Line, context.keyedElement().Select(k => this.Visit(k).As<ExprNode>()));

        public override ASTNode VisitKeyedElement(GoParser.KeyedElementContext context)
        {
            ExprNode element = this.Visit(context.element()).As<ExprNode>();
            if (context.key() is null)
                return element;

            ExprNode key = this.Visit(context.key()).As<ExprNode>();
            return new DictEntryNode(context.Start.Line, new IdNode(context.Start.Line, key.GetText()), element);
        }

        public override ASTNode VisitKey(GoParser.KeyContext context) => 
            this.Visit(context.children.Single());

        public override ASTNode VisitLiteralType(GoParser.LiteralTypeContext context) => 
            new TypeNameNode(context.Start.Line, context.GetText());

        public override ASTNode VisitLiteralValue(GoParser.LiteralValueContext context) =>
            context.elementList() is null
                ? new ExprListNode(context.Start.Line)
                : this.Visit(context.elementList()).As<ExprListNode>();
        
        public override ASTNode VisitFunctionLit(GoParser.FunctionLitContext context)
        {
            FuncNode signature = this.Visit(context.signature()).As<FuncNode>();
            BlockStatNode body = this.Visit(context.block()).As<BlockStatNode>();
            return signature.ParametersNode is null
                ? new LambdaFuncExprNode(context.Start.Line, body)
                : new LambdaFuncExprNode(context.Start.Line, signature.ParametersNode, body);
        }

        public override ASTNode VisitElement(GoParser.ElementContext context) =>
            this.Visit(context.children.Single()).As<ExprNode>();
        
        public override ASTNode VisitElementType(GoParser.ElementTypeContext context) => this.Visit(context.type_());

        public override ASTNode VisitQualifiedIdent(GoParser.QualifiedIdentContext context) =>
            new TypeNameNode(context.Start.Line, context.GetText());

        public override ASTNode VisitTypeAssertion(GoParser.TypeAssertionContext context) =>
            this.Visit(context.type_()).As<TypeNameNode>();

        public override ASTNode VisitInteger(GoParser.IntegerContext context)
        {
            if (context.RUNE_LIT() is not null) {
                string unescaped = Regex.Unescape(context.RUNE_LIT().Symbol.Text);
                if (unescaped.Length == 3 && unescaped[0] == '\'' && unescaped[2] == '\'') {
                    unescaped = unescaped[1].ToString();
                }
                if (unescaped.Length != 1) {
                    throw new NotSupportedException("Unsupported rune literal: " + context.RUNE_LIT());
                }

                return new LitExprNode(context.Start.Line, unescaped[0]);
            }

            if (context.IMAGINARY_LIT() is not null) {
                return new LitExprNode(context.Start.Line, context.IMAGINARY_LIT().GetText());
            }

            if (context.DECIMAL_LIT() is not null) {
                return new LitExprNode(context.Start.Line, long.Parse(context.DECIMAL_LIT().GetText()));
            }

            if (context.HEX_LIT() is not null) {
                return new LitExprNode(context.Start.Line, long.Parse(context.HEX_LIT().GetText(), NumberStyles.HexNumber));
            }
            
            if (context.BINARY_LIT() is not null) {
                return new LitExprNode(context.Start.Line, Convert.ToInt64(context.BINARY_LIT().GetText()[2..], 2));
            }
            
            if (context.OCTAL_LIT() is not null) {
                return new LitExprNode(context.Start.Line, Convert.ToInt64(context.OCTAL_LIT().GetText(), 8));
            }

            throw new NotSupportedException("Unsupported integer literal: " + context);
        }

        public override ASTNode VisitString_(GoParser.String_Context context)
        {
            string text = context.GetText();
            if (text[0] == '`' && text.Last() == '`')
                return new LitExprNode(context.Start.Line, text[1..^1]);

            if (text[0] != '"' || text.Last() != '"') {
                throw new Exceptions.SyntaxErrorException("String literal without quotes");
            }
            return new LitExprNode(context.Start.Line, 
                text[1..(text.Length-1)]);
        } 

        public override ASTNode VisitSlice_(GoParser.Slice_Context context) =>
            new IdNode(context.Start.Line, context.GetText());
    }
}
