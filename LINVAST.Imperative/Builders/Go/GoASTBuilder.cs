using System;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Logging;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Go
{
    [ASTBuilder(".go")]
    public sealed partial class GoASTBuilder : GoParserBaseVisitor<ASTNode>, IASTBuilder<GoParser>
    {
        public ASTNode BuildFromSource(string code) => this.Visit(this.CreateParser(code).sourceFile());

        public GoParser CreateParser(string code) {
            ICharStream stream = CharStreams.fromstring(code);
            var lexer = new GoLexer(stream);
            lexer.AddErrorListener(new ThrowExceptionErrorListener());
            ITokenStream tokens = new CommonTokenStream(lexer);
            var parser = new GoParser(tokens);
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowExceptionErrorListener());
            return parser;
        }

        public ASTNode BuildFromSource(string code, Func<GoParser, ParserRuleContext> entryProvider) => 
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

        public override ASTNode VisitSourceFile(GoParser.SourceFileContext ctx)
        {
            if (ctx.packageClause() is not null) {
                // TODO package declaration
            }
            
            var imports = ctx.importDecl().Select(this.Visit);
            var functions = ctx.functionDecl().Select(this.Visit);
            var methods = ctx.methodDecl().Select(this.Visit);
            var declarations = ctx.declaration().Select(this.Visit);
            return new SourceNode(imports.Concat(functions).Concat(methods).Concat(declarations));
        }
    }
}