using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace LINVAST.Imperative.Nodes
{
    public sealed class ArrDeclNode : DeclNode
    {
        [JsonIgnore]
        public ExprNode? SizeExpression
            => this.Children.Count > 2 ? this.Children[1].As<ExprNode>()
                                       : this.Initializer is not null ? null
                                                                 : this.Children.ElementAtOrDefault(1) as ExprNode;

        [JsonIgnore]
        public ArrInitExprNode? Initializer
            => this.Children.Count > 2 ? this.Children[2].As<ArrInitExprNode>()
                                       : this.Children.ElementAtOrDefault(1) as ArrInitExprNode;


        public ArrDeclNode(int line, IdNode identifier)
            : base(line, identifier) { }

        public ArrDeclNode(int line, IdNode identifier, ExprNode sizeExpr)
            : base(line, identifier, sizeExpr) { }

        public ArrDeclNode(int line, IdNode identifier, ArrInitExprNode init)
            : base(line, identifier, init) { }

        public ArrDeclNode(int line, IdNode identifier, ExprNode sizeExpr, ArrInitExprNode init)
            : base(line, identifier, sizeExpr, init) { }


        public override string GetText()
        {
            var sb = new StringBuilder(base.GetText());
            sb.Append('[').Append(this.SizeExpression?.ToString() ?? "").Append(']');
            if (this.Initializer is not null)
                sb.Append(" = ").Append(this.Initializer.ToString());
            return sb.ToString();
        }
    }

    public sealed class ArrInitExprNode : ExprListNode
    {
        [JsonIgnore]
        public IEnumerable<ExprNode> Initializers => this.Expressions;


        public ArrInitExprNode(int line, IEnumerable<ExprNode> exprs)
            : base(line, exprs) { }

        public ArrInitExprNode(int line, params ExprNode[] exprs)
            : base(line, exprs) { }


        public override string GetText() => $"{{ {string.Join(", ", this.Initializers.Select(i => i.GetText()))} }}";
    }
}
