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

        [Test]
        public void TupleUnpackingSimple()
        {
            var source = "a, b = 1, 2\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children;

            Assert.That(nodes.Single(), Is.TypeOf<DeclStatNode>());
            var declStat = nodes.Single().As<DeclStatNode>();
            var varDecls = declStat.DeclaratorList.Declarators.ToList();

            Assert.That(varDecls.Count(), Is.EqualTo(2));
            Assert.That(varDecls[0].As<VarDeclNode>().Identifier, Is.EqualTo("a"));
            Assert.That(varDecls[1].As<VarDeclNode>().Identifier, Is.EqualTo("b"));
        }

        [Test]
        public void TupleUnpackingMismatchedCounts()
        {
            var source = "x, y, z = 1, 2\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children;

            Assert.That(nodes.Single(), Is.TypeOf<ExprStatNode>());
            Assert.That(nodes.Single().As<ExprStatNode>().Expression, Is.TypeOf<AssignExprNode>());
        }

        [Test]
        public void TupleUnpackingWithPreviouslyDeclaredIdentifierKeepsAssignment()
        {
            var source = "a: int = 0\na, b = 1, 2\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children.ToList();

            Assert.That(nodes.Count, Is.EqualTo(2));
            Assert.That(nodes[0], Is.TypeOf<DeclStatNode>());
            Assert.That(nodes[1], Is.TypeOf<ExprStatNode>());
            Assert.That(nodes[1].As<ExprStatNode>().Expression, Is.TypeOf<AssignExprNode>());
        }

        [Test]
        public void ReassignmentAfterAnnotatedDeclarationIsNotRedeclared()
        {
            var source = "count: int = 1\ncount = 2\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children.ToList();

            Assert.That(nodes.Count, Is.EqualTo(2));
            Assert.That(nodes[0], Is.TypeOf<DeclStatNode>());
            Assert.That(nodes[1], Is.TypeOf<ExprStatNode>());
            Assert.That(nodes[1].As<ExprStatNode>().Expression, Is.TypeOf<AssignExprNode>());
        }
    }
}
