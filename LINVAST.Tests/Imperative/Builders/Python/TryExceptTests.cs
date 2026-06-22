using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class TryExceptTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void TryExceptBuildsTryStatNode()
        {
            var tryStatement = this.ParseStatement(
                "try:\n    pass\nexcept ValueError:\n    pass\n").As<TryStatNode>();

            Assert.That(tryStatement.TryBody.As<BlockStatNode>().Children.Single(), Is.TypeOf<EmptyStatNode>());
            Assert.That(tryStatement.CatchClauses.Count(), Is.EqualTo(1));

            var catchClause = tryStatement.CatchClauses.Single();
            Assert.That(catchClause.ExceptionType, Is.TypeOf<IdNode>());
            Assert.That(catchClause.ExceptionType!.As<IdNode>().Identifier, Is.EqualTo("ValueError"));
            Assert.That(catchClause.Binding, Is.Null);
            Assert.That(tryStatement.ElseStat, Is.Null);
            Assert.That(tryStatement.FinallyStat, Is.Null);
        }

        [Test]
        public void TryExceptAsBuildsCatchClauseWithBinding()
        {
            var tryStatement = this.ParseStatement(
                "try:\n    pass\nexcept ValueError as err:\n    pass\n").As<TryStatNode>();

            var catchClause = tryStatement.CatchClauses.Single();
            Assert.That(catchClause.Binding, Is.TypeOf<IdNode>());
            Assert.That(catchClause.Binding!.Identifier, Is.EqualTo("err"));
        }

        [Test]
        public void TryExceptElseFinallyBuildsTryStatNode()
        {
            var tryStatement = this.ParseStatement(
                "try:\n    pass\nexcept:\n    pass\nelse:\n    pass\nfinally:\n    pass\n").As<TryStatNode>();

            Assert.That(tryStatement.CatchClauses.Single().ExceptionType, Is.Null);
            Assert.That(tryStatement.ElseStat, Is.TypeOf<BlockStatNode>());
            Assert.That(tryStatement.FinallyStat, Is.TypeOf<BlockStatNode>());
        }

        [Test]
        public void TryFinallyBuildsTryStatNode()
        {
            var tryStatement = this.ParseStatement(
                "try:\n    pass\nfinally:\n    pass\n").As<TryStatNode>();

            Assert.That(tryStatement.CatchClauses, Is.Empty);
            Assert.That(tryStatement.ElseStat, Is.Null);
            Assert.That(tryStatement.FinallyStat, Is.TypeOf<BlockStatNode>());
        }

        private StatNode ParseStatement(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<StatNode>();
    }
}
