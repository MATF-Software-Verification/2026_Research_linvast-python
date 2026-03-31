using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using LINVAST.Nodes;
using Newtonsoft.Json;

namespace LINVAST.Imperative.Nodes
{
    public sealed class TagNode : ASTNode
    {
        [JsonIgnore]
        public IEnumerable<TagFieldNode> Fields => this.Children.Skip(1).Cast<TagFieldNode>();

        [JsonIgnore]
        public IdNode IdentifierNode => this.Children.First().As<IdNode>();

        [JsonIgnore]
        public string Identifier => this.IdentifierNode.Identifier;


        public TagNode(int line, string name)
            : base(line, new IdNode(line, name)) { }

        public TagNode(int line, IdNode name)
            : base(line, name) { }

        public TagNode(int line, string name, IEnumerable<TagFieldNode> fields)
            : base(line, new ASTNode[] { new IdNode(line, name) }.Concat(fields)) { }

        public TagNode(int line, IdNode name, params TagFieldNode[] fields)
            : base(line, new ASTNode[] { name }.Concat(fields)) { }


        public override string GetText()
        {
            var sb = new StringBuilder(this.Identifier);
            if (this.Fields.Any())
                sb.Append('(').AppendJoin(", ", this.Fields).Append(')');
            return sb.ToString();
        }
    }


    public sealed class TagFieldNode : ASTNode
    {
        public string Name { get; }
        public LitExprNode Value { get; }


        public TagFieldNode(int line, string name, LitExprNode value)
            : base(line)
        {
            this.Name = name;
            this.Value = value;
        }


        public override string GetText()
            => $"{this.Name}={this.Value}";

        public override bool Equals(object? obj)
            => this.Equals(obj as TagFieldNode);

        public override bool Equals([AllowNull] ASTNode other)
        {
            if (!base.Equals(other))
                return false;

            var field = other as TagFieldNode;
            return Equals(this.Name, field?.Name) && Equals(this.Value, field?.Value);
        }

        public override int GetHashCode() => this.Name.GetHashCode();
    }

    [Flags]
    public enum TagRestrictions
    {
        NotSpecified = 0,
        Type = 1,
        Field = 2,
        Property = 4,
        Method = 8,
        Constructor = 16,
        LocalVariable = 32,
        LocalFunction = 64,
        FunctionParameter = 128,
    }

    // TODO tag definition
}
