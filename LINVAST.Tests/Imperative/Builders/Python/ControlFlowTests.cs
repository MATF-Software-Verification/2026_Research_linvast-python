using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class ControlFlowTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void IfElifElseBuildsNestedIfNodes()
        {
            var ifStatement = this.ParseStatement(
                "if True:\n    pass\nelif False:\n    pass\nelse:\n    pass\n").As<IfStatNode>();

            Assert.That(ifStatement.Condition, Is.TypeOf<LitExprNode>());
            Assert.That(ifStatement.ThenStat.As<BlockStatNode>().Children.Single(), Is.TypeOf<EmptyStatNode>());
            Assert.That(ifStatement.ElseStat, Is.TypeOf<IfStatNode>());
        }

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

        [Test]
        public void WithStatementBuildsWithStatNode()
        {
            var withStatement = this.ParseStatement("with open(path):\n    pass\n").As<WithStatNode>();

            Assert.That(withStatement.ContextManager, Is.TypeOf<FuncCallExprNode>());
            Assert.That(withStatement.Target, Is.Null);
            Assert.That(withStatement.Body.As<BlockStatNode>().Children.Single(), Is.TypeOf<EmptyStatNode>());
        }

        [Test]
        public void WithAsTargetBuildsWithStatNode()
        {
            var withStatement = this.ParseStatement("with open(path) as f:\n    pass\n").As<WithStatNode>();

            Assert.That(withStatement.ContextManager, Is.TypeOf<FuncCallExprNode>());
            Assert.That(withStatement.Target, Is.TypeOf<IdNode>());
            Assert.That(withStatement.Target!.As<IdNode>().Identifier, Is.EqualTo("f"));
        }

        [Test]
        public void MultipleWithItemsBuildNestedWithStatNodes()
        {
            var withStatement = this.ParseStatement("with a() as x, b() as y:\n    pass\n").As<WithStatNode>();

            Assert.That(withStatement.ContextManager.As<FuncCallExprNode>().Identifier, Is.EqualTo("a"));
            Assert.That(withStatement.Target!.As<IdNode>().Identifier, Is.EqualTo("x"));

            var innerWith = withStatement.Body.As<WithStatNode>();
            Assert.That(innerWith.ContextManager.As<FuncCallExprNode>().Identifier, Is.EqualTo("b"));
            Assert.That(innerWith.Target!.As<IdNode>().Identifier, Is.EqualTo("y"));
            Assert.That(innerWith.Body.As<BlockStatNode>().Children.Single(), Is.TypeOf<EmptyStatNode>());
        }

        [Test]
        public void MatchStatementBuildsMatchStatNode()
        {
            var matchStatement = this.ParseStatement(
                "match x:\n    case 1:\n        pass\n").As<MatchStatNode>();

            Assert.That(matchStatement.Subject, Is.TypeOf<IdNode>());
            Assert.That(matchStatement.Cases.Count(), Is.EqualTo(1));

            CaseNode caseNode = matchStatement.Cases.Single();
            Assert.That(caseNode.Pattern, Is.TypeOf<PatternLiteralNode>());
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
            Assert.That(matchStatement.Cases.First().Pattern, Is.TypeOf<PatternLiteralNode>());
            Assert.That(matchStatement.Cases.Last().Pattern, Is.TypeOf<PatternWildcardNode>());
        }

        [Test]
        public void MatchAsPatternBuildsPatternAsNode()
        {
            var matchStatement = this.ParseStatement(
                "match x:\n    case [1, 2] as pair:\n        pass\n").As<MatchStatNode>();

            Assert.That(matchStatement.Cases.Single().Pattern, Is.TypeOf<PatternAsNode>());
            Assert.That(matchStatement.Cases.Single().Pattern.As<PatternAsNode>().Target.Identifier, Is.EqualTo("pair"));
        }

        [Test]
        public void MatchOpenSequencePatternCaseBuildsPatternSequenceNode()
        {
            var matchStatement = this.ParseStatement(
                "match x:\n    case 1, 2:\n        pass\n").As<MatchStatNode>();

            Assert.That(matchStatement.Cases.Single().Pattern, Is.TypeOf<PatternSequenceNode>());
            Assert.That(
                matchStatement.Cases.Single().Pattern.As<PatternSequenceNode>().Kind,
                Is.EqualTo(SequencePatternKind.OpenParen));
        }

        [Test]
        public void ForLoopWithIdentifierTargetBuildsForStatNode()
        {
            var forStat = this.ParseStatement("for x in xs:\n    pass\n").As<ForStatNode>();

            Assert.That(forStat.ForDeclaration!.As<VarDeclNode>().Identifier, Is.EqualTo("x"));
            Assert.That(forStat.Condition.As<IdNode>().Identifier, Is.EqualTo("xs"));
        }

        [Test]
        public void ForLoopWithAttributeTargetIsRepresentedAsDottedIdentifier()
        {
            var forStat = this.ParseStatement("for obj.x in xs:\n    pass\n").As<ForStatNode>();

            Assert.That(forStat.ForDeclaration!.As<VarDeclNode>().Identifier, Is.EqualTo("obj.x"));
        }

        [Test]
        public void AsyncForBuildsAsyncStatNode()
        {
            var asyncFor = this.ParseStatement("async for x in items:\n    pass\n").As<AsyncStatNode>();

            Assert.That(asyncFor.Tags.Single().Identifier, Is.EqualTo("async"));
            Assert.That(asyncFor.Statement, Is.TypeOf<ForStatNode>());
        }

        [Test]
        public void AsyncWithBuildsAsyncStatNode()
        {
            var asyncWith = this.ParseStatement("async with lock:\n    pass\n").As<AsyncStatNode>();

            Assert.That(asyncWith.Tags.Single().Identifier, Is.EqualTo("async"));
            Assert.That(asyncWith.Statement, Is.TypeOf<WithStatNode>());
        }

        private StatNode ParseStatement(string source)
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<StatNode>();
    }
}
