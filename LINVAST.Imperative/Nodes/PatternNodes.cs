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

    public sealed class LiteralPatternNode : PatternNode
    {
        [JsonIgnore]
        public ExprNode Value => this.Children[0].As<ExprNode>();


        public LiteralPatternNode(int line, ExprNode value)
            : base(line, value) { }


        public override string GetText() => this.Value.GetText();
    }

    public sealed class CapturePatternNode : PatternNode
    {
        [JsonIgnore]
        public IdNode Target => this.Children[0].As<IdNode>();


        public CapturePatternNode(int line, IdNode target)
            : base(line, target) { }


        public override string GetText() => this.Target.GetText();
    }

    public sealed class WildcardPatternNode : PatternNode
    {
        public WildcardPatternNode(int line)
            : base(line) { }


        public override string GetText() => "_";
    }

    public sealed class ValuePatternNode : PatternNode
    {
        [JsonIgnore]
        public IdNode Value => this.Children[0].As<IdNode>();


        public ValuePatternNode(int line, IdNode value)
            : base(line, value) { }


        public override string GetText() => this.Value.GetText();
    }

    public sealed class GroupPatternNode : PatternNode
    {
        [JsonIgnore]
        public PatternNode Pattern => this.Children[0].As<PatternNode>();


        public GroupPatternNode(int line, PatternNode pattern)
            : base(line, pattern) { }


        public override string GetText() => $"({this.Pattern.GetText()})";
    }

    public sealed class OrPatternNode : PatternNode
    {
        [JsonIgnore]
        public IEnumerable<PatternNode> Alternatives => this.Children.Cast<PatternNode>();


        public OrPatternNode(int line, IEnumerable<PatternNode> alternatives)
            : base(line, alternatives) { }


        public override string GetText()
            => string.Join(" | ", this.Alternatives.Select(a => a.GetText()));
    }

    public enum SequencePatternKind
    {
        Bracket,
        Paren,
        OpenParen,
    }

    public sealed class SequencePatternNode : PatternNode
    {
        public SequencePatternKind Kind { get; }

        [JsonIgnore]
        public IEnumerable<PatternNode> Elements => this.Children.Cast<PatternNode>();


        public SequencePatternNode(int line, SequencePatternKind kind, IEnumerable<PatternNode> elements)
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
    }

    public sealed class StarPatternNode : PatternNode
    {
        public bool IsWildcard { get; }

        [JsonIgnore]
        public IdNode? Target => this.IsWildcard ? null : this.Children[0].As<IdNode>();


        public StarPatternNode(int line, IdNode target)
            : base(line, target)
        {
            this.IsWildcard = false;
        }

        public StarPatternNode(int line, WildcardPatternNode wildcard)
            : base(line, wildcard)
        {
            this.IsWildcard = true;
        }


        public override string GetText()
            => this.IsWildcard ? "*_" : $"*{this.Target!.GetText()}";
    }
}
