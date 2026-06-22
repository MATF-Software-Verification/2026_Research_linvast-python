using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class DeclarationTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void AnnotatedAssignmentBuildsTypedDeclaration()
        {
            var declaration = this.ParseSingle<DeclStatNode>("count: int = 1\n");
            var variable = declaration.DeclaratorList.Declarators.Single().As<VarDeclNode>();

            Assert.That(declaration.Specifiers.TypeName, Is.EqualTo("int"));
            Assert.That(variable.Identifier, Is.EqualTo("count"));
            Assert.That(variable.Initializer, Is.TypeOf<LitExprNode>());
        }

        [Test]
        public void AnnotatedAssignmentPreservesGenericTypeText()
        {
            var declaration = this.ParseSingle<DeclStatNode>("items: List[int] = values\n");

            Assert.That(declaration.Specifiers.TypeName, Is.EqualTo("List[int]"));
        }

        [Test]
        public void AnnotatedAssignmentOnNonIdentifierTargetWithInitializerKeepsAssignment()
        {
            var stat = this.ParseSingle<ExprStatNode>("arr[0]: int = 42\n");

            Assert.That(stat.Expression, Is.TypeOf<AssignExprNode>());
            Assert.That(stat.Expression.As<AssignExprNode>().RightOperand.As<LitExprNode>().Value, Is.EqualTo("42"));
        }

        [Test]
        public void DelSingleTarget()
        {
            var stat = this.ParseSingle<DeleteStatNode>("del x\n");

            Assert.That(stat.Targets.Single(), Is.TypeOf<IdNode>());
            Assert.That(stat.Targets.Single().As<IdNode>().Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void DelMultipleTargets()
        {
            var stat = this.ParseSingle<DeleteStatNode>("del a, b\n");

            Assert.That(stat.Targets.Select(t => t.As<IdNode>().Identifier), Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void GlobalSingleIdentifier()
        {
            var stat = this.ParseSingle<GlobalStatNode>("global x\n");

            Assert.That(stat.Identifiers.Select(i => i.Identifier), Is.EqualTo(new[] { "x" }));
        }

        [Test]
        public void GlobalMultipleIdentifiers()
        {
            var stat = this.ParseSingle<GlobalStatNode>("global x, y\n");

            Assert.That(stat.Identifiers.Select(i => i.Identifier), Is.EqualTo(new[] { "x", "y" }));
        }

        [Test]
        public void NonlocalSingleIdentifier()
        {
            var stat = this.ParseSingle<NonlocalStatNode>("nonlocal a\n");

            Assert.That(stat.Identifiers.Select(i => i.Identifier), Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void NonlocalMultipleIdentifiers()
        {
            var stat = this.ParseSingle<NonlocalStatNode>("nonlocal a, b\n");

            Assert.That(stat.Identifiers.Select(i => i.Identifier), Is.EqualTo(new[] { "a", "b" }));
        }

        private TNode ParseSingle<TNode>(string source)
            where TNode : ASTNode
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<TNode>();
    }
}
