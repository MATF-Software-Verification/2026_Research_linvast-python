using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using Newtonsoft.Json;

namespace LINVAST.Imperative.Nodes
{
    public abstract class StatNode : ASTNode
    {
        protected StatNode(int line, IEnumerable<ASTNode> children)
            : base(line, children) { }

        protected StatNode(int line, params ASTNode[] children)
            : base(line, children) { }


        public override string GetText() => $"{base.GetText()};";
    }

    public sealed class EmptyStatNode : StatNode
    {
        public EmptyStatNode(int line)
            : base(line) { }
    }

    public abstract class SimpleStatNode : StatNode
    {
        protected SimpleStatNode(int line, IEnumerable<ASTNode> children)
            : base(line, children) { }

        protected SimpleStatNode(int line, params ASTNode[] children)
            : base(line, children) { }
    }

    public class DeclStatNode : SimpleStatNode
    {
        [JsonIgnore]
        public IEnumerable<ASTNode> ChildrenWithoutTags => this.Children.SkipWhile(e => e is TagNode);

        [JsonIgnore]
        public IEnumerable<TagNode> Tags => this.Children.TakeWhile(e => e is TagNode).Cast<TagNode>();

        [JsonIgnore]
        public DeclSpecsNode Specifiers => this.ChildrenWithoutTags.ElementAt(0).As<DeclSpecsNode>();

        [JsonIgnore]
        public DeclListNode DeclaratorList => this.ChildrenWithoutTags.ElementAt(1).As<DeclListNode>();

        [JsonIgnore]
        public Modifiers Modifiers => this.Specifiers.Modifiers;


        public DeclStatNode(int line, DeclSpecsNode declSpecs, DeclListNode declList)
            : base(line, declSpecs, declList) { }

        public DeclStatNode(int line, IEnumerable<TagNode> tags, DeclSpecsNode declSpecs, DeclListNode declList)
            : base(line, tags.Concat(new ASTNode[] { declSpecs, declList })) { }
    }

    public abstract class ComplexStatNode : StatNode
    {
        protected ComplexStatNode(int line, IEnumerable<ASTNode> children)
            : base(line, children) { }

        protected ComplexStatNode(int line, params ASTNode[] children)
            : base(line, children) { }
    }

    public sealed class BlockStatNode : ComplexStatNode
    {
        public BlockStatNode(int line, IEnumerable<ASTNode> children)
            : base(line, children) { }

        public BlockStatNode(int line, params ASTNode[] children)
            : base(line, children) { }


        public override string GetText() => $"{{ {string.Join(" ", this.Children.Select(c => c.GetText()))} }}";
    }

    public class ExprStatNode : SimpleStatNode
    {
        [JsonIgnore]
        public ExprNode Expression => this.Children.First().As<ExprNode>();


        public ExprStatNode(int line, ExprNode expr)
            : base(line, expr) { }
    }

    // Python-specific
    public sealed class DeleteStatNode : SimpleStatNode
    {
        [JsonIgnore]
        public IEnumerable<ExprNode> Targets => this.Children.Cast<ExprNode>();


        public DeleteStatNode(int line, IEnumerable<ExprNode> targets)
            : base(line, targets.Cast<ASTNode>()) { }

        public DeleteStatNode(int line, params ExprNode[] targets)
            : base(line, targets.Cast<ASTNode>()) { }


        public override string GetText() => $"del {string.Join(", ", this.Targets.Select(t => t.GetText()))}";
    }

    // Python-specific
    public sealed class GlobalStatNode : SimpleStatNode
    {
        [JsonIgnore]
        public IEnumerable<IdNode> Identifiers => this.Children.Cast<IdNode>();


        public GlobalStatNode(int line, IEnumerable<IdNode> identifiers)
            : base(line, identifiers.Cast<ASTNode>()) { }

        public GlobalStatNode(int line, params IdNode[] identifiers)
            : base(line, identifiers.Cast<ASTNode>()) { }


        public override string GetText() => $"global {string.Join(", ", this.Identifiers.Select(i => i.Identifier))}";
    }

    // Python-specific
    public sealed class NonlocalStatNode : SimpleStatNode
    {
        [JsonIgnore]
        public IEnumerable<IdNode> Identifiers => this.Children.Cast<IdNode>();


        public NonlocalStatNode(int line, IEnumerable<IdNode> identifiers)
            : base(line, identifiers.Cast<ASTNode>()) { }

        public NonlocalStatNode(int line, params IdNode[] identifiers)
            : base(line, identifiers.Cast<ASTNode>()) { }


        public override string GetText() => $"nonlocal {string.Join(", ", this.Identifiers.Select(i => i.Identifier))}";
    }

    public sealed class IfStatNode : ComplexStatNode
    {
        [JsonIgnore]
        public ExprNode Condition => this.Children[0].As<ExprNode>();

        [JsonIgnore]
        public StatNode ThenStat => this.Children[1].As<StatNode>();

        [JsonIgnore]
        public StatNode? ElseStat => this.Children.ElementAtOrDefault(2)?.As<StatNode>() ?? null;


        public IfStatNode(int line, ExprNode cond, StatNode @then)
            : base(line, cond, @then) { }

        public IfStatNode(int line, ExprNode cond, StatNode @then, StatNode @else)
            : base(line, cond, @then, @else) { }


        public override string GetText()
            => $"if {this.Condition.GetText()} {this.ThenStat.GetText()} {(this.ElseStat is null ? "" : $"else {this.ElseStat.GetText()}")}";
    }

    public sealed class WithStatNode : ComplexStatNode
    {
        [JsonIgnore]
        public ExprNode ContextManager => this.Children[0].As<ExprNode>();

        [JsonIgnore]
        public ExprNode? Target => this.Children.ElementAtOrDefault(1) as ExprNode;

        [JsonIgnore]
        public StatNode Body => this.Children[this.Target is null ? 1 : 2].As<StatNode>();


        public WithStatNode(int line, ExprNode contextManager, StatNode body)
            : base(line, contextManager, body) { }

        public WithStatNode(int line, ExprNode contextManager, ExprNode target, StatNode body)
            : base(line, contextManager, target, body) { }


        public override string GetText()
        {
            var sb = new StringBuilder("with ").Append(this.ContextManager.GetText());
            if (this.Target is not null)
                sb.Append(" as ").Append(this.Target.GetText());
            sb.Append(' ').Append(this.Body.GetText());
            return sb.ToString();
        }
    }

    public sealed class CatchClauseNode : ASTNode
    {
        [JsonIgnore]
        public ExprNode? ExceptionType => this.Children.Count >= 2 ? this.Children[0] as ExprNode : null;

        [JsonIgnore]
        public IdNode? Binding => this.Children.Count == 3 ? this.Children[1].As<IdNode>() : null;

        [JsonIgnore]
        public StatNode Body => this.Children[this.Children.Count - 1].As<StatNode>();


        public CatchClauseNode(int line, StatNode body, ExprNode? exceptionType = null, IdNode? binding = null)
            : base(line, BuildChildren(body, exceptionType, binding)) { }


        public override string GetText()
        {
            var sb = new StringBuilder("except");
            if (this.ExceptionType is not null)
                sb.Append(' ').Append(this.ExceptionType.GetText());
            if (this.Binding is not null)
                sb.Append(" as ").Append(this.Binding.GetText());
            sb.Append(' ').Append(this.Body.GetText());
            return sb.ToString();
        }


        private static ASTNode[] BuildChildren(StatNode body, ExprNode? exceptionType, IdNode? binding)
        {
            if (exceptionType is null)
                return new ASTNode[] { body };
            if (binding is null)
                return new ASTNode[] { exceptionType, body };
            return new ASTNode[] { exceptionType, binding, body };
        }
    }

    public sealed class TryStatNode : ComplexStatNode
    {
        public int CatchClauseCount { get; }
        public bool HasElse { get; }
        public bool HasFinally { get; }

        [JsonIgnore]
        public StatNode TryBody => this.Children[0].As<StatNode>();

        [JsonIgnore]
        public IEnumerable<CatchClauseNode> CatchClauses =>
            this.Children.Skip(1).Take(this.CatchClauseCount).Cast<CatchClauseNode>();

        [JsonIgnore]
        public StatNode? ElseStat => this.HasElse ? this.Children[1 + this.CatchClauseCount].As<StatNode>() : null;

        [JsonIgnore]
        public StatNode? FinallyStat => this.HasFinally ? this.Children[this.Children.Count - 1].As<StatNode>() : null;


        public TryStatNode(
            int line,
            StatNode tryBody,
            CatchClauseNode[] catchClauses,
            StatNode? elseStat,
            StatNode? finallyStat)
            : base(line, AssembleChildren(tryBody, catchClauses, elseStat, finallyStat))
        {
            this.CatchClauseCount = catchClauses.Length;
            this.HasElse = elseStat is not null;
            this.HasFinally = finallyStat is not null;
        }


        public override string GetText()
        {
            var sb = new StringBuilder("try ").Append(this.TryBody.GetText());
            foreach (CatchClauseNode catchClause in this.CatchClauses)
                sb.Append(' ').Append(catchClause.GetText());
            if (this.ElseStat is not null)
                sb.Append(" else ").Append(this.ElseStat.GetText());
            if (this.FinallyStat is not null)
                sb.Append(" finally ").Append(this.FinallyStat.GetText());
            return sb.ToString();
        }


        private static ASTNode[] AssembleChildren(
            StatNode tryBody,
            CatchClauseNode[] catchClauses,
            StatNode? elseStat,
            StatNode? finallyStat)
        {
            var children = new List<ASTNode> { tryBody };
            children.AddRange(catchClauses);
            if (elseStat is not null)
                children.Add(elseStat);
            if (finallyStat is not null)
                children.Add(finallyStat);
            return children.ToArray();
        }
    }

    public sealed class AsyncStatNode : ComplexStatNode
    {
        [JsonIgnore]
        public IEnumerable<TagNode> Tags => this.Children.TakeWhile(e => e is TagNode).Cast<TagNode>();

        [JsonIgnore]
        public StatNode Statement => this.Children.SkipWhile(e => e is TagNode).Single().As<StatNode>();


        public AsyncStatNode(int line, StatNode statement)
            : base(line, new TagNode(line, "async"), statement) { }


        public override string GetText() => $"async {this.Statement.GetText()}";
    }

    public sealed class MatchStatNode : ComplexStatNode
    {
        [JsonIgnore]
        public ExprNode Subject => this.Children[0].As<ExprNode>();

        [JsonIgnore]
        public IEnumerable<CaseNode> Cases => this.Children.Skip(1).Cast<CaseNode>();


        public MatchStatNode(int line, ExprNode subject, IEnumerable<CaseNode> cases)
            : base(line, new ASTNode[] { subject }.Concat(cases)) { }


        public override string GetText()
            => $"match {this.Subject.GetText()} {{ {string.Join(" ", this.Cases.Select(c => c.GetText()))} }}";
    }

    public sealed class JumpStatNode : SimpleStatNode
    {
        public JumpStatType Type { get; set; }

        [JsonIgnore]
        public ExprNode? ReturnExpr => this.Children.FirstOrDefault() as ExprNode ?? null;

        [JsonIgnore]
        public IdNode? GotoLabel => this.Children.First() as IdNode ?? null;


        public JumpStatNode(int line, JumpStatType type)
            : base(line)
        {
            this.Type = type;
        }

        public JumpStatNode(int line, ExprNode? returnExpr)
            : base(line, returnExpr is null ? Enumerable.Empty<ASTNode>() : new[] { returnExpr })
        {
            this.Type = JumpStatType.Return;
        }

        public JumpStatNode(int line, IdNode label)
            : base(line, label)
        {
            this.Type = JumpStatType.Goto;
        }


        public override string GetText()
        {
            var sb = new StringBuilder(this.Type.ToStringToken());
            if (this.Type == JumpStatType.Return && this.ReturnExpr is not null)
                sb.Append(' ').Append(this.ReturnExpr.GetText());
            else if (this.Type == JumpStatType.Goto && this.GotoLabel is not null)
                sb.Append(' ').Append(this.GotoLabel.GetText());
            sb.Append(';');
            return sb.ToString();
        }
    }

    public sealed class LabeledStatNode : SimpleStatNode
    {
        public string Label { get; }

        [JsonIgnore]
        public StatNode Statement => this.Children.First().As<StatNode>();


        public LabeledStatNode(int line, string label, StatNode statement)
            : base(line, statement)
        {
            this.Label = label;
        }


        public override string GetText() => $"{this.Label}: {this.Statement.GetText()}";

        public override bool Equals([AllowNull] ASTNode other)
            => base.Equals(other) && this.Label.Equals((other as LabeledStatNode)?.Label);
    }

    public abstract class IterStatNode : ComplexStatNode
    {
        [JsonIgnore]
        public ExprNode Condition => this.Children[0].As<ExprNode>();

        [JsonIgnore]
        public StatNode Statement => this.Children[1].As<StatNode>();


        protected IterStatNode(int line, ExprNode cond, StatNode stat)
            : base(line, cond, stat) { }

        protected IterStatNode(int line, IEnumerable<ASTNode> children)
            : base(line, children) { }
    }

    public sealed class WhileStatNode : IterStatNode
    {
        public WhileStatNode(int line, ExprNode cond, StatNode stat)
            : base(line, cond, stat) { }


        public override string GetText() => $"while {this.Condition.GetText()} {{ {this.Statement.GetText()} }}";
    }

    public sealed class ForStatNode : IterStatNode
    {
        public DeclarationNode? ForDeclaration { get; }
        public ExprNode? InitExpr { get; }
        public ExprNode? IncrExpr { get; }


        public ForStatNode(int line, DeclarationNode decl, ExprNode? cond, ExprNode? expr, StatNode stat)
            : base(line, new ASTNode[] { cond ?? new LitExprNode(line, true), stat })
        {
            this.ForDeclaration = decl;
            this.InitExpr = null;
            this.IncrExpr = expr;
        }

        public ForStatNode(int line, ExprNode? initExpr, ExprNode? cond, ExprNode? incExpr, StatNode stat)
            : base(line, new ASTNode[] { cond ?? new LitExprNode(line, true), stat })
        {
            this.ForDeclaration = null;
            this.InitExpr = initExpr;
            this.IncrExpr = incExpr;
        }


        public override string GetText()
        {
            var sb = new StringBuilder("for (");
            if (this.ForDeclaration is not null)
                sb.Append(this.ForDeclaration.GetText());
            else if (this.InitExpr is not null)
                sb.Append(this.InitExpr.GetText());
            sb.Append("; ");
            sb.Append(this.Condition.GetText());
            sb.Append("; ");
            if (this.IncrExpr is not null)
                sb.Append(this.IncrExpr.GetText());
            sb.Append(") { ");
            sb.Append(this.Statement.GetText());
            sb.Append(" }");
            return sb.ToString();
        }
    }

    public sealed class ThrowStatNode : ExprStatNode
    {
        public ThrowStatNode(int line, ExprNode exp)
            : base(line, exp) { }

        public override string GetText() => $"throw {this.Expression.GetText()}";
    }
}
