using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace LINVAST.Imperative.Nodes
{
    public sealed class DictDeclNode : DeclNode
    {
        [JsonIgnore]
        public DictInitNode? Initializer => this.Children.ElementAtOrDefault(1)?.As<DictInitNode>();


        public DictDeclNode(int line, IdNode identifier)
            : base(line, identifier) { }

        public DictDeclNode(int line, IdNode identifier, DictInitNode initializer)
            : base(line, identifier, initializer) { }


        public override string GetText() => $"{base.GetText()} = {this.Initializer?.GetText() ?? "{}"}";
    }

    public sealed class DictEntryNode : ExprNode
    {
        [JsonIgnore]
        public IdNode Key => this.Children[0].As<IdNode>();

        [JsonIgnore]
        public ExprNode Value => this.Children[1].As<ExprNode>();


        public DictEntryNode(int line, IdNode key, ExprNode value)
            : base(line, key, value) { }


        public override string GetText() => $"'{this.Key}' : {this.Value}";
    }

    public sealed class DictInitNode : ExprListNode
    {
        [JsonIgnore]
        public IEnumerable<DictEntryNode> Entries => this.Children.Cast<DictEntryNode>();


        public DictInitNode(int line, IEnumerable<DictEntryNode> entries)
            : base(line, entries) { }

        public DictInitNode(int line, params DictEntryNode[] entries)
            : base(line, entries) { }


        public override string GetText()
            => new StringBuilder("{ ").AppendJoin(", ", this.Entries.Select(e => e.GetText())).Append(" }").ToString();
    }
}
