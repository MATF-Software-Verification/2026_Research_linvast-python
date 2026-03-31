using System;
using Antlr4.Runtime;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Common
{
    internal abstract class BuildingErrorTestsBase : ASTBuilderTestBase
    {
        protected void AssertThrows<TException>(string src) where TException : Exception
            => Assert.That(() => this.GenerateAST(src), Throws.InstanceOf<TException>());
    }
}
