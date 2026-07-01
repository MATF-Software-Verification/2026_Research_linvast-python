using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    public sealed partial class PythonASTBuilder : Python3ParserBaseVisitor<ASTNode>, IASTBuilder<Python3Parser>
    {
        // funcdef: 'def' name parameters ('->' test)? ':' block
        public override ASTNode VisitFuncdef(Python3Parser.FuncdefContext ctx)
            => this.CreateFunctionNode(ctx);

        // async_funcdef: ASYNC funcdef
        public override ASTNode VisitAsync_funcdef(Python3Parser.Async_funcdefContext ctx)
        {
            var func = this.CreateFunctionNode(ctx.funcdef(), new[] { new TagNode(ctx.Start.Line, "async") });
            return func;
        }

        // parameters: '(' typedargslist? ')'
        public override ASTNode VisitParameters(Python3Parser.ParametersContext ctx)
            => ctx.typedargslist() is null
                ? new FuncParamsNode(ctx.Start.Line)
                : this.Visit(ctx.typedargslist());

        // typedargslist: tfpdef ('=' test)? ... | '*' tfpdef? ... | '**' tfpdef
        public override ASTNode VisitTypedargslist(Python3Parser.TypedargslistContext ctx)
            => this.BuildParams(ctx);

        // tfpdef: name (':' test)?
        public override ASTNode VisitTfpdef(Python3Parser.TfpdefContext ctx)
            => this.CreateParam(ctx);

        // varargslist: vfpdef ('=' test)? ... | '*' vfpdef? ... | '**' vfpdef
        public override ASTNode VisitVarargslist(Python3Parser.VarargslistContext ctx)
            => this.BuildParams(ctx);

        // vfpdef: name
        public override ASTNode VisitVfpdef(Python3Parser.VfpdefContext ctx)
            => this.CreateParam(ctx);

        // lambdef: 'lambda' varargslist? ':' test
        public override ASTNode VisitLambdef(Python3Parser.LambdefContext ctx)
        {
            var body = new BlockStatNode(ctx.Start.Line,
                new JumpStatNode(ctx.test().Start.Line, this.Visit(ctx.test()).As<ExprNode>()));
            return ctx.varargslist() is null
                ? new LambdaFuncExprNode(ctx.Start.Line, body)
                : new LambdaFuncExprNode(ctx.Start.Line, this.Visit(ctx.varargslist()).As<FuncParamsNode>(), body);
        }

        // lambdef_nocond: 'lambda' varargslist? ':' test_nocond
        public override ASTNode VisitLambdef_nocond(Python3Parser.Lambdef_nocondContext ctx)
        {
            var body = new BlockStatNode(ctx.Start.Line,
                new JumpStatNode(ctx.test_nocond().Start.Line, this.Visit(ctx.test_nocond()).As<ExprNode>()));
            return ctx.varargslist() is null
                ? new LambdaFuncExprNode(ctx.Start.Line, body)
                : new LambdaFuncExprNode(ctx.Start.Line, this.Visit(ctx.varargslist()).As<FuncParamsNode>(), body);
        }

        // decorator: '@' dotted_name ('(' arglist? ')')? NEWLINE
        public override ASTNode VisitDecorator(Python3Parser.DecoratorContext ctx)
        {
            string name = ctx.dotted_name().GetText();
            if (ctx.OPEN_PAREN() is not null)
                name += ctx.arglist() is null ? "()" : $"({ctx.arglist().GetText()})";
            return new TagNode(ctx.Start.Line, name);
        }

        // decorators: decorator+
        public override ASTNode VisitDecorators(Python3Parser.DecoratorsContext ctx)
            => new TagListNode(ctx.Start.Line, ctx.decorator().Select(d => this.Visit(d).As<TagNode>()));

        // decorated: decorators (classdef | funcdef | async_funcdef)
        public override ASTNode VisitDecorated(Python3Parser.DecoratedContext ctx)
        {
            var tags = this.Visit(ctx.decorators()).As<TagListNode>().Tags.ToArray();
            if (ctx.funcdef() is not null)
                return this.CreateFunctionNode(ctx.funcdef(), tags);
            if (ctx.async_funcdef() is not null) {
                var asyncTags = tags.Concat(new[] { new TagNode(ctx.async_funcdef().Start.Line, "async") });
                return this.CreateFunctionNode(ctx.async_funcdef().funcdef(), asyncTags);
            }

            ASTNode decorated = this.Visit(ctx.classdef());
            if (decorated is ClassNode classNode) {
                var typeDecl = classNode.DeclaratorList.Declarators.Single().As<TypeDeclNode>();
                var classTags = tags.Concat(classNode.Tags).ToArray();
                int line = Math.Min(classNode.Line, classTags.Select(t => t.Line).DefaultIfEmpty(classNode.Line).Min());
                return new ClassNode(line, classTags, classNode.Specifiers, typeDecl);
            }

            return decorated;
        }

        private FuncNode CreateFunctionNode(Python3Parser.FuncdefContext ctx, IEnumerable<TagNode>? tags = null)
        {
            var name = new IdNode(ctx.name().Start.Line, ctx.name().GetText());
            FuncParamsNode @params = this.Visit(ctx.parameters()).As<FuncParamsNode>();
            BlockStatNode body = this.AsFunctionBody(this.Visit(ctx.block()));
            var declSpecs = new DeclSpecsNode(ctx.Start.Line, ctx.test() is null ? "void" : ctx.test().GetText());
            var decl = new FuncDeclNode(ctx.Start.Line, name, @params, body);
            var tagArray = tags?.ToArray() ?? Array.Empty<TagNode>();
            int line = Math.Min(ctx.Start.Line, tagArray.Select(t => t.Line).DefaultIfEmpty(ctx.Start.Line).Min());
            return tagArray.Any()
                ? new FuncNode(line, tagArray, declSpecs, decl)
                : new FuncNode(line, declSpecs, decl);
        }

        private BlockStatNode AsFunctionBody(ASTNode body)
        {
            var block = body is BlockStatNode blockBody
                ? blockBody
                : new BlockStatNode(body.Line, body);
            return new BlockStatNode(block.Line, this.AddDeclarations(block.Children));
        }

        private FuncParamsNode BuildParams(ParserRuleContext ctx)
        {
            var parameters = new List<FuncParamNode>();
            bool variadic = false;
            string? prefix = null;

            // A '/' separator (PEP 570) marks every preceding parameter as
            // positional-only. Locate it up front so those parameters can be
            // tagged as we build them.
            int slashIndex = -1;
            for (int i = 0; i < ctx.ChildCount; i++) {
                if (ctx.GetChild(i) is ITerminalNode slash && slash.GetText() == "/") {
                    slashIndex = i;
                    break;
                }
            }

            for (int i = 0; i < ctx.ChildCount; i++) {
                IParseTree child = ctx.GetChild(i);
                if (child is ITerminalNode terminal) {
                    string text = terminal.GetText();
                    if (text == "*" || text == "**")
                        prefix = text;
                    else if (text == "," && prefix == "*")
                        prefix = null;
                    continue;
                }

                bool positionalOnly = slashIndex >= 0 && i < slashIndex;
                FuncParamNode? param = child switch
                {
                    Python3Parser.TfpdefContext typed => this.CreateParam(typed, this.FindDefaultValue(ctx, i), prefix, positionalOnly),
                    Python3Parser.VfpdefContext untyped => this.CreateParam(untyped, this.FindDefaultValue(ctx, i), prefix, positionalOnly),
                    _ => null,
                };

                if (param is null)
                    continue;

                if (prefix is "*" or "**")
                    variadic = true;
                parameters.Add(param);
                prefix = null;
            }

            var result = new FuncParamsNode(ctx.Start.Line, parameters);
            result.IsVariadic = variadic;
            return result;
        }

        private ExprNode? FindDefaultValue(ParserRuleContext ctx, int paramIndex)
        {
            if (paramIndex + 2 >= ctx.ChildCount || ctx.GetChild(paramIndex + 1).GetText() != "=")
                return null;
            return ctx.GetChild(paramIndex + 2) is ParserRuleContext value
                ? this.Visit(value).As<ExprNode>()
                : null;
        }

        private FuncParamNode CreateParam(Python3Parser.TfpdefContext ctx, ExprNode? initializer = null, string? prefix = null, bool positionalOnly = false)
        {
            string typeName = ctx.test() is null ? "object" : ctx.test().GetText();
            return this.CreateParam(ctx.Start.Line, ctx.name().GetText(), typeName, initializer, prefix, positionalOnly);
        }

        private FuncParamNode CreateParam(Python3Parser.VfpdefContext ctx, ExprNode? initializer = null, string? prefix = null, bool positionalOnly = false)
            => this.CreateParam(ctx.Start.Line, ctx.name().GetText(), "object", initializer, prefix, positionalOnly);

        private FuncParamNode CreateParam(int line, string name, string typeName, ExprNode? initializer, string? prefix, bool positionalOnly = false)
        {
            var tags = this.ParamTags(line, prefix, positionalOnly).ToArray();
            var declSpecs = new DeclSpecsNode(line, typeName);
            var id = new IdNode(line, name);
            VarDeclNode decl = initializer is null
                ? new VarDeclNode(line, id)
                : new VarDeclNode(line, id, initializer);
            return tags.Any()
                ? new FuncParamNode(line, tags, declSpecs, decl)
                : new FuncParamNode(line, declSpecs, decl);
        }

        private IEnumerable<TagNode> ParamTags(int line, string? prefix, bool positionalOnly)
        {
            if (positionalOnly)
                yield return new TagNode(line, "posonly");
            if (prefix == "*")
                yield return new TagNode(line, "args");
            else if (prefix == "**")
                yield return new TagNode(line, "kwargs");
        }
    }

    internal sealed class TagListNode : ASTNode
    {
        public IEnumerable<TagNode> Tags => this.Children.Cast<TagNode>();

        public TagListNode(int line, IEnumerable<TagNode> tags)
            : base(line, tags) { }

        public override string GetText() => string.Join(" ", this.Tags.Select(t => t.GetText()));
    }
}
