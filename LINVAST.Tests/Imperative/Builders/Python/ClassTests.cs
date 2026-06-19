using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class ClassTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void ClassDefinitionBuildsClassNode()
        {
            var classNode = this.ParseSingleClass("class Service:\n    pass\n");
            var declaration = classNode.DeclaratorList.Declarators.Single().As<TypeDeclNode>();

            Assert.That(declaration.Identifier, Is.EqualTo("Service"));
            Assert.That(declaration.BaseTypes.Types, Is.Empty);
        }

        private ClassNode ParseSingleClass(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<ClassNode>();
    }
}
