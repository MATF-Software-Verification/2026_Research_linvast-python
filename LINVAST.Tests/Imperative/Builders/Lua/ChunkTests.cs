using System.Linq;
using LINVAST.Imperative.Builders.Lua;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Lua
{
    internal sealed class ChunkTests : SourceComponentTestsBase
    {
        [Test]
        public void BasicTest()
        {
            SourceNode tu = this.AssertTranslationUnit(@"x = 2");
            Assert.That(tu.Children.Single(), Is.InstanceOf<DeclStatNode>());
        }

        [Test]
        public void FunctionTest()
        {
            SourceNode tu = this.AssertTranslationUnit(@"function two() return 2 end");
            Assert.That(tu.Children.Single(), Is.InstanceOf<FuncNode>());
        }

        [Test]
        public void EmptyFunctionBodyTest()
        {
            SourceNode tu = this.AssertTranslationUnit(@"function noop() end");
            FuncNode func = tu.Children.Single().As<FuncNode>();
            Assert.That(func.Definition!.Children, Is.Empty);
        }

        [Test]
        public void FunctionWithReturnListTest()
        {
            SourceNode tu = this.AssertTranslationUnit(@"function pair() return 1, 2 end");
            FuncNode func = tu.Children.Single().As<FuncNode>();

            JumpStatNode ret = func.Definition!.Children.Single().As<JumpStatNode>();
            ExprListNode values = ret.ReturnExpr!.As<ExprListNode>();
            Assert.That(values.Expressions.Select(v => LINVAST.Imperative.Visitors.ConstantExpressionEvaluator.Evaluate(v)),
                Is.EqualTo(new object[] { 1, 2 }));
        }


        protected override ASTNode GenerateAST(string src)
            => new LuaASTBuilder().BuildFromSource(src);
    }
}
