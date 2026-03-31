using System;
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
        
        public override ASTNode VisitSliceType(GoParser.SliceTypeContext context) => this.Visit(context.elementType()).As<TypeNode>();

        public override ASTNode VisitFunctionType(GoParser.FunctionTypeContext context) => this.Visit(context.signature());

        public override ASTNode VisitInterfaceType(GoParser.InterfaceTypeContext context) => throw new NotImplementedException("Interface type");
        
        public override ASTNode VisitArrayType(GoParser.ArrayTypeContext context) => throw new NotImplementedException("Array type");
        
        public override ASTNode VisitStructType(GoParser.StructTypeContext context) => throw new NotImplementedException("Struct type");

        public override ASTNode VisitPointerType(GoParser.PointerTypeContext context) => throw new NotImplementedException("Pointer type");

        public override ASTNode VisitMethodSpec(GoParser.MethodSpecContext context) => throw new NotImplementedException("Method type");

        public override ASTNode VisitMapType(GoParser.MapTypeContext context) => throw new NotImplementedException("Map type");
      
        public override ASTNode VisitChannelType(GoParser.ChannelTypeContext context) => throw new NotImplementedException("Channel type");

        public override ASTNode VisitConversion(GoParser.ConversionContext context) => throw new NotImplementedException("Conversion");
        
        public override ASTNode VisitEmbeddedField(GoParser.EmbeddedFieldContext context) => throw new NotImplementedException("Embedded field");
        public override ASTNode VisitFieldDecl(GoParser.FieldDeclContext context) => throw new NotImplementedException("Field decl");
    }
}