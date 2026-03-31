using LINVAST.Nodes;

namespace LINVAST
{
    public interface IASTFactory
    {
        ASTNode BuildFromFile(string path);
    }
}
