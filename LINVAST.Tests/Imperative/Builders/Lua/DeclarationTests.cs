using System.Linq;
using LINVAST.Imperative.Builders.Lua;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Lua
{
    internal sealed class DeclarationTests : DeclarationTestsBase
    {
        [Test]
        public void SimpleDeclarationTests()
        {
            this.AssertVariableDeclaration("x = 2", "x", "object", value: 2);
            this.AssertVariableDeclaration("local y", "y", "object", AccessModifiers.Private);
            this.AssertVariableDeclaration("local y = 2", "y", "object", AccessModifiers.Private, value: 2);
        }

        [Test]
        public void DeclarationListTests()
        {
            this.AssertVariableDeclarationList("x, y = 3, 4", "object", vars: new (string, object?)[] { ("x", 3), ("y", 4) });
            this.AssertVariableDeclarationList("x, y = 3", "object", vars: new (string, object?)[] { ("x", 3), ("y", null) });
            this.AssertVariableDeclarationList("x, y = 3, 4, 5", "object", vars: new (string, object?)[] { ("x", 3), ("y", 4) });
            this.AssertVariableDeclarationList("x, y = \"a\", 'b'", "object", vars: new (string, object?)[] { ("x", "a"), ("y", "b") });
            this.AssertVariableDeclarationList("x, y = true, 3.4", "object", vars: new (string, object?)[] { ("x", true), ("y", 3.4) });
            this.AssertVariableDeclarationList("x, y = false, nil", "object", vars: new (string, object?)[] { ("x", false), ("y", null) });
            this.AssertVariableDeclarationList("x, y = 3.2e2, 2.33e-2", "object", vars: new (string, object?)[] { ("x", 3.2e2), ("y", 2.33e-2) });
            this.AssertVariableDeclarationList("local x, y = 3", "object", AccessModifiers.Private, vars: new (string, object?)[] { ("x", 3), ("y", null) });
        }

        [Test]
        public void ArrayDeclarationTest()
        {
            this.AssertArrayDeclaration("local values = {1, 2}", "object", "values", access: AccessModifiers.Private, init: new object[] { 1, 2 });
        }

        [Test]
        public void DictionaryDeclarationTest()
        {
            SourceNode src = this.GenerateAST("local t = {name = 1, ['x'] = 2}").As<SourceNode>();
            DeclStatNode decl = src.Children.Single().As<DeclStatNode>();
            DictDeclNode dict = decl.DeclaratorList.Declarators.Single().As<DictDeclNode>();

            Assert.That(decl.Specifiers.Modifiers.AccessModifiers, Is.EqualTo(AccessModifiers.Private));
            Assert.That(dict.Identifier, Is.EqualTo("t"));
            Assert.That(dict.Initializer!.Entries.Select(e => e.Key.Identifier), Is.EqualTo(new[] { "name", "x" }));
        }

        [Test]
        public void LocalFunctionDeclarationTest()
        {
            SourceNode src = this.GenerateAST("local function f(a, ...) return a end").As<SourceNode>();
            FuncNode func = src.Children.Single().As<FuncNode>();

            Assert.That(func.Modifiers.AccessModifiers, Is.EqualTo(AccessModifiers.Private));
            Assert.That(func.Identifier, Is.EqualTo("f"));
            Assert.That(func.Parameters, Has.Exactly(1).Items);
            Assert.That(func.IsVariadic, Is.True);
        }

        [Test]
        public void ComplexArrayAssignmentDoesNotThrowTest()
        {
            SourceNode src = this.GenerateAST("f()[1] = 2").As<SourceNode>();

            Assert.That(src.Children.Single(), Is.InstanceOf<ExprStatNode>());
        }


        protected override ASTNode GenerateAST(string src)
            => new LuaASTBuilder().BuildFromSource(src, p => p.chunk());
    }
}
