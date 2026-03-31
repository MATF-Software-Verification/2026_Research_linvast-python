using System;
using System.Diagnostics.CodeAnalysis;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using Newtonsoft.Json;

namespace LINVAST.Imperative.Nodes
{
    public abstract class OpNode : ASTNode
    {
        public string Symbol { get; }


        protected OpNode(int line, string symbol)
            : base(line)
        {
            this.Symbol = symbol;
        }


        public override string GetText() => this.Symbol;

        public override bool Equals([AllowNull] ASTNode other)
            => base.Equals(other) && this.Symbol.Equals((other as OpNode)?.Symbol);
    }

    public sealed class UnaryOpNode : OpNode
    {
        public static UnaryOpNode FromSymbol(int line, string symbol)
            => new(line, symbol, UnaryOperations.UnaryFromSymbol(symbol));


        [JsonIgnore]
        public Func<object, object> ApplyTo { get; set; }


        public UnaryOpNode(int line, string symbol, Func<object, object> logic)
            : base(line, symbol)
        {
            this.ApplyTo = logic;
        }
    }

    public abstract class BinaryOpNode : OpNode
    {
        [JsonIgnore]
        public Func<object, object, object> ApplyTo { get; set; }


        protected BinaryOpNode(int line, string symbol, Func<object, object, object> logic)
            : base(line, symbol)
        {
            this.ApplyTo = logic;
        }
    }

    public sealed class ArithmOpNode : BinaryOpNode
    {
        public static ArithmOpNode FromSymbol(int line, string symbol)
            => new(line, symbol, BinaryOperations.ArithmeticFromSymbol(symbol));

        public static ArithmOpNode FromBitwiseSymbol(int line, string symbol)
            => new(line, symbol, BinaryOperations.BitwiseBinaryFromSymbol(symbol));


        public ArithmOpNode(int line, string symbol, Func<object, object, object> logic)
            : base(line, symbol, logic) { }
    }

    public sealed class RelOpNode : BinaryOpNode
    {
        public static RelOpNode FromSymbol(int line, string symbol)
            => new(line, symbol, BinaryOperations.RelationalFromSymbol(symbol));


        public RelOpNode(int line, string symbol, Func<object, object, bool> logic)
            : base(line, symbol, (x, y) => logic(x, y)) { }
    }

    public sealed class BinaryLogicOpNode : BinaryOpNode
    {
        public static BinaryLogicOpNode FromSymbol(int line, string symbol)
            => new(line, symbol, BinaryOperations.LogicFromSymbol(symbol));


        public BinaryLogicOpNode(int line, string symbol, Func<bool, bool, bool> logic)
            : base(line, symbol, (x, y) => logic(Convert.ToBoolean(x), Convert.ToBoolean(y))) { }
    }

    public class AssignOpNode : BinaryOpNode
    {
        public static AssignOpNode FromSymbol(int line, string symbol)
        {
            return symbol == "=" || symbol == ":="
                ? new AssignOpNode(line, symbol, (a, b) => b)
                : new ComplexAssignOpNode(line, symbol, BinaryOperations.AssignmentFromSymbol(symbol));
        }


        public AssignOpNode(int line, string symbol, Func<object, object, object> logic)
            : base(line, symbol, logic) { }
    }

    public sealed class ComplexAssignOpNode : AssignOpNode
    {
        public ComplexAssignOpNode(int line, string symbol, Func<object, object, object> logic)
            : base(line, symbol, logic) { }
    }
}
