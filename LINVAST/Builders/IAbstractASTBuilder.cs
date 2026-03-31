using LINVAST.Nodes;

namespace LINVAST.Builders
{
    public interface IAbstractASTBuilder
    {
        ASTNode BuildFromSource(string code);
    }
}
