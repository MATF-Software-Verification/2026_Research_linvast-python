using LINVAST.Nodes;

namespace LINVAST.Imperative.Nodes
{
    public sealed class PackageNode : ASTNode
    {
        public string Identifier { get; }


        public PackageNode(int line, string identifier)
            : base(line)
        {
            this.Identifier = identifier;
        }


        public override string ToString() => "package " + this.Identifier;
    }
}
