using System;
using Antlr4.Runtime;
using LINVAST.Nodes;

namespace LINVAST.Builders
{
    public interface IASTBuilder<TParser> : IAbstractASTBuilder where TParser : Parser
    {
        TParser CreateParser(string code);
        ASTNode BuildFromSource(string code, Func<TParser, ParserRuleContext> entryProvider);
    }
}
