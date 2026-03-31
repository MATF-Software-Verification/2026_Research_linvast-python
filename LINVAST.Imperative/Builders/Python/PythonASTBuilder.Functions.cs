using System;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    public sealed partial class PythonASTBuilder : Python3ParserBaseVisitor<ASTNode>, IASTBuilder<Python3Parser>
    {
        // funcdef: 'def' name parameters ('->' test)? ':' block
        public override ASTNode VisitFuncdef(Python3Parser.FuncdefContext ctx) =>
            throw new NotImplementedException("funcdef");

        // async_funcdef: ASYNC funcdef
        public override ASTNode VisitAsync_funcdef(Python3Parser.Async_funcdefContext ctx) =>
            throw new NotImplementedException("async_funcdef");

        // parameters: '(' typedargslist? ')'
        public override ASTNode VisitParameters(Python3Parser.ParametersContext ctx) =>
            throw new NotImplementedException("parameters");

        // typedargslist: tfpdef ('=' test)? ... | '*' tfpdef? ... | '**' tfpdef
        public override ASTNode VisitTypedargslist(Python3Parser.TypedargslistContext ctx) =>
            throw new NotImplementedException("typedargslist");

        // tfpdef: name (':' test)?
        public override ASTNode VisitTfpdef(Python3Parser.TfpdefContext ctx) =>
            throw new NotImplementedException("tfpdef");

        // varargslist: vfpdef ('=' test)? ... | '*' vfpdef? ... | '**' vfpdef
        public override ASTNode VisitVarargslist(Python3Parser.VarargslistContext ctx) =>
            throw new NotImplementedException("varargslist");

        // vfpdef: name
        public override ASTNode VisitVfpdef(Python3Parser.VfpdefContext ctx) =>
            throw new NotImplementedException("vfpdef");

        // lambdef: 'lambda' varargslist? ':' test
        public override ASTNode VisitLambdef(Python3Parser.LambdefContext ctx) =>
            throw new NotImplementedException("lambdef");

        // lambdef_nocond: 'lambda' varargslist? ':' test_nocond
        public override ASTNode VisitLambdef_nocond(Python3Parser.Lambdef_nocondContext ctx) =>
            throw new NotImplementedException("lambdef_nocond");

        // decorator: '@' dotted_name ('(' arglist? ')')? NEWLINE
        public override ASTNode VisitDecorator(Python3Parser.DecoratorContext ctx) =>
            throw new NotImplementedException("decorator");

        // decorators: decorator+
        public override ASTNode VisitDecorators(Python3Parser.DecoratorsContext ctx) =>
            throw new NotImplementedException("decorators");

        // decorated: decorators (classdef | funcdef | async_funcdef)
        public override ASTNode VisitDecorated(Python3Parser.DecoratedContext ctx) =>
            throw new NotImplementedException("decorated");
    }
}
