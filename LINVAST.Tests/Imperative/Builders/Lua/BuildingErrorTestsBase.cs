using LINVAST.Exceptions;
using LINVAST.Imperative.Builders.Lua;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Lua
{
    internal sealed class BuildingErrorTests : BuildingErrorTestsBase
    {
        [Test]
        public void EmptySourceTest()
        {
            this.AssertThrows<SyntaxErrorException>("");
        }

        // TODO


        protected override ASTNode GenerateAST(string src)
            => new LuaASTBuilder().BuildFromSource(src);
    }
}
