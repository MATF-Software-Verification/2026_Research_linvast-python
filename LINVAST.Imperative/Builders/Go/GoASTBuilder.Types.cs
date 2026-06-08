using System;
using System.Collections.Generic;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using System.Linq;


namespace LINVAST.Imperative.Builders.Go
{
    public sealed partial class GoASTBuilder : GoParserBaseVisitor<ASTNode>, IASTBuilder<GoParser>
    {
        public override ASTNode VisitType_(GoParser.Type_Context context)
        {
            if (context.typeName() is not null) {
                return this.Visit(context.typeName()).As<TypeNameNode>();
            }
            if (context.typeLit() is not null) {
                return this.Visit(context.typeLit()).As<TypeNameNode>();
            }

            return this.Visit(context.type_()).As<TypeNameNode>();
        }

        public override ASTNode VisitTypeName(GoParser.TypeNameContext context)
        {
            if (context.qualifiedIdent() is not null) {
                return this.Visit(context.qualifiedIdent()).As<TypeNameNode>();
            }
            string name = context.IDENTIFIER().GetText();
            return new TypeNameNode(context.Start.Line, name);
        }

        public override ASTNode VisitTypeLit(GoParser.TypeLitContext context) => this.Visit(context.children.Single());

        public override ASTNode VisitTypeList(GoParser.TypeListContext context)
        {
            if (context.type_() is not null) {
                return new TypeNameListNode(context.Start.Line, context.type_().Select(t => this.Visit(t).As<TypeNameNode>()));
            };

            return new TypeNameListNode(context.Start.Line);
        }

        public override ASTNode VisitNonNamedType(GoParser.NonNamedTypeContext context)
        {
            if (context.typeLit() is not null) {
                return this.Visit(context.typeLit()).As<TypeNameNode>();
            }

            return this.Visit(context.nonNamedType()).As<TypeNameNode>();
        }

        public override ASTNode VisitArrayLength(GoParser.ArrayLengthContext context) => this.Visit(context.expression()).As<ExprNode>();
        
        public override ASTNode VisitResult(GoParser.ResultContext context)
        {
            if (context.parameters() is not null) {
                return this.Visit(context.parameters());
            }
            return this.Visit(context.type_());
        }
        
        public override ASTNode VisitSliceType(GoParser.SliceTypeContext context)
            => new TypeNameNode(context.Start.Line, $"[]{this.Visit(context.elementType()).As<TypeNameNode>().GetText()}");

        public override ASTNode VisitFunctionType(GoParser.FunctionTypeContext context) => this.Visit(context.signature());

        public override ASTNode VisitInterfaceType(GoParser.InterfaceTypeContext context)
            => new TypeNameNode(context.Start.Line, context.GetText());
        
        public override ASTNode VisitArrayType(GoParser.ArrayTypeContext context)
            => new TypeNameNode(
                context.Start.Line,
                $"[{this.Visit(context.arrayLength()).As<ExprNode>().GetText()}]{this.Visit(context.elementType()).As<TypeNameNode>().GetText()}");
        
        public override ASTNode VisitStructType(GoParser.StructTypeContext context)
            => new TypeNameNode(context.Start.Line, context.GetText());

        public override ASTNode VisitPointerType(GoParser.PointerTypeContext context)
            => new TypeNameNode(context.Start.Line, $"*{this.Visit(context.type_()).As<TypeNameNode>().GetText()}");

        public override ASTNode VisitMethodSpec(GoParser.MethodSpecContext context)
        {
            var identifier = new IdNode(context.Start.Line, context.IDENTIFIER().GetText());
            FuncParamsNode parameters = this.Visit(context.parameters()).As<FuncParamsNode>();
            TypeNameNode returnType = context.result() is null
                ? new TypeNameNode(context.Start.Line, "void")
                : ResultTypeName(context.result());
            var declSpecs = new DeclSpecsNode(context.Start.Line, returnType);
            return new DeclStatNode(context.Start.Line, declSpecs, new DeclListNode(context.Start.Line, new FuncDeclNode(context.Start.Line, identifier, parameters)));
        }

        public override ASTNode VisitMapType(GoParser.MapTypeContext context)
            => new TypeNameNode(
                context.Start.Line,
                $"map[{this.Visit(context.type_()).As<TypeNameNode>().GetText()}]{this.Visit(context.elementType()).As<TypeNameNode>().GetText()}");
      
        public override ASTNode VisitChannelType(GoParser.ChannelTypeContext context)
            => new TypeNameNode(context.Start.Line, context.GetText());

        public override ASTNode VisitConversion(GoParser.ConversionContext context)
        {
            TypeNameNode type = this.Visit(context.nonNamedType()).As<TypeNameNode>();
            ExprNode expression = this.Visit(context.expression()).As<ExprNode>();
            return new ConsExprNode(context.Start.Line, new IdNode(context.Start.Line, type.GetText()), new ExprListNode(context.Start.Line, expression));
        }
        
        public override ASTNode VisitEmbeddedField(GoParser.EmbeddedFieldContext context)
            => new TypeNameNode(context.Start.Line, context.GetText());

        public override ASTNode VisitFieldDecl(GoParser.FieldDeclContext context)
        {
            TypeNameNode type = context.type_() is not null
                ? this.Visit(context.type_()).As<TypeNameNode>()
                : this.Visit(context.embeddedField()).As<TypeNameNode>();
            var declSpecs = new DeclSpecsNode(context.Start.Line, type);
            IEnumerable<DeclNode> declarators = context.identifierList() is null
                ? new[] { new VarDeclNode(context.Start.Line, new IdNode(context.Start.Line, type.GetText())) }
                : this.Visit(context.identifierList()).As<IdListNode>().Identifiers.Select(id => new VarDeclNode(id.Line, id));
            return new DeclStatNode(context.Start.Line, declSpecs, new DeclListNode(context.Start.Line, declarators));
        }

        private TypeNameNode ResultTypeName(GoParser.ResultContext context)
        {
            if (context.type_() is not null)
                return this.Visit(context.type_()).As<TypeNameNode>();

            FuncParamsNode parameters = this.Visit(context.parameters()).As<FuncParamsNode>();
            return new TypeNameNode(context.Start.Line, $"({string.Join(", ", parameters.Parameters.Select(ResultParameterText))})");
        }

        private static string ResultParameterText(FuncParamNode parameter)
        {
            string identifier = parameter.Declarator.Identifier;
            return identifier == "."
                ? parameter.Specifiers.TypeName
                : $"{parameter.Specifiers.TypeName} {identifier}";
        }
    }
}
