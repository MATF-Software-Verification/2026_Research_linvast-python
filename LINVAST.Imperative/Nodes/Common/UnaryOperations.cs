using System;
using LINVAST.Exceptions;

namespace LINVAST.Imperative.Nodes.Common
{
    public static class UnaryOperations
    {
        public static Func<object, object> UnaryFromSymbol(string symbol)
        {
            return symbol switch
            {
                "+" => x => x,
                "-" => NegatePrimitive,
                "!" => NotPrimitive,
                "not" => NotPrimitive,
                "~" => BitwiseNotPrimitive,
                "++" => IncrementPrimitive,
                "--" => DecrementPrimitive,
                "*" => x => x,  // TODO
                "&" => x => x,  // TODO
                _ => throw new UnknownOperatorException(symbol)
            };
        }

        public static object NegatePrimitive(object x)
        {
            ThrowIfNotPrimitiveType(x);

            if (x is string)
                throw new SyntaxErrorException("Cannot negate strings");
            else if (x is decimal)
                return -Convert.ToDecimal(x);
            else if (x is double)
                return -Convert.ToDouble(x);
            else if (x is float)
                return -Convert.ToSingle(x);
            else if (x is ulong)
                return (ulong)(-Convert.ToInt64(x));
            else if (x is long)
                return -Convert.ToInt64(x);
            else if (x is uint)
                return -Convert.ToUInt32(x);
            else if (x is int)
                return -Convert.ToInt32(x);
            else if (x is ushort)
                return -Convert.ToUInt16(x);
            else if (x is short)
                return -Convert.ToInt16(x);
            else if (x is char)
                return -Convert.ToChar(x);
            else if (x is byte)
                return -Convert.ToByte(x);
            else if (x is sbyte)
                return -Convert.ToSByte(x);
            else
                throw new EvaluationException("Cannot negate non-primitive types");
        }

        public static object BitwiseNotPrimitive(object x)
        {
            ThrowIfNotPrimitiveType(x);

            if (x is string || x is decimal || x is double || x is float)
                throw new EvaluationException("Bitwise operations can't be performed on floating point numbers");
            else if (x is ulong)
                return ~Convert.ToUInt64(x);
            else if (x is long)
                return ~Convert.ToInt64(x);
            else if (x is uint)
                return ~Convert.ToUInt32(x);
            else if (x is int)
                return ~Convert.ToInt32(x);
            else if (x is ushort)
                return ~Convert.ToUInt16(x);
            else if (x is short)
                return ~Convert.ToInt16(x);
            else if (x is char)
                return ~Convert.ToChar(x);
            else if (x is byte)
                return ~Convert.ToByte(x);
            else if (x is sbyte)
                return ~Convert.ToSByte(x);
            else
                throw new EvaluationException("Cannot perform bitwise not on non-primitive types");
        }

        public static object IncrementPrimitive(object x)
        {
            ThrowIfNotPrimitiveType(x);

            if (x is string)
                throw new EvaluationException("Increment operation can't be performed on strings");
            else if (x is decimal)
                return Convert.ToDecimal(x) + 1m;
            else if (x is double)
                return Convert.ToDouble(x) + 1d;
            else if (x is float)
                return Convert.ToSingle(x) + 1f;
            else if (x is ulong)
                return Convert.ToUInt64(x) + 1uL;
            else if (x is long)
                return Convert.ToInt64(x) + 1L;
            else if (x is uint)
                return Convert.ToUInt32(x) + 1u;
            else if (x is int)
                return Convert.ToInt32(x) + 1;
            else if (x is ushort)
                return Convert.ToUInt16(x) + 1;
            else if (x is short)
                return Convert.ToInt16(x) + 1;
            else if (x is char)
                return Convert.ToChar(x) + 1;
            else if (x is byte)
                return Convert.ToByte(x) + 1;
            else if (x is sbyte)
                return Convert.ToSByte(x) + 1;
            else
                throw new EvaluationException("Cannot perform increment on non-primitive types");
        }

        public static object DecrementPrimitive(object x)
        {
            ThrowIfNotPrimitiveType(x);

            if (x is string)
                throw new EvaluationException("Decrement operation can't be performed on strings");
            else if (x is decimal)
                return Convert.ToDecimal(x) - 1m;
            else if (x is double)
                return Convert.ToDouble(x) - 1d;
            else if (x is float)
                return Convert.ToSingle(x) - 1f;
            else if (x is ulong)
                return Convert.ToUInt64(x) - 1uL;
            else if (x is long)
                return Convert.ToInt64(x) - 1L;
            else if (x is uint)
                return Convert.ToUInt32(x) - 1u;
            else if (x is int)
                return Convert.ToInt32(x) - 1;
            else if (x is ushort)
                return Convert.ToUInt16(x) - 1;
            else if (x is short)
                return Convert.ToInt16(x) - 1;
            else if (x is char)
                return Convert.ToChar(x) - 1;
            else if (x is byte)
                return Convert.ToByte(x) - 1;
            else if (x is sbyte)
                return Convert.ToSByte(x) - 1;
            else
                throw new EvaluationException("Cannot perform decrement on non-primitive types");
        }

        public static object NotPrimitive(object x)
        {
            ThrowIfNotPrimitiveType(x);

            if (x is string)
                throw new EvaluationException("Negate operation can't be performed on strings");
            else if (x is decimal)
                return !Convert.ToBoolean(Convert.ToDecimal(x));
            else if (x is double)
                return !Convert.ToBoolean(Convert.ToDouble(x));
            else if (x is float)
                return !Convert.ToBoolean(Convert.ToSingle(x));
            else if (x is ulong)
                return !Convert.ToBoolean(Convert.ToUInt64(x));
            else if (x is long)
                return !Convert.ToBoolean(Convert.ToInt64(x));
            else if (x is uint)
                return !Convert.ToBoolean(Convert.ToUInt32(x));
            else if (x is int)
                return !Convert.ToBoolean(Convert.ToInt32(x));
            else if (x is ushort)
                return !Convert.ToBoolean(Convert.ToUInt16(x));
            else if (x is short)
                return !Convert.ToBoolean(Convert.ToInt16(x));
            else if (x is char)
                return !Convert.ToBoolean(Convert.ToChar(x));
            else if (x is byte)
                return !Convert.ToBoolean(Convert.ToByte(x));
            else if (x is sbyte)
                return !Convert.ToBoolean(Convert.ToSByte(x));
            else if (x is bool)
                return !Convert.ToBoolean(x);
            else
                throw new EvaluationException("Cannot perform negate on non-primitive types");
        }


        private static void ThrowIfNotPrimitiveType(object x)
        {
            Type tx = x.GetType();
            if (!tx.IsPrimitive && tx != typeof(decimal) && tx != typeof(string))
                throw new EvaluationException("Non-primitive type supplied to arithmetic operation!");
        }
    }
}
