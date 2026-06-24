using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using LINVAST.Nodes;
using Newtonsoft.Json;

namespace LINVAST.Imperative.Nodes
{
    public abstract class PatternNode : ASTNode
    {
        protected PatternNode(int line, IEnumerable<ASTNode> children)
            : base(line, children) { }

        protected PatternNode(int line, params ASTNode[] children)
            : base(line, children) { }
    }

    public sealed class PatternLiteralNode : PatternNode
    {
        [JsonIgnore]
        public ExprNode Value => this.Children[0].As<ExprNode>();


        public PatternLiteralNode(int line, ExprNode value)
            : base(line, value) { }


        public override string GetText() => this.Value.GetText();
    }

    public sealed class PatternCaptureNode : PatternNode
    {
        [JsonIgnore]
        public IdNode Target => this.Children[0].As<IdNode>();


        public PatternCaptureNode(int line, IdNode target)
            : base(line, target) { }


        public override string GetText() => this.Target.GetText();
    }

    public sealed class PatternWildcardNode : PatternNode
    {
        public PatternWildcardNode(int line)
            : base(line) { }


        public override string GetText() => "_";
    }

    public sealed class PatternValueNode : PatternNode
    {
        [JsonIgnore]
        public IdNode Value => this.Children[0].As<IdNode>();


        public PatternValueNode(int line, IdNode value)
            : base(line, value) { }


        public override string GetText() => this.Value.GetText();
    }

    public sealed class PatternOrNode : PatternNode
    {
        [JsonIgnore]
        public IEnumerable<PatternNode> Alternatives => this.Children.Cast<PatternNode>();


        public PatternOrNode(int line, IEnumerable<PatternNode> alternatives)
            : base(line, alternatives) { }


        public override string GetText()
            => string.Join(" | ", this.Alternatives.Select(a => a.GetText()));
    }

    public sealed class PatternAsNode : PatternNode
    {
        [JsonIgnore]
        public PatternNode Pattern => this.Children[0].As<PatternNode>();

        [JsonIgnore]
        public IdNode Target => this.Children[1].As<IdNode>();


        public PatternAsNode(int line, PatternNode pattern, IdNode target)
            : base(line, pattern, target) { }


        public override string GetText() => $"{this.Pattern.GetText()} as {this.Target.GetText()}";
    }

    public sealed class CaseNode : ASTNode
    {
        [JsonIgnore]
        public PatternNode Pattern => this.Children[0].As<PatternNode>();

        [JsonIgnore]
        public ExprNode? Guard => this.Children.Count == 3 ? this.Children[1].As<ExprNode>() : null;

        [JsonIgnore]
        public StatNode Body => this.Children[this.Children.Count - 1].As<StatNode>();


        public CaseNode(int line, PatternNode pattern, StatNode body)
            : base(line, pattern, body) { }

        public CaseNode(int line, PatternNode pattern, ExprNode guard, StatNode body)
            : base(line, pattern, guard, body) { }


        public override string GetText()
        {
            var sb = new StringBuilder("case ").Append(this.Pattern.GetText());
            if (this.Guard is not null)
                sb.Append(" if ").Append(this.Guard.GetText());
            sb.Append(' ').Append(this.Body.GetText());
            return sb.ToString();
        }
    }

    public enum SequencePatternKind
    {
        Bracket,
        Paren,
        OpenParen,
    }

    public sealed class PatternSequenceNode : PatternNode
    {
        public SequencePatternKind Kind { get; }

        [JsonIgnore]
        public IEnumerable<PatternNode> Elements => this.Children.Cast<PatternNode>();


        public PatternSequenceNode(int line, SequencePatternKind kind, IEnumerable<PatternNode> elements)
            : base(line, elements)
        {
            this.Kind = kind;
        }


        public override string GetText()
        {
            string open = this.Kind == SequencePatternKind.Bracket ? "[" : "(";
            string close = this.Kind == SequencePatternKind.Bracket ? "]" : ")";
            return $"{open}{string.Join(", ", this.Elements.Select(e => e.GetText()))}{close}";
        }

        public override bool Equals([AllowNull] ASTNode other)
            => base.Equals(other) && this.Kind == ((PatternSequenceNode)other!).Kind;
    }

    public sealed class PatternStarNode : PatternNode
    {
        public bool IsWildcard { get; }

        [JsonIgnore]
        public IdNode? Target => this.IsWildcard ? null : this.Children[0].As<IdNode>();


        public PatternStarNode(int line, IdNode target)
            : base(line, target)
        {
            this.IsWildcard = false;
        }

        public PatternStarNode(int line, PatternWildcardNode wildcard)
            : base(line, wildcard)
        {
            this.IsWildcard = true;
        }


        public override string GetText()
            => this.IsWildcard ? "*_" : $"*{this.Target!.GetText()}";
    }

    public sealed class PatternKeyValueNode : PatternNode
    {
        [JsonIgnore]
        public ExprNode Key => this.Children[0].As<ExprNode>();

        [JsonIgnore]
        public PatternNode Value => this.Children[1].As<PatternNode>();


        public PatternKeyValueNode(int line, ExprNode key, PatternNode value)
            : base(line, key, value) { }


        public override string GetText() => $"{this.Key.GetText()}: {this.Value.GetText()}";
    }

    public sealed class PatternDoubleStarNode : PatternNode
    {
        [JsonIgnore]
        public IdNode Target => this.Children[0].As<IdNode>();


        public PatternDoubleStarNode(int line, IdNode target)
            : base(line, target) { }


        public override string GetText() => $"**{this.Target.GetText()}";
    }

    public sealed class PatternMappingNode : PatternNode
    {
        public int ItemCount { get; }
        public bool HasRest { get; }

        [JsonIgnore]
        public IEnumerable<PatternKeyValueNode> Items =>
            this.Children.Take(this.ItemCount).Cast<PatternKeyValueNode>();

        [JsonIgnore]
        public PatternDoubleStarNode? Rest =>
            this.HasRest ? this.Children[this.ItemCount].As<PatternDoubleStarNode>() : null;


        public PatternMappingNode(int line, PatternKeyValueNode[] items, PatternDoubleStarNode? rest)
            : base(line, AssembleChildren(items, rest))
        {
            this.ItemCount = items.Length;
            this.HasRest = rest is not null;
        }


        public override string GetText()
        {
            var parts = this.Items.Select(i => i.GetText()).ToList();
            if (this.Rest is not null)
                parts.Add(this.Rest.GetText());
            return $"{{{string.Join(", ", parts)}}}";
        }


        private static ASTNode[] AssembleChildren(PatternKeyValueNode[] items, PatternDoubleStarNode? rest)
        {
            var children = new List<ASTNode>(items);
            if (rest is not null)
                children.Add(rest);
            return children.ToArray();
        }
    }

    public sealed class PatternKeywordNode : PatternNode
    {
        [JsonIgnore]
        public IdNode Name => this.Children[0].As<IdNode>();

        [JsonIgnore]
        public PatternNode Pattern => this.Children[1].As<PatternNode>();


        public PatternKeywordNode(int line, IdNode name, PatternNode pattern)
            : base(line, name, pattern) { }


        public override string GetText() => $"{this.Name.GetText()}={this.Pattern.GetText()}";
    }

    public sealed class PatternClassNode : PatternNode
    {
        public int PositionalPatternCount { get; }
        public bool HasKeywordPatterns { get; }

        [JsonIgnore]
        public IdNode ClassName => this.Children[0].As<IdNode>();

        [JsonIgnore]
        public IEnumerable<PatternNode> PositionalPatterns =>
            this.Children.Skip(1).Take(this.PositionalPatternCount).Cast<PatternNode>();

        [JsonIgnore]
        public IEnumerable<PatternKeywordNode> KeywordPatterns =>
            this.Children.Skip(1 + this.PositionalPatternCount).Cast<PatternKeywordNode>();


        public PatternClassNode(
            int line,
            IdNode className,
            PatternNode[] positionalPatterns,
            PatternKeywordNode[] keywordPatterns)
            : base(line, AssembleChildren(className, positionalPatterns, keywordPatterns))
        {
            this.PositionalPatternCount = positionalPatterns.Length;
            this.HasKeywordPatterns = keywordPatterns.Length > 0;
        }


        public override string GetText()
        {
            var args = this.PositionalPatterns.Select(p => p.GetText()).ToList();
            args.AddRange(this.KeywordPatterns.Select(k => k.GetText()));
            return $"{this.ClassName.GetText()}({string.Join(", ", args)})";
        }


        private static ASTNode[] AssembleChildren(
            IdNode className,
            PatternNode[] positionalPatterns,
            PatternKeywordNode[] keywordPatterns)
        {
            var children = new List<ASTNode> { className };
            children.AddRange(positionalPatterns);
            children.AddRange(keywordPatterns);
            return children.ToArray();
        }
    }

    public sealed class PatternListNode : PatternNode
    {
        [JsonIgnore]
        public IEnumerable<PatternNode> Patterns => this.Children.Cast<PatternNode>();


        public PatternListNode(int line, IEnumerable<PatternNode> patterns)
            : base(line, patterns) { }


        public override string GetText()
            => string.Join(", ", this.Patterns.Select(p => p.GetText()));
    }
}
