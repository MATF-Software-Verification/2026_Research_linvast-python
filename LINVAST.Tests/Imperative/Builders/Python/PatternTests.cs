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
        public void LiteralPatternBuildsLiteralPatternNode()
        {
            var pattern = this.ParsePattern<LiteralPatternNode>("1");

            Assert.That(pattern.Value, Is.TypeOf<LitExprNode>());
            Assert.That(pattern.Value.As<LitExprNode>().Value, Is.EqualTo(1L));
        }

        [Test]
        public void CapturePatternBuildsCapturePatternNode()
        {
            var pattern = this.ParsePattern<CapturePatternNode>("x");

            Assert.That(pattern.Target.Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void WildcardPatternBuildsWildcardPatternNode()
        {
            var pattern = this.Parse<WildcardPatternNode>("_", parser => parser.wildcard_pattern());

            Assert.That(pattern.GetText(), Is.EqualTo("_"));
        }

        [Test]
        public void ValuePatternBuildsValuePatternNode()
        {
            var pattern = this.ParsePattern<ValuePatternNode>("Color.RED");

            Assert.That(pattern.Value.Identifier, Is.EqualTo("Color.RED"));
        }

        [Test]
        public void GroupPatternWrapsInnerPattern()
        {
            var pattern = this.ParsePattern<GroupPatternNode>("(x)");

            Assert.That(pattern.Pattern, Is.TypeOf<CapturePatternNode>());
            Assert.That(pattern.Pattern.As<CapturePatternNode>().Target.Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void OrPatternBuildsAlternatives()
        {
            var pattern = this.Parse<OrPatternNode>("1 | 2", parser => parser.or_pattern());

            Assert.That(pattern.Alternatives.ToList(), Has.Count.EqualTo(2));
            Assert.That(pattern.Alternatives, Is.All.TypeOf<LiteralPatternNode>());
        }

        [Test]
        public void LiteralExprBuildsExprNode()
        {
            var literal = this.Parse<LitExprNode>("42", parser => parser.literal_expr());

            Assert.That(literal.Value, Is.EqualTo(42L));
        }

        [Test]
        public void BracketSequencePatternBuildsSequencePatternNode()
        {
            var pattern = this.ParsePattern<SequencePatternNode>("[1, 2]");

            Assert.That(pattern.Kind, Is.EqualTo(SequencePatternKind.Bracket));
            Assert.That(pattern.Elements.ToList(), Has.Count.EqualTo(2));
            Assert.That(pattern.Elements, Is.All.TypeOf<LiteralPatternNode>());
        }

        [Test]
        public void EmptyParenSequencePatternBuildsEmptySequence()
        {
            var pattern = this.ParsePattern<SequencePatternNode>("()");

            Assert.That(pattern.Kind, Is.EqualTo(SequencePatternKind.Paren));
            Assert.That(pattern.Elements, Is.Empty);
        }

        [Test]
        public void OpenParenSequencePatternBuildsSingleElementTuple()
        {
            var pattern = this.ParsePattern<SequencePatternNode>("(x,)");

            Assert.That(pattern.Kind, Is.EqualTo(SequencePatternKind.OpenParen));
            Assert.That(pattern.Elements.Single(), Is.TypeOf<CapturePatternNode>());
        }

        [Test]
        public void StarCapturePatternBuildsStarPatternNode()
        {
            var sequence = this.ParsePattern<SequencePatternNode>("[*rest]");
            var pattern = sequence.Elements.Single().As<StarPatternNode>();

            Assert.That(pattern.IsWildcard, Is.False);
            Assert.That(pattern.Target!.Identifier, Is.EqualTo("rest"));
        }

        [Test]
        public void StarWildcardPatternBuildsStarPatternNode()
        {
            var pattern = this.Parse<StarPatternNode>("*_", parser => parser.star_pattern());

            Assert.That(pattern.IsWildcard, Is.True);
            Assert.That(pattern.Target, Is.Null);
        }

        [Test]
        public void EmptyMappingPatternBuildsMappingPatternNode()
        {
            var pattern = this.ParsePattern<MappingPatternNode>("{}");

            Assert.That(pattern.Items, Is.Empty);
            Assert.That(pattern.Rest, Is.Null);
        }

        [Test]
        public void MappingPatternWithRestBuildsDoubleStarPatternNode()
        {
            var pattern = this.ParsePattern<MappingPatternNode>("{**rest}");

            Assert.That(pattern.Items, Is.Empty);
            Assert.That(pattern.Rest!.Target.Identifier, Is.EqualTo("rest"));
        }

        [Test]
        public void MappingPatternWithItemsBuildsKeyValuePatternNodes()
        {
            var pattern = this.ParsePattern<MappingPatternNode>("{1: x}");

            Assert.That(pattern.Items.ToList(), Has.Count.EqualTo(1));
            Assert.That(pattern.Items.Single().Key, Is.TypeOf<LitExprNode>());
            Assert.That(pattern.Items.Single().Value, Is.TypeOf<CapturePatternNode>());
        }

        [Test]
        public void EmptyClassPatternBuildsClassPatternNode()
        {
            var pattern = this.ParsePattern<ClassPatternNode>("Point()");

            Assert.That(pattern.ClassName.Identifier, Is.EqualTo("Point"));
            Assert.That(pattern.PositionalPatterns, Is.Empty);
            Assert.That(pattern.KeywordPatterns, Is.Empty);
        }

        [Test]
        public void ClassPatternWithPositionalArgumentsBuildsClassPatternNode()
        {
            var pattern = this.ParsePattern<ClassPatternNode>("Point(x, y)");

            Assert.That(pattern.ClassName.Identifier, Is.EqualTo("Point"));
            Assert.That(pattern.PositionalPatterns.ToList(), Has.Count.EqualTo(2));
            Assert.That(pattern.KeywordPatterns, Is.Empty);
        }

        [Test]
        public void ClassPatternWithKeywordArgumentsBuildsClassPatternNode()
        {
            var pattern = this.ParsePattern<ClassPatternNode>("Point(x=1)");

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
