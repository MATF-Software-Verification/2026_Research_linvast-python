using System;
using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class PatternTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void LiteralPatternBuildsPatternLiteralNode()
        {
            var pattern = this.ParsePattern<PatternLiteralNode>("1");

            Assert.That(pattern.Value, Is.TypeOf<LitExprNode>());
            Assert.That(pattern.Value.As<LitExprNode>().Value, Is.EqualTo(1L));
        }

        [Test]
        public void CapturePatternBuildsPatternCaptureNode()
        {
            var pattern = this.ParsePattern<PatternCaptureNode>("x");

            Assert.That(pattern.Target.Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void WildcardPatternBuildsPatternWildcardNode()
        {
            var pattern = this.Parse<PatternWildcardNode>("_", parser => parser.wildcard_pattern());

            Assert.That(pattern.GetText(), Is.EqualTo("_"));
        }

        [Test]
        public void ValuePatternBuildsPatternValueNode()
        {
            var pattern = this.ParsePattern<PatternValueNode>("Color.RED");

            Assert.That(pattern.Value.Identifier, Is.EqualTo("Color.RED"));
        }

        [Test]
        public void GroupPatternReturnsInnerPattern()
        {
            var pattern = this.ParsePattern<PatternCaptureNode>("(x)");

            Assert.That(pattern.Target.Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void OrPatternBuildsAlternatives()
        {
            var pattern = this.Parse<PatternOrNode>("1 | 2", parser => parser.or_pattern());

            Assert.That(pattern.Alternatives.ToList(), Has.Count.EqualTo(2));
            Assert.That(pattern.Alternatives, Is.All.TypeOf<PatternLiteralNode>());
        }

        [Test]
        public void BracketSequencePatternBuildsPatternSequenceNode()
        {
            var pattern = this.ParsePattern<PatternSequenceNode>("[1, 2]");

            Assert.That(pattern.Kind, Is.EqualTo(SequencePatternKind.Bracket));
            Assert.That(pattern.Elements.ToList(), Has.Count.EqualTo(2));
            Assert.That(pattern.Elements, Is.All.TypeOf<PatternLiteralNode>());
        }

        [Test]
        public void EmptyParenSequencePatternBuildsEmptySequence()
        {
            var pattern = this.ParsePattern<PatternSequenceNode>("()");

            Assert.That(pattern.Kind, Is.EqualTo(SequencePatternKind.Paren));
            Assert.That(pattern.Elements, Is.Empty);
        }

        [Test]
        public void OpenParenSequencePatternBuildsSingleElementTuple()
        {
            var pattern = this.ParsePattern<PatternSequenceNode>("(x,)");

            Assert.That(pattern.Kind, Is.EqualTo(SequencePatternKind.OpenParen));
            Assert.That(pattern.Elements.Single(), Is.TypeOf<PatternCaptureNode>());
        }

        [Test]
        public void StarCapturePatternBuildsPatternStarNode()
        {
            var sequence = this.ParsePattern<PatternSequenceNode>("[*rest]");
            var pattern = sequence.Elements.Single().As<PatternStarNode>();

            Assert.That(pattern.IsWildcard, Is.False);
            Assert.That(pattern.Target!.Identifier, Is.EqualTo("rest"));
        }

        [Test]
        public void StarWildcardPatternBuildsPatternStarNode()
        {
            var pattern = this.Parse<PatternStarNode>("*_", parser => parser.star_pattern());

            Assert.That(pattern.IsWildcard, Is.True);
            Assert.That(pattern.Target, Is.Null);
        }

        [Test]
        public void EmptyMappingPatternBuildsPatternMappingNode()
        {
            var pattern = this.ParsePattern<PatternMappingNode>("{}");

            Assert.That(pattern.Items, Is.Empty);
            Assert.That(pattern.Rest, Is.Null);
        }

        [Test]
        public void MappingPatternWithRestBuildsPatternDoubleStarNode()
        {
            var pattern = this.ParsePattern<PatternMappingNode>("{**rest}");

            Assert.That(pattern.Items, Is.Empty);
            Assert.That(pattern.Rest!.Target.Identifier, Is.EqualTo("rest"));
        }

        [Test]
        public void MappingPatternWithItemsBuildsPatternKeyValueNodes()
        {
            var pattern = this.ParsePattern<PatternMappingNode>("{1: x}");

            Assert.That(pattern.Items.ToList(), Has.Count.EqualTo(1));
            Assert.That(pattern.Items.Single().Key, Is.TypeOf<LitExprNode>());
            Assert.That(pattern.Items.Single().Value, Is.TypeOf<PatternCaptureNode>());
        }

        [Test]
        public void EmptyClassPatternBuildsPatternClassNode()
        {
            var pattern = this.ParsePattern<PatternClassNode>("Point()");

            Assert.That(pattern.ClassName.Identifier, Is.EqualTo("Point"));
            Assert.That(pattern.PositionalPatterns, Is.Empty);
            Assert.That(pattern.KeywordPatterns, Is.Empty);
        }

        [Test]
        public void ClassPatternWithPositionalArgumentsBuildsPatternClassNode()
        {
            var pattern = this.ParsePattern<PatternClassNode>("Point(x, y)");

            Assert.That(pattern.ClassName.Identifier, Is.EqualTo("Point"));
            Assert.That(pattern.PositionalPatterns.ToList(), Has.Count.EqualTo(2));
            Assert.That(pattern.KeywordPatterns, Is.Empty);
        }

        [Test]
        public void ClassPatternWithKeywordArgumentsBuildsPatternClassNode()
        {
            var pattern = this.ParsePattern<PatternClassNode>("Point(x=1)");

            Assert.That(pattern.ClassName.Identifier, Is.EqualTo("Point"));
            Assert.That(pattern.PositionalPatterns, Is.Empty);
            Assert.That(pattern.KeywordPatterns.Single().Name.Identifier, Is.EqualTo("x"));
        }

        private TPattern ParsePattern<TPattern>(string source)
            where TPattern : PatternNode
            => this.Parse<TPattern>(source, parser => parser.closed_pattern());

        private TNode Parse<TNode>(string source, Func<Python3Parser, Antlr4.Runtime.ParserRuleContext> entry)
            where TNode : ASTNode
            => this.builder.BuildFromSource(source, entry).As<TNode>();
    }
}
