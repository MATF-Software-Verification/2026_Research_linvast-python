using System.Linq;
using LINVAST.Imperative.Builders.C;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.C
{
    internal sealed class SourceComponentTests : SourceComponentTestsBase
    {
        [Test]
        public void BasicTest()
        {
            SourceNode sc = this.AssertTranslationUnit(@"void f() { }");
            Assert.That(sc.Children.Single(), Is.InstanceOf<FuncNode>());
        }

        [Test]
        public void MultipleFunctionsTest()
        {
            SourceNode sc = this.AssertTranslationUnit(@"
                int f(int x) { 
                    int y = 3;
                    return x + y;
                }

                static float st_x() {
                    return 3.5f;
                }
            ");
            Assert.That(sc.Children, Is.All.InstanceOf<FuncNode>());
        }

        [Test]
        public void MixedDeclarationTest()
        {
            SourceNode sc = this.AssertTranslationUnit(@"
                int f(int x) { 
                    int y = 3;
                    return x + y;
                }

                extern static unsigned int x, y = 5;

                static float st_x() {
                    return 3.5f;
                }
            ");
            Assert.That(sc.Children.ElementAt(0), Is.InstanceOf<FuncNode>());
            Assert.That(sc.Children.ElementAt(1), Is.InstanceOf<DeclStatNode>());
            Assert.That(sc.Children.ElementAt(2), Is.InstanceOf<FuncNode>());
        }

        [Test]
        public void EmptySourceTest()
        {
            this.AssertTranslationUnit("", empty: true);
        }


        protected override ASTNode GenerateAST(string src)
            => new CASTBuilder().BuildFromSource(src);
    }
}
