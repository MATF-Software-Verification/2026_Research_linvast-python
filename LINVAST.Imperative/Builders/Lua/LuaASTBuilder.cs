using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Logging;
using LINVAST.Nodes;
using Serilog;
using static LINVAST.Imperative.Builders.Lua.LuaParser;

namespace LINVAST.Imperative.Builders.Lua
{
    [ASTBuilder(".lua")]
    public sealed partial class LuaASTBuilder : LuaBaseVisitor<ASTNode>, IASTBuilder<LuaParser>
    {
        public LuaParser CreateParser(string code)
        {
            ICharStream stream = CharStreams.fromstring(code);
            var lexer = new LuaLexer(stream);
            lexer.AddErrorListener(new ThrowExceptionErrorListener());
            ITokenStream tokens = new CommonTokenStream(lexer);
            var parser = new LuaParser(tokens);
            parser.BuildParseTree = true;
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowExceptionErrorListener());
            return parser;
        }

        public ASTNode BuildFromSource(string code)
            => this.Visit(this.CreateParser(code).chunk());

        public ASTNode BuildFromSource(string code, Func<LuaParser, ParserRuleContext> entryProvider)
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

        public override ASTNode VisitChunk([NotNull] ChunkContext ctx)
        {
            BlockStatNode block = this.Visit(ctx.block()).As<BlockStatNode>();
            return new SourceNode(this.AddDeclarations(block.Children));
        }

        public override ASTNode VisitBlock([NotNull] BlockContext ctx)
        {
            IEnumerable<ASTNode> statements = ctx.stat().Select(c => this.Visit(c));
            if (!statements.Any() && ctx.retstat() is null)
                throw new SyntaxErrorException("Missing statements in block");
            if (ctx.retstat() is not null)
                statements = statements.Concat(new[] { this.Visit(ctx.retstat()) });

            return new BlockStatNode(ctx.Start.Line, statements);
        }


        private IReadOnlyList<ASTNode> AddDeclarations(IEnumerable<ASTNode> statements, HashSet<string>? declaredVars = null)
        {
            declaredVars ??= new HashSet<string>();

            var nodes = new List<ASTNode>();
            foreach (ASTNode stat in statements) {
                if (stat is ExprStatNode expr && expr.Expression is AssignExprNode assignmentExpr) {
                    if (assignmentExpr.LeftOperand is IdNode v) {
                        if (IsDeclared(v)) {
                            nodes.Add(new ExprStatNode(v.Line, assignmentExpr));
                        } else {
                            var declList = new DeclListNode(v.Line, CreateDeclarator(v, assignmentExpr.RightOperand));
                            var declSpecs = new DeclSpecsNode(v.Line);
                            nodes.Add(new DeclStatNode(v.Line, declSpecs, declList));
                            declaredVars.Add(v.Identifier);
                        }
                    } else if (assignmentExpr.LeftOperand is ArrAccessExprNode arr) {
                        IdNode arrayExpr = arr.Array as IdNode ?? throw new NotSupportedException("Complex array access expressions");
                        if (!IsDeclared(arrayExpr)) {
                            var declList = new DeclListNode(arr.Line, new ArrDeclNode(arr.Line, arrayExpr));
                            var declSpecs = new DeclSpecsNode(arr.Line);
                            nodes.Add(new DeclStatNode(arr.Line, declSpecs, declList));
                            declaredVars.Add(arrayExpr.Identifier);
                        }
                        nodes.Add(stat);
                    } else {
                        Log.Debug("Skipping statement: {Stat}", stat.GetText());
                    }
                } else if (stat is BlockStatNode block) {
                    if (block.Children.All(c => c is AssignExprNode ae && ae.LeftOperand is IdNode)) {
                        var declSpecs = new DeclSpecsNode(block.Line);
                        IEnumerable<AssignExprNode> declList = block.Children.Cast<AssignExprNode>();

                        // Declare non-declared vars
                        var notDeclared = declList
                            .Where(e => !IsDeclared(e.LeftOperand.As<IdNode>()))
                            .Select(e => CreateDeclarator(e.LeftOperand.As<IdNode>(), e.RightOperand, ignoreInitializer: true))
                            .ToList()
                            ;
                        if (notDeclared.Any()) {
                            nodes.Add(new DeclStatNode(block.Line, declSpecs, new DeclListNode(block.Line, notDeclared)));
                            foreach (string v in notDeclared.Select(d => d.Identifier))
                                declaredVars.Add(v);
                        }

                        // Declare temporary variables and initialize them
                        var tmpDeclList = new DeclListNode(block.Line, declList.Select(d => CreateTmpDeclarator(d)));
                        var tmpDeclSpecs = new DeclSpecsNode(block.Line);
                        var tmpDecl = new DeclStatNode(block.Line, tmpDeclSpecs, tmpDeclList);
                        nodes.Add(tmpDecl);
                        foreach (string v in tmpDeclList.Declarators.Select(d => d.Identifier))
                            declaredVars.Add(v);

                        // Add tmp assignments
                        var tmpAssignments = new ExprListNode(block.Line, declList.Select(decl => CreateTmpAssignment(decl)));
                        nodes.Add(new ExprStatNode(block.Line, tmpAssignments));


                        static IdNode CreateTmpIdentifier(IdNode id)
                            => new(id.Line, $"tmp__{id.Identifier}");

                        static DeclNode CreateTmpDeclarator(AssignExprNode expr)
                            => CreateDeclarator(CreateTmpIdentifier(expr.LeftOperand.As<IdNode>()), expr.RightOperand);

                        static ExprNode CreateTmpAssignment(AssignExprNode expr)
                        {
                            IdNode tmpId = CreateTmpIdentifier(expr.LeftOperand.As<IdNode>());
                            return new AssignExprNode(expr.Line, expr.LeftOperand, tmpId);
                        }
                    } else {
                        nodes.Add(new BlockStatNode(block.Line, this.AddDeclarations(block.Children, declaredVars)));
                    }
                } else if (stat is FuncNode fdef) {
                    var @params = new List<string>();
                    if (fdef.Parameters is not null)
                        @params.AddRange(fdef.Parameters.Select(p => p.Declarator.Identifier));
                    @params = @params.Except(declaredVars).ToList();

                    foreach (string p in @params)
                        declaredVars.Add(p);
                    if (fdef.Definition is null)
                        throw new SyntaxErrorException($"Function {fdef.Identifier} lacking defition in line {fdef.Line}");
                    var alteredDefinition = new BlockStatNode(fdef.Definition.Line, this.AddDeclarations(fdef.Definition.Children, declaredVars));
                    FuncDeclNode alteredDeclarator = fdef.ParametersNode is not null
                        ? new FuncDeclNode(fdef.Declarator.Line, fdef.Declarator.IdentifierNode, fdef.ParametersNode, fdef.Definition)
                        : new FuncDeclNode(fdef.Declarator.Line, fdef.Declarator.IdentifierNode, fdef.Definition);
                    nodes.Add(new FuncNode(fdef.Line, fdef.Specifiers, fdef.Declarator));
                    foreach (string p in @params)
                        declaredVars.Remove(p);
                } else if (stat is IfStatNode @if) {
                    IfStatNode alteredIf;
                    var alteredThen = new BlockStatNode(@if.ThenStat.Line, this.AddDeclarations(@if.ThenStat.Children, declaredVars));
                    if (@if.ElseStat is not null) {
                        var alteredElse = new BlockStatNode(@if.ElseStat.Line, this.AddDeclarations(@if.ElseStat.Children, declaredVars));
                        alteredIf = new IfStatNode(@if.Line, @if.Condition, alteredThen, alteredElse);
                    } else {
                        alteredIf = new IfStatNode(@if.Line, @if.Condition, alteredThen);
                    }
                    nodes.Add(alteredIf);
                } else {
                    nodes.Add(stat);
                }
            }

            return nodes.AsReadOnly();


            bool IsDeclared(IdNode node)
                => declaredVars.Contains(node.Identifier);
            //=> nodes.Any(n => n is DeclarationStatementNode decl && decl.DeclaratorList.Declarations.Any(d => d.IdentifierNode.Equals(node)));

            static DeclNode CreateDeclarator(IdNode identifier, ExprNode initializer, bool ignoreInitializer = false)
            {
                if (ignoreInitializer) {
                    return initializer switch
                    {
                        DictInitNode dict => new DictDeclNode(identifier.Line, identifier),
                        ExprListNode expList => new ArrDeclNode(
                            identifier.Line,
                            identifier,
                            LitExprNode.FromString(expList.Line, expList.Expressions.Count().ToString())
                        ),
                        ExprNode varInit => new VarDeclNode(identifier.Line, identifier),
                        _ => throw new SyntaxErrorException("Unexpected variable initializer"),
                    };
                } else {
                    return initializer switch
                    {
                        DictInitNode dict => new DictDeclNode(identifier.Line, identifier, dict),
                        ExprListNode expList => new ArrDeclNode(
                            identifier.Line,
                            identifier,
                            new ArrInitExprNode(expList.Line, expList.Expressions)
                        ),
                        ExprNode varInit => new VarDeclNode(identifier.Line, identifier, varInit),
                        _ => throw new SyntaxErrorException("Unexpected variable initializer"),
                    };
                }
            }
        }
    }
}
