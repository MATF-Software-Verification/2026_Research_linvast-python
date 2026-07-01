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

        [Test]
        public void MultipleStarredTargetsThrowSyntaxErrorException()
        {
            Assert.That(() => this.builder.BuildFromSource("a, *b, *c = 1, 2, 3\n"),
                Throws.InstanceOf<SyntaxErrorException>());
        }

        [Test]
        public void ForLoopWithSubscriptTargetThrowsSyntaxErrorException()
        {
            Assert.That(() => this.builder.BuildFromSource("for arr[0] in xs:\n    pass\n"),
                Throws.InstanceOf<SyntaxErrorException>());
        }
    }
}
