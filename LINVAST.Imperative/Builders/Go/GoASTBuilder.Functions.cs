using System;
using System.Linq;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Go
{
    public sealed partial class GoASTBuilder : GoParserBaseVisitor<ASTNode>, IASTBuilder<GoParser>
    {
        public override ASTNode VisitFunctionDecl(GoParser.FunctionDeclContext context)
        {
            var funcName = new IdNode(context.Start.Line, context.IDENTIFIER().GetText());
            FuncNode signature = this.Visit(context.signature()).As<FuncNode>();
            BlockStatNode? body = null;
            if (context.block() is not null) {
                body = this.Visit(context.block()).As<BlockStatNode>();
            }

            var declSpecs = (DeclSpecsNode)signature.ChildrenWithoutTags.ElementAt(0);

            if (signature.ParametersNode is null && body is null) {
                return new FuncNode(context.Start.Line, declSpecs,
                    new FuncDeclNode(context.Start.Line, funcName));
            }

            if (signature.ParametersNode is not null && body is not null) {
                return new FuncNode(context.Start.Line, declSpecs,
                    new FuncDeclNode(context.Start.Line, funcName, signature.ParametersNode, body));
            }

            if (signature.ParametersNode is not null) {
                return new FuncNode(context.Start.Line, declSpecs,
                    new FuncDeclNode(context.Start.Line, funcName, signature.ParametersNode));
            }

            if (body is not null) {
                return new FuncNode(context.Start.Line, declSpecs,
                    new FuncDeclNode(context.Start.Line, funcName, body));
            }

            throw new Exception("Unreachable code was reached!");
        }

        public override ASTNode VisitParameters(GoParser.ParametersContext context)
        {
            var paramsArr = context.parameterDecl()
                .Select(p => this.Visit(p).As<FuncParamsNode>()).ToList();
            var funcParamsNode = new FuncParamsNode(context.Start.Line,
                paramsArr.SelectMany(p=>p.Parameters));
            if (paramsArr.Any() && paramsArr.Last().IsVariadic) {
                funcParamsNode.IsVariadic = true;
            }

            return funcParamsNode;
        }

        // note this returns FuncParamsNode, NOT FuncParamNode !
        public override ASTNode VisitParameterDecl(GoParser.ParameterDeclContext context)
        {
            TypeNameNode paramType = this.Visit(context.type_()).As<TypeNameNode>();
            var paramDeclSpec = new DeclSpecsNode(context.Start.Line, paramType);
            FuncParamNode[] @params;
            if (context.identifierList() is null) {
                @params = new FuncParamNode[]
                { new (context.Start.Line, paramDeclSpec, new VarDeclNode(context.Start.Line, 
                    new IdNode(context.Start.Line, "."))) };
            } else {
                IdListNode decls = this.Visit(context.identifierList()).As<IdListNode>();
                @params = decls.Identifiers
                    .Select(d => new FuncParamNode(context.Start.Line, paramDeclSpec, 
                        new VarDeclNode(context.Start.Line, d)))
                    .ToArray();
            }

            var funcParamsNode = new FuncParamsNode(context.Start.Line, @params);
            if (context.ELLIPSIS() is not null) {
                funcParamsNode.IsVariadic = true;
            }

            return funcParamsNode;
        }

        // this effectively returns a FuncNode for unnamed function without a body,
        // as there is no {return value, params} ASTNode
        // (not the greatest solution)
        public override ASTNode VisitSignature(GoParser.SignatureContext context)
        {
            DeclSpecsNode? retTypeNode = null;
            GoParser.ResultContext? resultContext = context.result();
            if (resultContext is not null) {
                if (resultContext.parameters() is not null) {
                    throw new NotImplementedException("Parameters as return value of a function are not supported");
                }

                TypeNameNode retType = this.Visit(resultContext.type_()).As<TypeNameNode>();

                retTypeNode = new DeclSpecsNode(context.Start.Line, retType);
            } else {
                retTypeNode = new DeclSpecsNode(context.Start.Line, "void");
            }

            FuncParamsNode @params = this.Visit(context.parameters()).As<FuncParamsNode>();
            var funcDecl = new FuncDeclNode(context.Start.Line, 
                new IdNode(context.Start.Line, "."), @params);

            return new FuncNode(context.Start.Line, retTypeNode, funcDecl);
        }
        
        # region method-specific stuff

        public override ASTNode VisitMethodDecl(GoParser.MethodDeclContext context)
        {
            FuncParamNode receiver = this.Visit(context.receiver()).As<FuncParamNode>();
            var funcName = new IdNode(context.Start.Line, receiver.Specifiers.TypeName + "." + context.IDENTIFIER().GetText());
            // todo proper receiver params (e.g. we're omitting receiver param name here)
            
            FuncNode signature = this.Visit(context.signature()).As<FuncNode>();
            BlockStatNode? body = null;
            if (context.block() is not null) {
                body = this.Visit(context.block()).As<BlockStatNode>();
            }

            var declSpecs = (DeclSpecsNode)signature.ChildrenWithoutTags.ElementAt(0);

            if (signature.ParametersNode is null && body is null) {
                return new FuncNode(context.Start.Line, declSpecs,
                    new FuncDeclNode(context.Start.Line, funcName));
            }

            if (signature.ParametersNode is not null) {
                return new FuncNode(context.Start.Line, declSpecs,
                    new FuncDeclNode(context.Start.Line, funcName, signature.ParametersNode));
            }

            if (body is not null) {
                return new FuncNode(context.Start.Line, declSpecs,
                    new FuncDeclNode(context.Start.Line, funcName, body));
            }

            return new FuncNode(context.Start.Line, declSpecs,
                new FuncDeclNode(context.Start.Line, funcName, signature.ParametersNode, body));
        }

        public override ASTNode VisitReceiver(GoParser.ReceiverContext context)
        {
            FuncParamsNode receiver = this.Visit(context.parameters()).As<FuncParamsNode>();
            if (receiver.IsVariadic) {
                throw new NotSupportedException("Receiver type cannot be variadic!");
            }

            if (receiver.Parameters.Count() > 1) {
                throw new NotSupportedException("Receiver cannot have multiple params!");
            }

            return receiver.Parameters.Single();
        }

        #endregion
    }
}