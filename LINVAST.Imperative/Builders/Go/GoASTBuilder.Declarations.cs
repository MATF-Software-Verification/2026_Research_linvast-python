
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
            DeclListNode d;
            IEnumerable<VarDeclNode> idExprList;
            DeclSpecsNode type;

            if (context.type_() is not null) {
                type = new DeclSpecsNode(context.Start.Line, context.type_().GetText());
            } else {
                if (context.expressionList().children.Count > 1) {
                    throw new NotImplementedException("Not implemented.");
                }
                ExprNode t = this.Visit(context.expressionList().children.First()).As<ExprNode>();
                TypeCode exprType;
                if (t is not LitExprNode) {
                    throw new NotImplementedException("Not implemented.");
                } 
                
                exprType = t.As<LitExprNode>().TypeCode;
                type = new DeclSpecsNode(context.Start.Line, exprType.ToString());
            }
            if (context.expressionList() is not null) {
                ExprListNode exprList = this.Visit(context.expressionList()).As<ExprListNode>();
                idExprList = idListNodes.Identifiers.Zip(exprList.Expressions, (i, e) => new VarDeclNode(context.Start.Line, i, e));
            } else {
                idExprList = idListNodes.Identifiers.Select(i => new VarDeclNode(context.Start.Line, i));
            }

            d = new DeclListNode(context.Start.Line, idExprList);
            return new DeclStatNode(context.Start.Line, type, d);
        }

        public override ASTNode VisitShortVarDecl(GoParser.ShortVarDeclContext context)
        {
            IdListNode idListNodes = this.Visit(context.identifierList()).As<IdListNode>();
            ExprListNode exprList = this.Visit(context.expressionList()).As<ExprListNode>();
            DeclSpecsNode type;

            if (exprList.Children.Count > 1) {
                throw new NotImplementedException("Not implemented.");
            }

            ExprNode t = this.Visit(context.expressionList().children.First()).As<ExprNode>();
            TypeCode exprType;
            if (t is not LitExprNode) {
                throw new NotImplementedException("Not implemented.");
            } 
                
            exprType = t.As<LitExprNode>().TypeCode;
            type = new DeclSpecsNode(context.Start.Line, exprType.ToString());
            
            IEnumerable<VarDeclNode> idExprList = idListNodes.Identifiers.Zip(exprList.Expressions, (i, e) => new VarDeclNode(context.Start.Line, i, e));
            DeclListNode declList = new DeclListNode(context.Start.Line, idExprList);
            return new DeclStatNode(context.Start.Line, type,declList);
        }

        public override ASTNode VisitConstSpec(GoParser.ConstSpecContext context)
        {
            IdListNode idListNodes = this.Visit(context.identifierList()).As<IdListNode>();
            DeclListNode d;
            IEnumerable<VarDeclNode> idExprList;
            DeclSpecsNode type;

            if (context.type_() is not null) {
                type = new DeclSpecsNode(context.Start.Line, "const", context.type_().GetText());
            } else {
                if (context.expressionList() is not null) {
                    if (context.expressionList().children.Count > 1) {
                        throw new NotImplementedException("Not implemented.");
                    }
                    ExprNode t = this.Visit(context.expressionList().children.First()).As<ExprNode>();
                    TypeCode exprType;
                    if (t is not LitExprNode) {
                        throw new NotImplementedException("Not implemented.");
                    } 
                
                    exprType = t.As<LitExprNode>().TypeCode;
                    type = new DeclSpecsNode(context.Start.Line, "const", exprType.ToString());   
                } else 
                    throw new NotImplementedException("Not implemented.");
            }

            if (context.expressionList() is not null) {
                ExprListNode exprList = this.Visit(context.expressionList()).As<ExprListNode>();
                idExprList = idListNodes.Identifiers.Zip(exprList.Expressions, (i, e) => new VarDeclNode(context.Start.Line, i, e));
            } else 
                idExprList = idListNodes.Identifiers.Select(i => new VarDeclNode(context.Start.Line, i));

            d = new DeclListNode(context.Start.Line, idExprList);
            return new DeclStatNode(context.Start.Line, type, d);
        }

        public override ASTNode VisitConstDecl(GoParser.ConstDeclContext context)
        {
            if (context.constSpec().Count() == 1) {
                return this.Visit(context.constSpec().First()).As<DeclStatNode>();
            } 
            
            return new BlockStatNode(context.Start.Line, context.constSpec().Select(cs => this.Visit(cs).As<DeclStatNode>()));
        }
        
        public override ASTNode VisitTypeDecl(GoParser.TypeDeclContext context) => base.VisitTypeDecl(context);

        public override ASTNode VisitTypeSpec(GoParser.TypeSpecContext context) => base.VisitTypeSpec(context);
    }
}