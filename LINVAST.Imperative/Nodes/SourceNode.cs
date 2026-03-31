using System.Collections.Generic;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Nodes
{
    public sealed class SourceNode : ASTNode
    {
        public string? Name { get; set; }


        public SourceNode(IEnumerable<ASTNode> children)
            : base(1, children) { }

        public SourceNode(params ASTNode[] children)
            : base(1, children) { }

        public SourceNode(string name, IEnumerable<ASTNode> children)
            : base(1, children)
        {
            this.Name = name;
        }

        public SourceNode(string name, params ASTNode[] children)
            : base(1, children)
        {
            this.Name = name;
        }
    }
}
