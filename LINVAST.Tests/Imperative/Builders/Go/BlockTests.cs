using System.Linq;
using LINVAST.Imperative.Builders.Go;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Go
{
    internal sealed class BlockTests : BlockTestsBase
    {
        [Test]
        public void EmptyBlockTest()
        {
            this.AssertBlock("{ }", empty: true);
            this.AssertBlock(@"{ 
                    // still empty
            }", empty: true);
        }

        [Test]
        public void SimpleBlockTest()
        {
            BlockStatNode block = this.AssertBlock(@" 
                {           // line 2
                            // line 3
                    var x int;  // line 4, block begins
                }
            ");
            Assert.That(block.Line, Is.EqualTo(4));
            Assert.That(block.Children.Single(), Is.InstanceOf<DeclStatNode>());
        }

        [Test]
        public void ComplexBlockTest()
        {
            BlockStatNode block = this.AssertBlock(@" 
                {           // line 2
                    var x int;  // line 3, block begins
                    if x == 0 {
                        x = 4;


                    } else {
                        var x float32;
                        var y = 3;


                    }

                    var w, h float32;
                }
            ");
            Assert.That(block.Line, Is.EqualTo(3));
            Assert.That(block.Children, Has.Exactly(3).Items);
            Assert.That(block.Children.ElementAt(0), Is.InstanceOf<DeclStatNode>());
            Assert.That(block.Children.ElementAt(1), Is.InstanceOf<IfStatNode>());
            Assert.That(block.Children.ElementAt(2), Is.InstanceOf<DeclStatNode>());
        }


        protected override ASTNode GenerateAST(string src)
            => new GoASTBuilder().BuildFromSource(src, p => p.block());
    }
}
