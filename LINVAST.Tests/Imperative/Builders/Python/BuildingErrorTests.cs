using LINVAST.Exceptions;
using LINVAST.Imperative.Builders.Python;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class BuildingErrorTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void InvalidPythonSyntaxThrowsSyntaxErrorException()
        {
            Assert.That(() => this.builder.BuildFromSource("def broken(:\n    pass\n"),
                Throws.InstanceOf<SyntaxErrorException>());
        }
    }
}
