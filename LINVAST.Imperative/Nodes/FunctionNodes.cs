using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using LINVAST.Nodes;
using Newtonsoft.Json;

namespace LINVAST.Imperative.Nodes
{
    public sealed class FuncDeclNode : DeclNode
    {
        [JsonIgnore]
        public bool IsVariadic => this.ParametersNode?.IsVariadic ?? false;

        [JsonIgnore]
        public TypeNameListNode TemplateArgs => this.Children[1].As<TypeNameListNode>();

        [JsonIgnore]
        public FuncParamsNode? ParametersNode => this.Children.ElementAtOrDefault(2) as FuncParamsNode ?? null;

        [JsonIgnore]
        public BlockStatNode? Definition => this.Children.Last() as BlockStatNode ?? null;

        [JsonIgnore]
        public IEnumerable<FuncParamNode>? Parameters => this.ParametersNode?.Parameters;


        public FuncDeclNode(int line, IdNode identifier)
            : base(line, identifier) { }

        public FuncDeclNode(int line, IdNode identifier, FuncParamsNode @params)
            : base(line, identifier, new TypeNameListNode(line), @params) { }

        public FuncDeclNode(int line, IdNode identifier, BlockStatNode body)
            : base(line, identifier, new TypeNameListNode(line), body) { }

        public FuncDeclNode(int line, IdNode identifier, FuncParamsNode @params, BlockStatNode body)
            : base(line, identifier, new TypeNameListNode(line), @params, body) { }

        public FuncDeclNode(int line, IdNode identifier, TypeNameListNode templateArgs, FuncParamsNode @params)
            : base(line, identifier, templateArgs, @params) { }

        public FuncDeclNode(int line, IdNode identifier, TypeNameListNode templateArgs, BlockStatNode body)
            : base(line, identifier, templateArgs, body) { }

        public FuncDeclNode(int line, IdNode identifier, TypeNameListNode templateArgs, FuncParamsNode @params, BlockStatNode body)
            : base(line, identifier, templateArgs, @params, body) { }

        public FuncDeclNode(int line, IEnumerable<TagNode> tags, IdNode identifier)
            : base(line, tags, identifier) { }

        public FuncDeclNode(int line, IEnumerable<TagNode> tags, IdNode identifier, FuncParamsNode @params)
            : base(line, tags, identifier, new TypeNameListNode(line), @params) { }

        public FuncDeclNode(int line, IEnumerable<TagNode> tags, IdNode identifier, BlockStatNode body)
            : base(line, tags, identifier, new TypeNameListNode(line), body) { }

        public FuncDeclNode(int line, IEnumerable<TagNode> tags, IdNode identifier, FuncParamsNode @params, BlockStatNode body)
            : base(line, tags, identifier, new TypeNameListNode(line), @params, body) { }

        public FuncDeclNode(int line, IEnumerable<TagNode> tags, IdNode identifier, TypeNameListNode templateArgs, FuncParamsNode @params)
            : base(line, tags, identifier, templateArgs, @params) { }

        public FuncDeclNode(int line, IEnumerable<TagNode> tags, IdNode identifier, TypeNameListNode templateArgs, BlockStatNode body)
            : base(line, tags, identifier, templateArgs, body) { }

        public FuncDeclNode(int line, IEnumerable<TagNode> tags, IdNode identifier, TypeNameListNode templateArgs, FuncParamsNode @params, BlockStatNode body)
            : base(line, tags, identifier, templateArgs, @params, body) { }


        public override string GetText()
        {
            var sb = new StringBuilder();
            sb.Append(base.GetText()).Append('(');
            if (this.TemplateArgs is not null)
                sb.Append('<').AppendJoin(',', this.TemplateArgs.Types).Append('>');
            if (this.ParametersNode is not null)
                sb.Append(this.ParametersNode.GetText());
            sb.Append(')');
            if (this.Definition is not null)
                sb.Append(this.Definition.GetText());
            else
                sb.Append(';');
            return sb.ToString();
        }
    }

    public sealed class LambdaFuncExprNode : ExprNode
    {
        [JsonIgnore]
        public BlockStatNode Definition => this.Children.Last().As<BlockStatNode>();

        [JsonIgnore]
        public FuncParamsNode? ParametersNode => this.Children.ElementAtOrDefault(0) as FuncParamsNode ?? null;

        [JsonIgnore]
        public IEnumerable<FuncParamNode>? Parameters => this.ParametersNode?.Parameters;


        public LambdaFuncExprNode(int line, BlockStatNode def)
            : base(line, def)
        {

        }

        public LambdaFuncExprNode(int line, FuncParamsNode @params, BlockStatNode def)
            : base(line, @params, def)
        {

        }


        public override string GetText()
            => $"lambda ({this.ParametersNode?.GetText() ?? ""}): {this.Definition.GetText()}";
    }

    public sealed class FuncNode : DeclStatNode
    {
        [JsonIgnore]
        public FuncDeclNode Declarator => this.ChildrenWithoutTags.ElementAt(1).As<DeclListNode>().Declarators.Single().As<FuncDeclNode>();

        [JsonIgnore]
        public BlockStatNode? Definition => this.Declarator.Definition;

        [JsonIgnore]
        public string ReturnTypeName => this.Specifiers.TypeName;

        [JsonIgnore]
        public Type? ReturnType => this.Specifiers.Type;

        [JsonIgnore]
        public string Identifier => this.Declarator.Identifier;

        [JsonIgnore]
        public bool IsVariadic => this.Declarator.IsVariadic;

        [JsonIgnore]
        public FuncParamsNode? ParametersNode => this.Declarator.ParametersNode;

        [JsonIgnore]
        public IEnumerable<FuncParamNode>? Parameters => this.ParametersNode?.Parameters;


        public FuncNode(int line, DeclSpecsNode declSpecs, FuncDeclNode decl)
            : base(line, declSpecs, new DeclListNode(line, decl)) { }

        public FuncNode(int line, IEnumerable<TagNode> tags, DeclSpecsNode declSpecs, FuncDeclNode decl)
            : base(line, tags, declSpecs, new DeclListNode(line, decl)) { }


        public override string GetText()
            => $"{string.Join(' ', this.Tags)} {this.Modifiers} {this.Declarator.GetText()}";
    }

    public sealed class FuncParamsNode : DeclarationNode
    {
        public bool IsVariadic { get; set; }

        [JsonIgnore]
        public IEnumerable<FuncParamNode> Parameters => this.Children.Cast<FuncParamNode>();


        public FuncParamsNode(int line, IEnumerable<FuncParamNode> @params)
            : base(line, @params) { }

        public FuncParamsNode(int line, params FuncParamNode[] @params)
            : base(line, @params) { }


        public override string GetText() => string.Join(", ", this.Children.Select(c => c.GetText()));

        public override bool Equals([AllowNull] ASTNode other)
            => base.Equals(other) && this.IsVariadic.Equals((other as FuncParamsNode)?.IsVariadic);
    }

    public class FuncParamNode : DeclarationNode
    {
        [JsonIgnore]
        public DeclSpecsNode Specifiers => this.Children[0].As<DeclSpecsNode>();

        [JsonIgnore]
        public DeclNode Declarator => this.Children[1].As<DeclNode>();


        public FuncParamNode(int line, DeclSpecsNode declSpecs, DeclNode declarator)
            : base(line, declSpecs, declarator) { }

        public FuncParamNode(int line, IEnumerable<TagNode> tags, DeclSpecsNode declSpecs, DeclNode declarator)
            : base(line, tags, declSpecs, declarator) { }
    }
}
