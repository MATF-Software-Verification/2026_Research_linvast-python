using System;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Logging;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    [ASTBuilder(".py")]
    public sealed partial class PythonASTBuilder : Python3ParserBaseVisitor<ASTNode>, IASTBuilder<Python3Parser>
    {
        public ASTNode BuildFromSource(string code) => this.Visit(this.CreateParser(code).file_input());

        public Python3Parser CreateParser(string code)
        {
            ICharStream stream = CharStreams.fromstring(code);
            var lexer = new Python3Lexer(stream);
            lexer.AddErrorListener(new ThrowExceptionErrorListener());
            ITokenStream tokens = new CommonTokenStream(lexer);
            var parser = new Python3Parser(tokens);
            parser.BuildParseTree = true;
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowExceptionErrorListener());
            return parser;
        }

        public ASTNode BuildFromSource(string code, Func<Python3Parser, ParserRuleContext> entryProvider) =>
            this.Visit(entryProvider(this.CreateParser(code)));

        public override ASTNode Visit(IParseTree tree)
        {
            LogObj.Visit(tree as ParserRuleContext);
            try {
                return base.Visit(tree);
            } catch (NullReferenceException e) {
                throw new SyntaxErrorException("Source file contained unexpected content", e);
            }
        }

        public override ASTNode VisitFile_input(Python3Parser.File_inputContext ctx)
        {
            var children = ctx.stmt().Select(this.Visit);
            return new SourceNode(children);
        }

        public override ASTNode VisitStmt(Python3Parser.StmtContext ctx)
        {
            if (ctx.simple_stmts() is not null)
                return this.Visit(ctx.simple_stmts());
            return this.Visit(ctx.compound_stmt());
        }

        public override ASTNode VisitSimple_stmts(Python3Parser.Simple_stmtsContext ctx)
        {
            var stmts = ctx.simple_stmt().Select(this.Visit).ToArray();
            if (stmts.Length == 1)
                return stmts[0];
            return new BlockStatNode(ctx.Start.Line, stmts);
        }

        public override ASTNode VisitSimple_stmt(Python3Parser.Simple_stmtContext ctx) =>
            this.Visit(ctx.children.Single(c => c is ParserRuleContext));
    }
}
