using LINVAST.Imperative.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Common
{
    internal abstract class BlockTestsBase : ASTBuilderTestBase
    {
        protected BlockStatNode AssertBlock(string src, bool empty = false)
        {
            BlockStatNode block = this.GenerateAST(src).As<BlockStatNode>();
            Assert.That(block, Is.Not.Null);
            Assert.That(block.Children, empty ? Is.Empty : Is.Not.Empty);
            this.AssertChildrenParentProperties(block);
            return block;
        }
    }
}
