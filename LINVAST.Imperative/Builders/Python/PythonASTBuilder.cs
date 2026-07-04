using System;
using System.Collections.Generic;
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
        private int comprehensionAccumulatorIndex;
        private readonly List<PendingComprehension> pendingComprehensions = new();

        public ASTNode BuildFromSource(string code)
        {
            this.pendingComprehensions.Clear();
            try {
                return this.Visit(this.CreateParser(code).file_input());
            } finally {
                this.pendingComprehensions.Clear();
            }
        }

        public Python3Parser CreateParser(string code) => this.CreateParser(code, initialLine: 1);

        private Python3Parser CreateParser(string code, int initialLine)
        {
            ICharStream stream = CharStreams.fromstring(code);
            var lexer = new Python3Lexer(stream);
            lexer.Line = initialLine;
            lexer.AddErrorListener(new ThrowExceptionErrorListener());
            ITokenStream tokens = new CommonTokenStream(lexer);
            var parser = new Python3Parser(tokens);
            parser.BuildParseTree = true;
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowExceptionErrorListener());
            return parser;
        }

        public ASTNode BuildFromSource(string code, Func<Python3Parser, ParserRuleContext> entryProvider)
        {
            this.pendingComprehensions.Clear();
            try {
                return this.Visit(entryProvider(this.CreateParser(code)));
            } finally {
                this.pendingComprehensions.Clear();
            }
        }

        private ASTNode BuildFromSource(string code, Func<Python3Parser, ParserRuleContext> entryProvider, int initialLine) =>
            this.Visit(entryProvider(this.CreateParser(code, initialLine)));

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
            IEnumerable<ASTNode> statements = ctx.stmt()
                .Select(this.Visit)
                .SelectMany(node => node is BlockStatNode block
                    ? block.Children.AsEnumerable()
                    : Enumerable.Repeat(node, 1));

            return new SourceNode(this.AddDeclarations(statements));
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

        private static DeclStatNode MakeVarDecl(int line, IdNode identifier, ExprNode? initializer, string typeName)
        {
            var declSpecs = new DeclSpecsNode(line, typeName);
            DeclNode decl = initializer is not null
                ? new VarDeclNode(line, identifier, initializer)
                : new VarDeclNode(line, identifier);
            return new DeclStatNode(line, declSpecs, new DeclListNode(line, decl));
        }

        private int MarkPendingComprehensions() => this.pendingComprehensions.Count;

        private IReadOnlyList<PendingComprehension> TakePendingComprehensions(int mark)
        {
            var items = this.pendingComprehensions.Skip(mark).ToArray();
            this.pendingComprehensions.RemoveRange(mark, this.pendingComprehensions.Count - mark);
            return items;
        }

        private static IEnumerable<ASTNode> HoistedStatements(IEnumerable<PendingComprehension> comprehensions) =>
            comprehensions.SelectMany(c => c.Expansion.Children);

        private static BlockStatNode HoistComprehensionsBefore(
            int line,
            IEnumerable<PendingComprehension> comprehensions,
            StatNode statement) =>
            new(line, HoistedStatements(comprehensions).Concat(new ASTNode[] { statement }));

        private sealed class PendingComprehension
        {
            public string AccumulatorName { get; }
            public BlockStatNode Expansion { get; }

            public PendingComprehension(string accumulatorName, BlockStatNode expansion)
            {
                this.AccumulatorName = accumulatorName;
                this.Expansion = expansion;
            }
        }

        private IReadOnlyList<ASTNode> AddDeclarations(IEnumerable<ASTNode> statements)
        {
            var nodes = new List<ASTNode>();
            var declared = new HashSet<string>();
            IEnumerable<ASTNode> flattenedStatements = statements
                .SelectMany(stat => stat is BlockStatNode block
                    ? block.Children.AsEnumerable()
                    : Enumerable.Repeat(stat, 1));
            foreach (ASTNode stat in flattenedStatements) {
                if (stat is DeclStatNode declStat) {
                    foreach (DeclNode declarator in declStat.DeclaratorList.Declarators)
                        declared.Add(declarator.Identifier);
                    nodes.Add(stat);
                    continue;
                }

                if (stat is ExprStatNode expr && expr.Expression is AssignExprNode assign) {
                    if (this.HasMultipleStarredTargets(assign.LeftOperand))
                        throw new SyntaxErrorException("multiple starred expressions in assignment");

                    // Try single identifier assignment first
                    if (assign.LeftOperand is IdNode id) {
                        if (!declared.Contains(id.Identifier)) {
                            var declSpecs = new DeclSpecsNode(id.Line);
                            var declList = new DeclListNode(id.Line, new VarDeclNode(id.Line, id, assign.RightOperand));
                            nodes.Add(new DeclStatNode(id.Line, declSpecs, declList));
                            declared.Add(id.Identifier);
                        } else {
                            nodes.Add(stat);
                        }
                    }
                    // Try tuple unpacking assignment
                    else if (this.TryPromoteTupleUnpacking(assign.LeftOperand, assign.RightOperand, assign.Line, nodes, declared)) {
                        // Successfully promoted to declarations
                    } else {
                        // Could not promote, keep as expression statement
                        nodes.Add(stat);
                    }
                } else {
                    nodes.Add(stat);
                }
            }
            return nodes.AsReadOnly();
        }
    }
}
