using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class MatchTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void MatchStatementBuildsMatchStatNode()
        {
            var matchStatement = this.ParseStatement(
                "match x:\n    case 1:\n        pass\n").As<MatchStatNode>();

            Assert.That(matchStatement.Subject, Is.TypeOf<IdNode>());
            Assert.That(matchStatement.Cases.Count(), Is.EqualTo(1));

            CaseNode caseNode = matchStatement.Cases.Single();
            Assert.That(caseNode.Pattern, Is.TypeOf<LiteralPatternNode>());
            Assert.That(caseNode.Guard, Is.Null);
            Assert.That(caseNode.Body.As<BlockStatNode>().Children.Single(), Is.TypeOf<EmptyStatNode>());
        }

        [Test]
        public void MatchCaseWithGuardBuildsCaseNodeWithGuard()
        {
            var matchStatement = this.ParseStatement(
                "match x:\n    case 1 if x > 0:\n        pass\n").As<MatchStatNode>();

            CaseNode caseNode = matchStatement.Cases.Single();
            Assert.That(caseNode.Guard, Is.TypeOf<RelExprNode>());
        }

        [Test]
        public void MatchMultipleCasesBuildsCaseNodes()
        {
            var matchStatement = this.ParseStatement(
                "match x:\n    case 1:\n        pass\n    case _:\n        pass\n").As<MatchStatNode>();

            Assert.That(matchStatement.Cases.Count(), Is.EqualTo(2));
            Assert.That(matchStatement.Cases.First().Pattern, Is.TypeOf<LiteralPatternNode>());
            Assert.That(matchStatement.Cases.Last().Pattern, Is.TypeOf<WildcardPatternNode>());
        }

        [Test]
        public void MatchAsPatternBuildsAsPatternNode()
        {
            var matchStatement = this.ParseStatement(
                "match x:\n    case [1, 2] as pair:\n        pass\n").As<MatchStatNode>();

            Assert.That(matchStatement.Cases.Single().Pattern, Is.TypeOf<AsPatternNode>());
            Assert.That(matchStatement.Cases.Single().Pattern.As<AsPatternNode>().Target.Identifier, Is.EqualTo("pair"));
        }

        [Test]
        public void MatchOpenSequencePatternCaseBuildsSequencePatternNode()
        {
            var matchStatement = this.ParseStatement(
                "match x:\n    case 1, 2:\n        pass\n").As<MatchStatNode>();

            Assert.That(matchStatement.Cases.Single().Pattern, Is.TypeOf<SequencePatternNode>());
            Assert.That(
                matchStatement.Cases.Single().Pattern.As<SequencePatternNode>().Kind,
                Is.EqualTo(SequencePatternKind.OpenParen));
        }

        private StatNode ParseStatement(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<StatNode>();
    }
}
