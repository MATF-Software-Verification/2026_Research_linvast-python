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
        public void DelStatementBuildsDeleteNode()
        {
            var stat = this.ParseSingle<DeleteStatNode>("del x\n");

            Assert.That(stat.Targets.Single(), Is.TypeOf<IdNode>());
            Assert.That(stat.Targets.Single().As<IdNode>().Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void DelStatementWithMultipleTargetsBuildsDeleteNode()
        {
            var stat = this.ParseSingle<DeleteStatNode>("del a, b\n");

            Assert.That(stat.Targets.Select(t => t.As<IdNode>().Identifier), Is.EqualTo(new[] { "a", "b" }));
        }

        private TNode ParseSingle<TNode>(string source)
            where TNode : ASTNode
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<TNode>();
    }
}
