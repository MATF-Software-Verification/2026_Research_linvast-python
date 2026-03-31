using System.Linq;
using LINVAST.Imperative.Builders.Go;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Go
{
    internal sealed class FunctionTests : FunctionTestsBase
    {
        [Test]
        public void NoParametersDefinitionTest()
        {
            this.AssertFunctionSignature(
                "\nfunc f() int { }",
                2,
                "f",
                "int",
                isVariadic: false,
                AccessModifiers.Unspecified
            );
        }

        [Test]
        public void SingleParameterDefinitionTest()
        {
            this.AssertFunctionSignature("\n\n\nfunc f(x int) { }", 4, "f", @params: ("int", "x"));
        }

        [Test]
        public void MultipleParametersDefinitionTest()
        {
            this.AssertFunctionSignature(
                @"func f(x int, y float64, z float32, t Point) { }", 1, "f",
                @params: new[] { ("int", "x"), ("float64", "y"), ("float32", "z"), ("Point", "t") }
            );
        }

        [Test]
        public void SimpleDefinitionTest()
        {
            FuncNode f = this.AssertFunctionSignature(@"
                func f(x int) uint { 
                    return x;
                }",
                2, "f", "uint", @params: ("int", "x")
            );
            Assert.That(f.Definition, Is.Not.Null);
            Assert.That(f.Definition!.Children.Single(), Is.InstanceOf<JumpStatNode>());
        }

        [Test]
        public void ComplexDefinitionTest()
        {
            FuncNode f = this.AssertFunctionSignature(@"
                func f(x ...uint32) float32 {
                    var z int = 4
                    return 3.0;
                }",
                2, "f", "float32", isVariadic: true, @params: ("uint32", "x")
            );
            Assert.That(f.IsVariadic, Is.True);
            Assert.That(f.Definition, Is.Not.Null);
            Assert.That(f.Definition!.Children, Has.Exactly(2).Items);
            Assert.That(f.ParametersNode, Is.Not.Null);
            Assert.That(f.IsVariadic);
        }

        [Test]
        public void FunctionReturnExpressionTest()
        {
            this.AssertReturnValue("func g() int { return 3; }", 3);
            this.AssertReturnValue("func g() int { return 3.3; }", 3.3);
            this.AssertReturnValue("func g() int { return 3 + 1 - 2*3; }", -2);
            this.AssertReturnValue("func g() int { return ((1 << 2) + 4) >> 3; }", ((1 << 2) + 4) >> 3);
            this.AssertReturnValue("func g() int { return 1.1 > 1.0 && 1.0 > 1.02; }", false);
            this.AssertReturnValue("func g() int { return 1.01 > 1.0 || 1.0 > 1.02; }", true);
        }


        protected override ASTNode GenerateAST(string src)
            => new GoASTBuilder().BuildFromSource(src, p => p.functionDecl());
    }
}
