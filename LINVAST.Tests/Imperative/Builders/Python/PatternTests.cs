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

        private TPattern ParsePattern<TPattern>(string source)
            where TPattern : PatternNode
            => this.Parse<TPattern>(source, parser => parser.closed_pattern());

        private TNode Parse<TNode>(string source, Func<Python3Parser, Antlr4.Runtime.ParserRuleContext> entry)
            where TNode : ASTNode
            => this.builder.BuildFromSource(source, entry).As<TNode>();
    }
}
