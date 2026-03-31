using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using Newtonsoft.Json;

namespace LINVAST.Imperative.Nodes
{
    public abstract class ExprNode : ASTNode
    {
        protected ExprNode(int line, IEnumerable<ASTNode> children)
            : base(line, children) { }

        protected ExprNode(int line, params ASTNode[] children)
            : base(line, children) { }
    }

    public class ExprListNode : ExprNode
    {
        [JsonIgnore]
        public IEnumerable<ExprNode> Expressions => this.Children.Cast<ExprNode>();


        public ExprListNode(int line, params ExprNode[] expressions)
            : base(line, expressions) { }

        public ExprListNode(int line, IEnumerable<ExprNode> expressions)
            : base(line, expressions) { }


        public override string GetText() => string.Join(", ", this.Children.Select(c => c.GetText()));
    }

    public sealed class IdListNode : ExprListNode
    {
        [JsonIgnore]
        public IEnumerable<IdNode> Identifiers => this.Children.Cast<IdNode>();


        public IdListNode(int line, params IdNode[] expressions)
            : base(line, expressions) { }

        public IdListNode(int line, IEnumerable<IdNode> expressions)
            : base(line, expressions) { }
    }

    public sealed class UnaryExprNode : ExprNode
    {
        [JsonIgnore]
        public UnaryOpNode Operator => this.Children[0].As<UnaryOpNode>();

        [JsonIgnore]
        public ExprNode Operand => this.Children[1].As<ExprNode>();


        public UnaryExprNode(int line, UnaryOpNode @operator, ExprNode operand)
            : base(line, @operator, operand) { }


        public override string GetText() => $"{this.Operator}({this.Operand})";
    }

    public abstract class BinaryExprNode : ExprNode
    {
        [JsonIgnore]
        public BinaryOpNode Operator => this.Children[1].As<BinaryOpNode>();

        [JsonIgnore]
        public ExprNode LeftOperand => this.Children[0].As<ExprNode>();

        [JsonIgnore]
        public ExprNode RightOperand => this.Children[2].As<ExprNode>();


        protected BinaryExprNode(int line, ExprNode left, BinaryOpNode @operator, ExprNode right)
            : base(line, left, @operator, right) { }


        public override string GetText() => $"({this.LeftOperand} {this.Operator} {this.RightOperand})";
    }

    public sealed class ArithmExprNode : BinaryExprNode
    {
        public ArithmExprNode(int line, ExprNode left, ArithmOpNode @operator, ExprNode right)
            : base(line, left, @operator, right) { }
    }

    public sealed class LogicExprNode : BinaryExprNode
    {
        public LogicExprNode(int line, ExprNode left, BinaryLogicOpNode @operator, ExprNode right)
            : base(line, left, @operator, right) { }
    }

    public sealed class RelExprNode : BinaryExprNode
    {
        public RelExprNode(int line, ExprNode left, RelOpNode @operator, ExprNode right)
            : base(line, left, @operator, right) { }
    }

    public class AssignExprNode : BinaryExprNode
    {
        public AssignExprNode(int line, ExprNode left, AssignOpNode @operator, ExprNode right)
            : base(line, left, @operator, right) { }

        public AssignExprNode(int line, ExprNode left, ExprNode right)
            : base(line, left, AssignOpNode.FromSymbol(line, "="), right) { }


        public AssignExprNode SimplifyComplexAssignment()
        {
            if (this.Operator is ComplexAssignOpNode && this.Operator.Symbol.Length > 1) {
                string part = this.Operator.Symbol.Substring(0, this.Operator.Symbol.IndexOf('='));
                var expanded = new ArithmExprNode(this.Line,
                    this.LeftOperand,
                    ArithmOpNode.FromSymbol(this.Operator.Line, part),
                    this.RightOperand
                );
                return new AssignExprNode(this.Line, this.LeftOperand, expanded);
            }
            return this;
        }
    }

    public sealed class IdNode : ExprNode
    {
        public string Identifier { get; }


        public IdNode(int line, string identifier)
            : base(line)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentException("Identifier name must be set.");
            this.Identifier = identifier;
        }


        public override string GetText() => this.Identifier;

        public override bool Equals([AllowNull] ASTNode other)
            => base.Equals(other) && this.Identifier.Equals(other.As<IdNode>().Identifier);
    }

    public class FuncCallExprNode : ExprNode
    {
        [JsonIgnore]
        public string Identifier => this.Children[0].As<IdNode>().Identifier;

        [JsonIgnore]
        public TypeNameListNode? TemplateArguments => this.Children.ElementAtOrDefault(1)?.As<TypeNameListNode>();

        [JsonIgnore]
        public ExprListNode? Arguments
            => this.Children.Count > 2 ? this.Children[2].As<ExprListNode>()
                                       : this.Children.ElementAtOrDefault(1) as ExprListNode;


        public FuncCallExprNode(int line, IdNode identifier)
            : base(line, identifier) { }

        public FuncCallExprNode(int line, IdNode identifier, ExprListNode args)
            : base(line, identifier, args) { }

        public FuncCallExprNode(int line, IdNode identifier, TypeNameListNode templateArgs)
            : base(line, identifier, templateArgs) { }

        public FuncCallExprNode(int line, IdNode identifier, TypeNameListNode templateArgs, ExprListNode args)
            : base(line, identifier, templateArgs, args) { }


        public override string GetText() => $"{this.Identifier}<{(this.TemplateArguments?.GetText() ?? "")}>({this.Arguments?.GetText() ?? ""})";
    }

    public sealed class ConsExprNode : FuncCallExprNode
    {
        public ConsExprNode(int line, IdNode identifier)
            : base(line, identifier) { }

        public ConsExprNode(int line, IdNode identifier, ExprListNode args)
            : base(line, identifier, args) { }

        public ConsExprNode(int line, IdNode identifier, TypeNameListNode templateArgs)
            : base(line, identifier, templateArgs) { }

        public ConsExprNode(int line, IdNode identifier, TypeNameListNode templateArgs, ExprListNode args)
            : base(line, identifier, templateArgs, args) { }
    }

    public sealed class ArrAccessExprNode : ExprNode
    {
        [JsonIgnore]
        public ExprNode Array => this.Children[0].As<ExprNode>();

        [JsonIgnore]
        public ExprNode IndexExpression => this.Children[1].As<ExprNode>();


        public ArrAccessExprNode(int line, ExprNode array, ExprNode indexExpr)
            : base(line, array, indexExpr) { }


        public override string GetText() => $"{this.Array.GetText()}[{this.IndexExpression.GetText()}]";
    }

    public sealed class IncExprNode : AssignExprNode
    {
        [JsonIgnore]
        public ExprNode Expr => this.Children[0].As<ExprNode>();


        public IncExprNode(int line, ExprNode expr)
            : base(line, expr, AssignOpNode.FromSymbol(line, "+="), new LitExprNode(line, 1)) { }


        public override string GetText() => $"{this.LeftOperand.GetText()}++";
    }

    public sealed class DecExprNode : AssignExprNode
    {
        [JsonIgnore]
        public ExprNode Expr => this.Children[0].As<ExprNode>();


        public DecExprNode(int line, ExprNode expr)
            : base(line, expr, AssignOpNode.FromSymbol(line, "-="), new LitExprNode(line, 1)) { }


        public override string GetText() => $"{this.Expr.GetText()}--";
    }

    public class LitExprNode : ExprNode
    {
        public static LitExprNode FromString(int line, string str)
        {
            if (!Constants.TryConvert(str, out object? value, out string? suffix))
                throw new NotImplementedException($"Literal {str} is not supported");
            if (value is null)
                return new NullLitExprNode(line);
            return new LitExprNode(line, value, suffix);
        }


        public object? Value { get; }
        public string? Suffix { get; }
        public TypeCode TypeCode { get; }


        public LitExprNode(int line, object value, string? suffix = null)
            : base(line)
        {
            if (value is null)
                throw new ArgumentNullException("Value cannot be null. Use NullLiteralNode instead.");
            this.Suffix = suffix?.ToUpper();
            this.Value = value;
            this.TypeCode = Type.GetTypeCode(value.GetType());
        }

        protected LitExprNode(int line, object? value, TypeCode typeCode, string? suffix = null)
            : base(line)
        {
            this.Suffix = suffix?.ToUpper();
            this.Value = value;
            this.TypeCode = typeCode;
        }


        public override string GetText() => this.Value is string str ? $"\"{str}\"" : this.Value?.ToString() ?? "";

        public override bool Equals([AllowNull] ASTNode other)
        {
            var lit = other as LitExprNode;
            if (!base.Equals(other) || !this.TypeCode.Equals(lit?.TypeCode))
                return false;
            if (this.Value is null)
                return lit.Value is null;
            return this.Value.Equals(lit.Value);
        }
    }

    public sealed class NullLitExprNode : LitExprNode
    {
        public NullLitExprNode(int line)
            : base(line, null, TypeCode.Empty) { }


        public override string GetText() => "null";

        public override bool Equals([AllowNull] ASTNode other) => other is NullLitExprNode;
    }

    public sealed class CondExprNode : ExprNode
    {
        [JsonIgnore]
        public ExprNode Condition => this.Children[0].As<ExprNode>();

        [JsonIgnore]
        public ExprNode ThenExpression => this.Children[1].As<ExprNode>();

        [JsonIgnore]
        public ExprNode ElseExpression => this.Children[2].As<ExprNode>();


        public CondExprNode(int line, ExprNode cond, ExprNode @then, ExprNode @else)
            : base(line, cond, @then, @else) { }


        public override string GetText()
            => $"{this.Condition.GetText()} ? {this.ThenExpression.GetText()} : {this.ElseExpression.GetText()}";
    }
}
