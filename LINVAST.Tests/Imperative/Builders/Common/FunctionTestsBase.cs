using System.Linq;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Imperative.Visitors;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Common
{
    internal abstract class FunctionTestsBase : ASTBuilderTestBase
    {
        protected FuncNode AssertFunctionSignature(string src,
                                                                 int line,
                                                                 string fname,
                                                                 string returnType = "void",
                                                                 bool isVariadic = false,
                                                                 AccessModifiers access = AccessModifiers.Unspecified,
                                                                 QualifierFlags qualifiers = QualifierFlags.None,
                                                                 params (string Type, string Identifier)[] @params)
        {
            FuncNode f = this.GenerateAST(src).As<FuncNode>();
            this.AssertChildrenParentProperties(f);
            Assert.That(f.Definition, Is.Not.Null);
            this.AssertChildrenParentProperties(f.Definition!);
            Assert.That(f, Is.Not.Null);
            Assert.That(f.Line, Is.EqualTo(line));
            Assert.That(f.Declarator.Parent!.Parent, Is.EqualTo(f));
            Assert.That(f.Modifiers.AccessModifiers, Is.EqualTo(access));
            Assert.That(f.Modifiers.QualifierFlags, Is.EqualTo(qualifiers));
            Assert.That(f.Identifier, Is.EqualTo(fname));
            Assert.That(f.ReturnTypeName, Is.EqualTo(returnType));
            Assert.That(f.IsVariadic, Is.EqualTo(isVariadic));
            if (@params?.Any() ?? false) {
                Assert.That(f.Parameters, Is.Not.Null);
                Assert.That(f.Parameters, Has.Exactly(@params.Length).Items);
                Assert.That(f.ParametersNode, Is.Not.Null);
                Assert.That(f.Parameters, Is.Not.Null);
                Assert.That(f.Parameters!.Select(p => (p.Specifiers.TypeName, p.Declarator.Identifier)), Is.EqualTo(@params));
            }
            return f;
        }

        protected void AssertReturnValue(string code, object? expected)
        {
            FuncNode fnode = this.GenerateAST(code).As<FuncNode>();
            Assert.That(fnode.Definition, Is.Not.Null);
            JumpStatNode node = fnode.Definition!.Children.Last().As<JumpStatNode>();

            Assert.That(node.GotoLabel, Is.Null);
            if (expected is null) {
                Assert.That(node.ReturnExpr, Is.Null);
            } else {
                Assert.That(node.ReturnExpr, Is.Not.Null);
                if (node.ReturnExpr is not null)
                    Assert.That(ConstantExpressionEvaluator.Evaluate(node.ReturnExpr), Is.EqualTo(expected).Within(1e-10));
            }
        }
    }
}
