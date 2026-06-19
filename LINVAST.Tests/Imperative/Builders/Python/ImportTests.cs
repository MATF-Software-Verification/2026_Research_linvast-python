using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class ImportTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void ImportNameBuildsImportList()
        {
            var importList = this.ParseSingleImport("import os\n");

            var importNode = importList.Imports.Single();
            Assert.That(importNode.Directive, Is.EqualTo("os"));
            Assert.That(importNode.QualifiedAs, Is.Null);
        }

        private ImportListNode ParseSingleImport(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<ImportListNode>();
    }
}
