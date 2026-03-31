using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using Newtonsoft.Json;
using Serilog;

namespace LINVAST.Imperative.Nodes
{
    public abstract class DeclarationNode : ASTNode
    {
        [JsonIgnore]
        public IEnumerable<ASTNode> ChildrenWithoutTags => this.Children.SkipWhile(e => e is TagNode);

        [JsonIgnore]
        public IEnumerable<TagNode> Tags => this.Children.TakeWhile(e => e is TagNode).Cast<TagNode>();


        protected DeclarationNode(int line, IEnumerable<ASTNode> children)
            : base(line, children) { }

        protected DeclarationNode(int line, params ASTNode[] children)
            : base(line, children) { }

        protected DeclarationNode(int line, IEnumerable<TagNode> tags, IEnumerable<ASTNode> children)
            : base(line, tags.Concat(children)) { }

        protected DeclarationNode(int line, IEnumerable<TagNode> tags, params ASTNode[] children)
            : base(line, tags.Concat(children)) { }
    }

    public sealed class DeclSpecsNode : DeclarationNode
    {
        public Modifiers Modifiers { get; }

        [JsonIgnore]
        public TypeNameNode TypeNode => this.Children.Single().As<TypeNameNode>();

        [JsonIgnore]
        public string TypeName => this.TypeNode.TypeName;

        [JsonIgnore]
        public Type? Type => this.TypeNode.Type;


        public DeclSpecsNode(int line)
            : this(line, "object") { }

        public DeclSpecsNode(int line, string type)
            : this(line, "", type) { }

        public DeclSpecsNode(int line, string specs, string type)
            : base(line, new TypeNameNode(line, type))
        {
            this.Modifiers = Modifiers.Parse(specs);
        }

        public DeclSpecsNode(int line, TypeNameNode type)
            : this(line, "", type) { }

        public DeclSpecsNode(int line, string specs, TypeNameNode type)
            : base(line, type)
        {
            this.Modifiers = Modifiers.Parse(specs);
        }


        public override string GetText()
        {
            var sb = new StringBuilder();
            string declSpecs = this.Modifiers.ToString();
            if (!string.IsNullOrWhiteSpace(declSpecs))
                sb.Append(declSpecs).Append(' ');
            sb.Append(this.TypeName);
            return sb.ToString();
        }

        public override bool Equals(object? obj)
            => this.Equals(obj as DeclSpecsNode);

        public override bool Equals([AllowNull] ASTNode other)
        {
            if (!base.Equals(other))
                return false;

            var decl = other as DeclSpecsNode;
            if (!this.Modifiers.Equals(decl?.Modifiers))
                return false;
            return this.Type is not null ? this.Type.Equals(decl?.Type) : this.TypeName.Equals(decl?.TypeName);
        }

        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), this.Modifiers, this.TypeName);
    }

    public abstract class DeclNode : DeclarationNode
    {
        public int PointerLevel { get; set; }

        [JsonIgnore]
        public IdNode IdentifierNode => this.Children.First().As<IdNode>();

        [JsonIgnore]
        public string Identifier => this.IdentifierNode.Identifier;


        public DeclNode(int line, IdNode identifier, params ASTNode[] children)
            : base(line, new[] { identifier }.Concat(children)) { }

        public DeclNode(int line, IdNode identifier, IEnumerable<ASTNode> children)
            : base(line, new[] { identifier }.Concat(children)) { }

        public DeclNode(int line, IEnumerable<TagNode> tags, IdNode identifier, params ASTNode[] children)
            : base(line, tags.Concat(new ASTNode[] { identifier }).Concat(children)) { }

        public DeclNode(int line, IEnumerable<TagNode> tags, IdNode identifier, IEnumerable<ASTNode> children)
            : base(line, tags.Concat(new ASTNode[] { identifier }).Concat(children)) { }


        public override string GetText() => $"{new string('*', this.PointerLevel)}{this.IdentifierNode.GetText()}";
    }

    public class DeclListNode : DeclarationNode
    {
        [JsonIgnore]
        public IEnumerable<DeclNode> Declarators => this.Children.Cast<DeclNode>();


        public DeclListNode(int line, IEnumerable<DeclNode> decls)
            : base(line, decls) { }

        public DeclListNode(int line, params DeclNode[] decls)
            : base(line, decls) { }


        public override string GetText() => string.Join(", ", this.Children.Select(c => c.GetText()));
    }

    public sealed class VarDeclNode : DeclNode
    {
        [JsonIgnore]
        public ExprNode? Initializer => this.Children.ElementAtOrDefault(1)?.As<ExprNode>();


        public VarDeclNode(int line, IdNode identifier)
            : base(line, identifier) { }

        public VarDeclNode(int line, IdNode identifier, ExprNode initializer)
            : base(line, identifier, initializer) { }

        public VarDeclNode(int line, IEnumerable<TagNode> tags, IdNode identifier)
            : base(line, tags, identifier) { }

        public VarDeclNode(int line, IEnumerable<TagNode> tags, IdNode identifier, ExprNode initializer)
            : base(line, tags, identifier, initializer) { }


        public override string GetText()
            => this.Initializer is not null ? $"{base.GetText()} = {this.Initializer.GetText()}" : base.GetText();
    }

    public sealed class TypeNameListNode : DeclListNode
    {
        [JsonIgnore]
        public IEnumerable<TypeNameNode> Types => this.Children.Cast<TypeNameNode>();


        public TypeNameListNode(int line, IEnumerable<TypeNameNode> decls)
            : base(line, decls) { }

        public TypeNameListNode(int line, params TypeNameNode[] decls)
            : base(line, decls) { }
    }

    public sealed class TypeNameNode : DeclNode
    {
        public string TypeName => this.Identifier;

        [JsonIgnore]
        public IEnumerable<TypeNameNode> TemplateArguments => this.Children.Skip(1).Cast<TypeNameNode>();

        [JsonIgnore]
        public Type? Type { get; }


        public TypeNameNode(int line, string typeName, params TypeNameNode[] templateArgs)
            : this(line, typeName, templateArgs.AsEnumerable()) { }

        public TypeNameNode(int line, string typeName, IEnumerable<TypeNameNode> templateArgs)
            : base(line, new IdNode(line, typeName.Trim()), templateArgs)
        {
            TypeCode? typeCode = Types.TypeCodeFor(this.TypeName);
            if (typeCode is null)
                Log.Warning("Unknown type: {Type}", this.TypeName);
            else
                this.Type = Types.ToType(typeCode.Value);
        }


        public override string GetText()
        {
            if (!this.TemplateArguments.Any())
                return this.TypeName;

            var sb = new StringBuilder(this.TypeName);
            sb.Append('<').AppendJoin(", ", this.TemplateArguments).Append('>');
            return sb.ToString();
        }
    }
}
