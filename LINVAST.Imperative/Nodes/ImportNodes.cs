using System.Collections.Generic;
using System.Linq;
using System.Text;
using LINVAST.Nodes;
using Newtonsoft.Json;

namespace LINVAST.Imperative.Nodes
{
    public sealed class ImportListNode : ASTNode
    {
        [JsonIgnore]
        public IEnumerable<ImportNode> Imports => this.Children.Cast<ImportNode>();

        public ImportListNode(int line, IEnumerable<ImportNode> imports)
            : base(line, imports) { }

        public ImportListNode(int line, params ImportNode[] imports)
            : base(line, imports) { }


        public override string ToString() => string.Join('\n', this.Imports);
    }

    public sealed class ImportNode : ASTNode
    {
        public string Directive { get; }
        public string? QualifiedAs { get; }


        public ImportNode(int line, string directive, string? qualifiedAs = null)
            : base(line)
        {
            this.Directive = directive;
            this.QualifiedAs = qualifiedAs;
        }


        public override string ToString()
        {
            var sb = new StringBuilder("import ");
            sb.Append(this.Directive);
            if (this.QualifiedAs is not null)
                sb.Append(" as ").Append(this.QualifiedAs);
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
