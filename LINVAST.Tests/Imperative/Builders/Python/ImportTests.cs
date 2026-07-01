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

        [Test]
        public void ImportAliasPreservesQualifiedName()
        {
            var importNode = this.ParseSingleImport("import numpy as np\n").Imports.Single();

            Assert.That(importNode.Directive, Is.EqualTo("numpy"));
            Assert.That(importNode.QualifiedAs, Is.EqualTo("np"));
        }

        [Test]
        public void RelativeImportPreservesLeadingDot()
        {
            var importNode = this.ParseSingleImport("from . import sibling\n").Imports.Single();

            Assert.That(importNode.Directive, Is.EqualTo(".sibling"));
        }

        private ImportListNode ParseSingleImport(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<ImportListNode>();
    }
}
