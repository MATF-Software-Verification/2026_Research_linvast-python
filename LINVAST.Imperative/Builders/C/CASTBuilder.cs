using System;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Logging;
using LINVAST.Nodes;
using static LINVAST.Imperative.Builders.C.CParser;

namespace LINVAST.Imperative.Builders.C
{
    [ASTBuilder(".c")]
    public sealed partial class CASTBuilder : CBaseVisitor<ASTNode>, IASTBuilder<CParser>
    {
        public CParser CreateParser(string code)
        {
            ICharStream stream = CharStreams.fromstring(code);
            var lexer = new CLexer(stream);
            lexer.AddErrorListener(new ThrowExceptionErrorListener());
            ITokenStream tokens = new CommonTokenStream(lexer);
            var parser = new CParser(tokens);
            parser.BuildParseTree = true;
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowExceptionErrorListener());
            return parser;
        }

        public ASTNode BuildFromSource(string code)
            => this.Visit(this.CreateParser(code).compilationUnit());

        public ASTNode BuildFromSource(string code, Func<CParser, ParserRuleContext> entryProvider)
            => this.Visit(entryProvider(this.CreateParser(code)));


        public override ASTNode Visit(IParseTree tree)
        {
            LogObj.Visit(tree as ParserRuleContext);
            try {
                return base.Visit(tree);
            } catch (NullReferenceException e) {
                throw new SyntaxErrorException("Source file contained unexpected content", e);
            }
        }

        public override ASTNode VisitCompilationUnit([NotNull] CompilationUnitContext ctx)
            => ctx.translationUnit() is null ? new SourceNode(Enumerable.Empty<ASTNode>()) : this.Visit(ctx.translationUnit());

        public override ASTNode VisitTranslationUnit([NotNull] TranslationUnitContext ctx)
        {
            ASTNode decl = this.Visit(ctx.externalDeclaration());

            if (ctx.translationUnit() is null)
                return new SourceNode(decl);

            SourceNode tu = this.Visit(ctx.translationUnit()).As<SourceNode>();
            decl.Parent = tu;
            return new SourceNode(tu.Children.Concat(new[] { decl }));
        }
    }
}
