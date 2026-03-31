using System;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    public sealed partial class PythonASTBuilder : Python3ParserBaseVisitor<ASTNode>, IASTBuilder<Python3Parser>
    {
        // classdef: 'class' name ('(' arglist? ')')? ':' block
        public override ASTNode VisitClassdef(Python3Parser.ClassdefContext ctx) =>
            throw new NotImplementedException("classdef");
    }
}
