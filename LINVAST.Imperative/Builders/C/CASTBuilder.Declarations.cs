using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using Serilog;
using static LINVAST.Imperative.Builders.C.CParser;

namespace LINVAST.Imperative.Builders.C
{
    public sealed partial class CASTBuilder : CBaseVisitor<ASTNode>, IASTBuilder<CParser>
    {
        public override ASTNode VisitDeclaration([NotNull] DeclarationContext ctx)
        {
            if (ctx.staticAssertDeclaration() is not null)
                return this.Visit(ctx.staticAssertDeclaration());

            if (ctx.initDeclaratorList() is null) {
                if (TryGetSingleStructSpecifier(ctx.declarationSpecifiers(), out StructOrUnionSpecifierContext? structCtx))
                    return this.CreateStructNode(structCtx!);

                DeclSpecsNode emptyDeclSpecs = this.Visit(ctx.declarationSpecifiers()).As<DeclSpecsNode>();
                return CreateDeclarationStatement(ctx.Start.Line, emptyDeclSpecs, new DeclListNode(ctx.Start.Line));
            }

            DeclSpecsNode declSpecs = this.Visit(ctx.declarationSpecifiers()).As<DeclSpecsNode>();
            DeclListNode declList = this.Visit(ctx.initDeclaratorList()).As<DeclListNode>();
            return CreateDeclarationStatement(ctx.Start.Line, declSpecs, declList);
        }

        public override ASTNode VisitDeclarator([NotNull] DeclaratorContext ctx)
        {
            DeclNode decl = this.Visit(ctx.directDeclarator()).As<DeclNode>();
            if (ctx.pointer() is not null)
                decl.PointerLevel++;
            return decl;
        }

        public override ASTNode VisitDirectDeclarator([NotNull] DirectDeclaratorContext ctx)
        {
            if (ctx.declarator() is not null)
                return this.Visit(ctx.declarator());

            if (ctx.Identifier() is not null) {
                if (ctx.ChildCount == 1)
                    return new VarDeclNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.Identifier().ToString() ?? "<unknown_name>"));

                return new VarDeclNode(ctx.Start.Line, new IdNode(ctx.Start.Line, ctx.Identifier().ToString() ?? "<unknown_name>"));
            }

            DeclNode decl = this.Visit(ctx.directDeclarator()).As<DeclNode>();
            if (decl is VarDeclNode var) {
                if (AreBracketsTokensPresent(ctx)) {
                    if (ctx.assignmentExpression() is not null) {
                        ExprNode sizeExpr = this.Visit(ctx.assignmentExpression()).As<ExprNode>();
                        return new ArrDeclNode(ctx.Start.Line, var.IdentifierNode, sizeExpr);
                    } else {
                        return new ArrDeclNode(ctx.Start.Line, var.IdentifierNode);
                    }
                } else if (AreParenTokensPresent(ctx)) {
                    FuncDeclNode func;
                    if (ctx.parameterTypeList() is not null) {
                        FuncParamsNode @params = this.Visit(ctx.parameterTypeList()).As<FuncParamsNode>();
                        func = new FuncDeclNode(ctx.Start.Line, var.IdentifierNode, @params);
                    } else {
                        func = new FuncDeclNode(ctx.Start.Line, var.IdentifierNode);
                    }

                    func.PointerLevel = var.PointerLevel;
                    return func;
                } else {
                    return var;
                }
            } else if (decl is ArrDeclNode arr) {
                if (AreBracketsTokensPresent(ctx)) {
                    ExprNode? sizeExpr = ctx.assignmentExpression() is not null
                        ? this.Visit(ctx.assignmentExpression()).As<ExprNode>()
                        : null;
                    ExprNode? combinedSize = CombineArraySizes(ctx.Start.Line, arr.SizeExpression, sizeExpr);
                    return combinedSize is null
                        ? new ArrDeclNode(ctx.Start.Line, arr.IdentifierNode)
                        : new ArrDeclNode(ctx.Start.Line, arr.IdentifierNode, combinedSize);
                } else if (AreParenTokensPresent(ctx)) {
                    Log.Warning("Potential syntax error in line: {Line}. Parsing will continue but results are not guaranteed...", ctx.Start.Line);
                } else {
                    return arr;
                }
            } else {
                Log.Warning("Potential syntax error in line: {Line}. Parsing will continue but results are not guaranteed...", ctx.Start.Line);
            }

            return decl;


            static bool AreParenTokensPresent(DirectDeclaratorContext ctx)
                => ctx.GetToken(LeftParen, 0) is not null && ctx.GetToken(RightParen, 0) is not null;

            static bool AreBracketsTokensPresent(DirectDeclaratorContext ctx)
                => ctx.GetToken(LeftBracket, 0) is not null && ctx.GetToken(RightBracket, 0) is not null;

            static ExprNode? CombineArraySizes(int line, ExprNode? left, ExprNode? right)
            {
                if (left is null)
                    return right;
                if (right is null)
                    return left;

                IEnumerable<ExprNode> leftExpressions = left is ExprListNode list
                    ? list.Expressions
                    : new[] { left };
                return new ExprListNode(line, leftExpressions.Append(right));
            }
        }

        public override ASTNode VisitDeclarationSpecifiers([NotNull] DeclarationSpecifiersContext ctx)
        {
            string[] specs = ctx.declarationSpecifier().Select(DeclarationSpecifierText).ToArray();
            return CreateDeclSpecs(ctx.Start.Line, specs);
        }

        public override ASTNode VisitTypeSpecifier([NotNull] TypeSpecifierContext ctx)
            => new TypeNameNode(ctx.Start.Line, TypeSpecifierText(ctx));

        public override ASTNode VisitStructOrUnionSpecifier([NotNull] StructOrUnionSpecifierContext ctx)
            => ctx.structDeclarationList() is not null
                ? this.CreateStructNode(ctx)
                : new TypeNameNode(ctx.Start.Line, StructTypeName(ctx));

        public override ASTNode VisitStructDeclarationList([NotNull] StructDeclarationListContext ctx)
        {
            ASTNode decl = this.Visit(ctx.structDeclaration());

            if (ctx.structDeclarationList() is null)
                return new BlockStatNode(ctx.Start.Line, decl);

            BlockStatNode list = this.Visit(ctx.structDeclarationList()).As<BlockStatNode>();
            return new BlockStatNode(ctx.Start.Line, list.Children.Concat(new[] { decl }));
        }

        public override ASTNode VisitStructDeclaration([NotNull] StructDeclarationContext ctx)
        {
            if (ctx.staticAssertDeclaration() is not null)
                return this.Visit(ctx.staticAssertDeclaration());

            if (TryRecoverOverconsumedStructDeclarator(ctx, out DeclStatNode? recoveredDecl))
                return recoveredDecl!;

            DeclSpecsNode declSpecs = this.Visit(ctx.specifierQualifierList()).As<DeclSpecsNode>();
            DeclListNode declList = ctx.structDeclaratorList() is not null
                ? this.Visit(ctx.structDeclaratorList()).As<DeclListNode>()
                : new DeclListNode(ctx.Start.Line, Enumerable.Empty<DeclNode>());

            return CreateDeclarationStatement(ctx.Start.Line, declSpecs, declList);
        }

        public override ASTNode VisitSpecifierQualifierList([NotNull] SpecifierQualifierListContext ctx)
        {
            string[] specs = SpecifierQualifierTexts(ctx).ToArray();
            return CreateDeclSpecs(ctx.Start.Line, specs);
        }

        public override ASTNode VisitStructDeclaratorList([NotNull] StructDeclaratorListContext ctx)
        {
            IEnumerable<DeclNode> declarators = StructDeclarators(ctx)
                .Select(decl => this.Visit(decl) as DeclNode)
                .Where(decl => decl is not null)
                .Cast<DeclNode>();
            return new DeclListNode(ctx.Start.Line, declarators);
        }

        public override ASTNode VisitStructDeclarator([NotNull] StructDeclaratorContext ctx)
        {
            if (ctx.declarator() is null)
                return new EmptyStatNode(ctx.Start.Line);

            return this.Visit(ctx.declarator());
        }

        public override ASTNode VisitInitDeclaratorList([NotNull] InitDeclaratorListContext ctx)
        {
            DeclNode decl = this.Visit(ctx.initDeclarator()).As<DeclNode>();

            if (ctx.initDeclaratorList() is null)
                return new DeclListNode(ctx.Start.Line, decl);

            DeclListNode list = this.Visit(ctx.initDeclaratorList()).As<DeclListNode>();
            return new DeclListNode(ctx.Start.Line, list.Declarators.Concat(new[] { decl }));
        }

        public override ASTNode VisitInitDeclarator([NotNull] InitDeclaratorContext ctx)
        {
            DeclNode declarator = this.Visit(ctx.declarator()).As<DeclNode>();
            ASTNode? init = null;
            if (ctx.initializer() is not null)
                init = this.Visit(ctx.initializer());

            if (declarator is VarDeclNode var)
                return init is null ? var : new VarDeclNode(ctx.Start.Line, var.IdentifierNode, init.As<ExprNode>());

            if (declarator is ArrDeclNode arr) {
                if (arr.SizeExpression is null) {
                    if (init is null)
                        return new ArrDeclNode(ctx.Start.Line, arr.IdentifierNode);
                    else
                        return new ArrDeclNode(ctx.Start.Line, arr.IdentifierNode, init.As<ArrInitExprNode>());
                } else {
                    if (init is null)
                        return new ArrDeclNode(ctx.Start.Line, arr.IdentifierNode, arr.SizeExpression);
                    else
                        return new ArrDeclNode(ctx.Start.Line, arr.IdentifierNode, arr.SizeExpression, init.As<ArrInitExprNode>());
                }
            }

            return declarator;
        }

        public override ASTNode VisitInitializerList([NotNull] InitializerListContext ctx)
        {
            ExprNode init = this.Visit(ctx.initializer()).As<ExprNode>();

            if (ctx.initializerList() is null)
                return new ArrInitExprNode(ctx.Start.Line, init);

            ArrInitExprNode list = this.Visit(ctx.initializerList()).As<ArrInitExprNode>();
            return new ArrInitExprNode(ctx.Start.Line, list.Initializers.Concat(new[] { init }));
        }

        public override ASTNode VisitInitializer([NotNull] InitializerContext ctx)
            => ctx.assignmentExpression() is not null ? this.Visit(ctx.assignmentExpression()) : this.Visit(ctx.initializerList());

        public override ASTNode VisitStaticAssertDeclaration([NotNull] StaticAssertDeclarationContext ctx)
            => new EmptyStatNode(ctx.Start.Line);

        private StructNode CreateStructNode([NotNull] StructOrUnionSpecifierContext ctx)
        {
            string identifier = ctx.Identifier()?.GetText() ?? "<anonymous>";
            IEnumerable<DeclStatNode> declarations = ctx.structDeclarationList() is not null
                ? this.Visit(ctx.structDeclarationList()).As<BlockStatNode>().Children.OfType<DeclStatNode>()
                : Enumerable.Empty<DeclStatNode>();

            var decl = new TypeDeclNode(
                ctx.Start.Line,
                new IdNode(ctx.Start.Line, identifier),
                new TypeNameListNode(ctx.Start.Line),
                new TypeNameListNode(ctx.Start.Line),
                declarations);
            var declSpecs = new DeclSpecsNode(ctx.Start.Line, identifier);
            return new StructNode(ctx.Start.Line, declSpecs, decl);
        }

        private static DeclStatNode CreateDeclarationStatement(int line, DeclSpecsNode declSpecs, DeclListNode declList)
        {
            if (declSpecs.TypeName.EndsWith("*", StringComparison.Ordinal)) {
                string pointerFreeType = declSpecs.TypeName.Substring(0, declSpecs.TypeName.IndexOf("*", StringComparison.Ordinal));
                declSpecs = new DeclSpecsNode(declSpecs.Line, declSpecs.Modifiers.ToString(), pointerFreeType);
                foreach (DeclNode decl in declList.Declarators)
                    decl.PointerLevel++;
            }

            return new DeclStatNode(line, declSpecs, declList);
        }

        private static DeclSpecsNode CreateDeclSpecs(int line, string[] specs)
        {
            int structIndex = Array.FindLastIndex(specs, spec => spec.StartsWith("struct ", StringComparison.Ordinal) || spec.StartsWith("union ", StringComparison.Ordinal));
            int unsignedIndex = Array.IndexOf(specs, "unsigned");
            string type = structIndex != -1
                ? specs[structIndex]
                : unsignedIndex != -1
                    ? string.Join(' ', specs[unsignedIndex..])
                    : specs.Last();
            return new DeclSpecsNode(line, string.Join(' ', specs), type);
        }

        private static bool TryGetSingleStructSpecifier([NotNull] DeclarationSpecifiersContext ctx, out StructOrUnionSpecifierContext? structCtx)
        {
            structCtx = null;
            if (ctx.declarationSpecifier().Length != 1)
                return false;

            structCtx = ctx.declarationSpecifier().Single().typeSpecifier()?.structOrUnionSpecifier();
            return structCtx is not null;
        }

        private static string DeclarationSpecifierText([NotNull] DeclarationSpecifierContext ctx)
        {
            if (ctx.typeSpecifier() is { } typeSpecifier)
                return TypeSpecifierText(typeSpecifier);

            return ctx.GetText();
        }

        private static string TypeSpecifierText([NotNull] TypeSpecifierContext ctx)
        {
            if (ctx.structOrUnionSpecifier() is { } structCtx)
                return StructTypeName(structCtx);

            if (ctx.typeSpecifier() is { } innerType && ctx.pointer() is not null)
                return $"{TypeSpecifierText(innerType)}{ctx.pointer().GetText()}";

            return ctx.GetText();
        }

        private static string StructTypeName([NotNull] StructOrUnionSpecifierContext ctx)
        {
            string category = ctx.structOrUnion().GetText();
            string identifier = ctx.Identifier()?.GetText() ?? "<anonymous>";
            return $"{category} {identifier}";
        }

        private static IEnumerable<string> SpecifierQualifierTexts([NotNull] SpecifierQualifierListContext ctx)
        {
            string current = ctx.typeSpecifier() is { } typeSpecifier
                ? TypeSpecifierText(typeSpecifier)
                : ctx.typeQualifier().GetText();

            yield return current;

            if (ctx.specifierQualifierList() is not null) {
                foreach (string text in SpecifierQualifierTexts(ctx.specifierQualifierList()))
                    yield return text;
            }
        }

        private bool TryRecoverOverconsumedStructDeclarator([NotNull] StructDeclarationContext ctx, out DeclStatNode? decl)
        {
            decl = null;
            IToken[] specifierTokens = DefaultTokens(ctx.specifierQualifierList()).ToArray();
            if (specifierTokens.Length < 2 || specifierTokens[^1].Type != Identifier)
                return false;

            bool isMissingDeclarator = ctx.structDeclaratorList() is null;
            bool isNamedBitFieldParsedAsUnnamed = ctx.structDeclaratorList() is not null && DefaultTokens(ctx.structDeclaratorList()).FirstOrDefault()?.Type == Colon;
            if (!isMissingDeclarator && !isNamedBitFieldParsedAsUnnamed)
                return false;

            IToken identifier = specifierTokens[^1];
            DeclSpecsNode declSpecs = CreateDeclSpecsFromTokens(ctx.Start.Line, specifierTokens[..^1]);
            var declarators = new List<DeclNode>
            {
                new VarDeclNode(identifier.Line, new IdNode(identifier.Line, identifier.Text))
            };
            if (isNamedBitFieldParsedAsUnnamed) {
                bool skippedRecoveredBitField = false;
                foreach (StructDeclaratorContext structDeclarator in StructDeclarators(ctx.structDeclaratorList()!)) {
                    if (!skippedRecoveredBitField && structDeclarator.declarator() is null) {
                        skippedRecoveredBitField = true;
                        continue;
                    }

                    declarators.Add(this.Visit(structDeclarator).As<DeclNode>());
                }
            }

            var declList = new DeclListNode(
                ctx.Start.Line,
                declarators);
            decl = CreateDeclarationStatement(ctx.Start.Line, declSpecs, declList);
            return true;
        }

        private static IEnumerable<StructDeclaratorContext> StructDeclarators([NotNull] StructDeclaratorListContext ctx)
        {
            if (ctx.structDeclaratorList() is not null) {
                foreach (StructDeclaratorContext declarator in StructDeclarators(ctx.structDeclaratorList()))
                    yield return declarator;
            }

            yield return ctx.structDeclarator();
        }

        private static DeclSpecsNode CreateDeclSpecsFromTokens(int line, IReadOnlyList<IToken> tokens)
            => CreateDeclSpecs(line, SpecifierTextsFromTokens(tokens).ToArray());

        private static IEnumerable<string> SpecifierTextsFromTokens(IReadOnlyList<IToken> tokens)
        {
            for (int i = 0; i < tokens.Count; i++) {
                IToken token = tokens[i];
                if (token.Type == Struct || token.Type == Union) {
                    string category = token.Text;
                    string identifier = "<anonymous>";
                    int next = i + 1;
                    if (next < tokens.Count && tokens[next].Type == Identifier) {
                        identifier = tokens[next].Text;
                        next++;
                    }

                    if (next < tokens.Count && tokens[next].Type == LeftBrace)
                        next = FindMatchingBrace(tokens, next) + 1;

                    string text = $"{category} {identifier}";
                    while (next < tokens.Count && IsPointerToken(tokens[next])) {
                        text += tokens[next].Text;
                        next++;
                    }

                    yield return text;
                    i = next - 1;
                    continue;
                }

                string specifierText = token.Text;
                while (i + 1 < tokens.Count && IsPointerToken(tokens[i + 1])) {
                    specifierText += tokens[i + 1].Text;
                    i++;
                }

                yield return specifierText;
            }
        }

        private static int FindMatchingBrace(IReadOnlyList<IToken> tokens, int leftBraceIndex)
        {
            int depth = 0;
            for (int i = leftBraceIndex; i < tokens.Count; i++) {
                if (tokens[i].Type == LeftBrace)
                    depth++;
                else if (tokens[i].Type == RightBrace) {
                    depth--;
                    if (depth == 0)
                        return i;
                }
            }

            return leftBraceIndex;
        }

        private static bool IsPointerToken(IToken token)
            => token.Type == Star || token.Type == Caret;

        private static IEnumerable<IToken> DefaultTokens(IParseTree tree)
        {
            if (tree is ITerminalNode terminal) {
                if (terminal.Symbol.Channel == TokenConstants.DefaultChannel)
                    yield return terminal.Symbol;
                yield break;
            }

            for (int i = 0; i < tree.ChildCount; i++) {
                foreach (IToken token in DefaultTokens(tree.GetChild(i)))
                    yield return token;
            }
        }
    }
}
