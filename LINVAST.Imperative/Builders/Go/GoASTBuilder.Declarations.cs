
using System.Linq;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using System;
using System.Collections.Generic;
using System.Data;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes.Common;
using SyntaxErrorException = LINVAST.Exceptions.SyntaxErrorException;


namespace LINVAST.Imperative.Builders.Go
{
    public sealed partial class GoASTBuilder : GoParserBaseVisitor<ASTNode>, IASTBuilder<GoParser>
    {
        public override ASTNode VisitPackageClause(GoParser.PackageClauseContext context) =>
            new PackageNode(context.Start.Line, context.packageName.Text);

        public override ASTNode VisitDeclaration(GoParser.DeclarationContext context) => this.Visit(context.children.Single());

        public override ASTNode VisitIdentifierList(GoParser.IdentifierListContext context) => new IdListNode(context.Start.Line, context.IDENTIFIER().Select(e => new IdNode(context.Start.Line, e.GetText())));

        public override ASTNode VisitImportDecl(GoParser.ImportDeclContext context) => new ImportListNode(context.Start.Line, context.importSpec().Select(i => this.Visit(i).As<ImportNode>()));

        public override ASTNode VisitImportSpec(GoParser.ImportSpecContext context)
        {
            var importPath = new ImportNode(context.Start.Line, this.Visit(context.importPath()).As<IdNode>().Identifier);

            if (context.DOT() is null && context.IDENTIFIER() is null) {
                return importPath;
            }

            if (context.DOT() is not null) {
                return new ImportNode(context.Start.Line, importPath.Directive, "");
            }

            if (context.IDENTIFIER() is not null) {
                return new ImportNode(context.Start.Line, importPath.Directive, context.IDENTIFIER().GetText());
            }

            throw new SyntaxErrorException("Invalid import");
        }

        public override ASTNode VisitImportPath(GoParser.ImportPathContext context) => new IdNode(context.Start.Line, context.string_().GetText());

        public override ASTNode VisitVarDecl(GoParser.VarDeclContext context)
        {
            if (context.varSpec().Count() == 1) {
                return this.Visit(context.varSpec().First()).As<DeclStatNode>();
            }

            return new BlockStatNode(context.Start.Line, context.varSpec().Select(vs => this.Visit(vs).As<DeclStatNode>()));
        }

        public override ASTNode VisitVarSpec(GoParser.VarSpecContext context)
        {
            IdListNode idListNodes = this.Visit(context.identifierList()).As<IdListNode>();
            DeclSpecsNode type;

            type = context.type_() is not null
                ? new DeclSpecsNode(context.Start.Line, this.Visit(context.type_()).As<TypeNameNode>())
                : new DeclSpecsNode(context.Start.Line, InferExpressionListTypeName(context.expressionList()));
            ExprListNode? exprList = null;
            if (context.expressionList() is not null) {
                exprList = this.Visit(context.expressionList()).As<ExprListNode>();
            }

            DeclListNode d = new DeclListNode(context.Start.Line, this.BuildVarDeclarators(context.Start.Line, idListNodes.Identifiers, exprList));
            return new DeclStatNode(context.Start.Line, type, d);
        }

        public override ASTNode VisitShortVarDecl(GoParser.ShortVarDeclContext context)
        {
            IdListNode idListNodes = this.Visit(context.identifierList()).As<IdListNode>();
            ExprListNode exprList = this.Visit(context.expressionList()).As<ExprListNode>();
            DeclSpecsNode type;

            type = new DeclSpecsNode(context.Start.Line, InferExpressionListTypeName(context.expressionList()));
            
            IEnumerable<VarDeclNode> idExprList = this.BuildVarDeclarators(context.Start.Line, idListNodes.Identifiers, exprList);
            DeclListNode declList = new DeclListNode(context.Start.Line, idExprList);
            return new DeclStatNode(context.Start.Line, type,declList);
        }

        public override ASTNode VisitConstSpec(GoParser.ConstSpecContext context)
        {
            IdListNode idListNodes = this.Visit(context.identifierList()).As<IdListNode>();
            DeclSpecsNode type;

            type = context.type_() is not null
                ? new DeclSpecsNode(context.Start.Line, "const", this.Visit(context.type_()).As<TypeNameNode>())
                : new DeclSpecsNode(context.Start.Line, "const", context.expressionList() is null ? "object" : InferExpressionListTypeName(context.expressionList()));

            ExprListNode? exprList = null;
            if (context.expressionList() is not null) {
                exprList = this.Visit(context.expressionList()).As<ExprListNode>();
            }

            DeclListNode d = new DeclListNode(context.Start.Line, this.BuildVarDeclarators(context.Start.Line, idListNodes.Identifiers, exprList));
            return new DeclStatNode(context.Start.Line, type, d);
        }

        public override ASTNode VisitConstDecl(GoParser.ConstDeclContext context)
        {
            if (context.constSpec().Count() == 1) {
                return this.Visit(context.constSpec().First()).As<DeclStatNode>();
            } 
            
            return new BlockStatNode(context.Start.Line, context.constSpec().Select(cs => this.Visit(cs).As<DeclStatNode>()));
        }
        
        public override ASTNode VisitTypeDecl(GoParser.TypeDeclContext context)
        {
            if (context.typeSpec().Count() == 1)
                return this.Visit(context.typeSpec().First());

            return new BlockStatNode(context.Start.Line, context.typeSpec().Select(this.Visit));
        }

        public override ASTNode VisitTypeSpec(GoParser.TypeSpecContext context)
        {
            var identifier = new IdNode(context.Start.Line, context.IDENTIFIER().GetText());
            TypeNameNode typeName = this.Visit(context.type_()).As<TypeNameNode>();
            string modifier = context.ASSIGN() is null ? "type" : "type alias";
            var declSpecs = new DeclSpecsNode(context.Start.Line, modifier, typeName);
            var declList = new DeclListNode(context.Start.Line, new VarDeclNode(context.Start.Line, identifier));
            return new DeclStatNode(context.Start.Line, declSpecs, declList);
        }

        private string InferExpressionListTypeName(GoParser.ExpressionListContext context)
        {
            ExprNode[] expressions = context.expression().Select(e => this.Visit(e).As<ExprNode>()).ToArray();
            if (expressions.Length == 1 && expressions[0] is LitExprNode literal)
                return literal.TypeCode.ToString();

            return "object";
        }

        private IEnumerable<VarDeclNode> BuildVarDeclarators(int line, IEnumerable<IdNode> identifiers, ExprListNode? exprList)
        {
            IdNode[] ids = identifiers.ToArray();
            ExprNode[] expressions = exprList?.Expressions.ToArray() ?? Array.Empty<ExprNode>();

            if (expressions.Length == 0)
                return ids.Select(id => new VarDeclNode(line, id));

            if (expressions.Length == ids.Length)
                return ids.Zip(expressions, (id, expr) => new VarDeclNode(line, id, expr));

            if (expressions.Length == 1 && ids.Length > 1) {
                return ids.Select((id, index) =>
                    new VarDeclNode(
                        line,
                        id,
                        this.MarkerExpression(
                            line,
                            "__linvast_multi_value",
                            new IdNode(line, expressions[0].GetText()),
                            new LitExprNode(line, index))));
            }

            return ids.Select((id, index) =>
                index < expressions.Length
                    ? new VarDeclNode(line, id, expressions[index])
                    : new VarDeclNode(line, id));
        }
    }
}
