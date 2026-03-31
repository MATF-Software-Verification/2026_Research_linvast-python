using System.Linq;
using Antlr4.Runtime;
using Serilog;
using Serilog.Events;

namespace LINVAST.Logging
{
    public static class LogObj
    {
        public static void Context(ParserRuleContext ctx, LogEventLevel level = LogEventLevel.Debug)
        {
            Log.Write(
                level,
                "[{Depth}:{ContextType}] [{SourceInterval}] | children: {ChildrenCount} | {Code}",
                ctx.Depth(),
                ctx.GetType().Name,
                ctx.SourceInterval,
                ctx.ChildCount,
                ctx.GetText()
            );
        }

        public static void Visit(ParserRuleContext? ctx, LogEventLevel level = LogEventLevel.Debug)
        {
            if (ctx is null)
                return;

            Log.Write(
                level,
                "Visiting [L{Line}:C{Column}:D{Depth}:{ContextType}] | children: {ChildrenCount} | {CodeInit} ...",
                ctx.Start.Line,
                ctx.Start.Column,
                ctx.Depth(),
                ctx.GetType().Name,
                ctx.ChildCount,
                string.Join(string.Empty, ctx.GetText().Take(30))
            );
        }
    }
}
