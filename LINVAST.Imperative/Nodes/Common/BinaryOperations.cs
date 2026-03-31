using System;
using LINVAST.Exceptions;

namespace LINVAST.Imperative.Nodes.Common
{
    public static class BinaryOperations
    {
        public static Func<object, object, object> ArithmeticFromSymbol(string symbol)
        {
            return symbol switch
            {
                "+" => AddPrimitive,
                "-" => SubtractPrimitive,
                "*" => MultiplyPrimitive,
                "/" => DividePrimitive,
                "<<" => ShiftLeftPrimitive,
                ">>" => ShiftRightPrimitive,
                "%" => ModulusPrimitive,
                "//" => throw new NotImplementedException(symbol),
                "div" => throw new NotImplementedException(symbol),
                "mod" => ModulusPrimitive,
                _ => throw new UnknownOperatorException(symbol),
            };
        }

        public static Func<object, object, bool> RelationalFromSymbol(string symbol)
        {
            return symbol switch
            {
                ">" => GreaterThanPrimitive,
                "<" => LessThanPrimitive,
                ">=" => GreaterThanOrEqualPrimitive,
                "<=" => LessThanOrEqualPrimitive,
                "==" => EqualsPrimitive,
                "!=" => NotEqualsPrimitive,
                "~=" => NotEqualsPrimitive,
                "%=" => throw new NotImplementedException(symbol),
                "//=" => throw new NotImplementedException(symbol),
                _ => throw new UnknownOperatorException(symbol),
            };
        }

        public static Func<object, object, object> AssignmentFromSymbol(string symbol)
        {
            return symbol switch
            {
                "=" => (a, b) => b,
                ":=" => (a, b) => b,
                "+=" => AddPrimitive,
                "-=" => SubtractPrimitive,
                "*=" => MultiplyPrimitive,
                "/=" => DividePrimitive,
                "%=" => ModulusPrimitive,
                "<<=" => ShiftLeftPrimitive,
                ">>=" => ShiftRightPrimitive,
                "&=" => BitwiseAndPrimitive,
                "|=" => BitwiseOrPrimitive,
                "^=" => BitwiseXorPrimitive,
                _ => throw new UnknownOperatorException(symbol),
            };
        }

        public static Func<object, object, object> BitwiseBinaryFromSymbol(string symbol)
        {
            return symbol switch
            {
                "<<" => ShiftLeftPrimitive,
                ">>" => ShiftRightPrimitive,
                "&" => BitwiseAndPrimitive,
                "|" => BitwiseOrPrimitive,
                "^" => BitwiseXorPrimitive,
                _ => throw new UnknownOperatorException(symbol),
            };
        }

        public static Func<bool, bool, bool> LogicFromSymbol(string symbol)
        {
            return symbol switch
            {
                "&&" => (a, b) => a && b,
                "and" => (a, b) => a && b,
                "||" => (a, b) => a || b,
                "or" => (a, b) => a || b,
                _ => throw new UnknownOperatorException(symbol),
            };
        }


        public static object AddPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is string || y is string)
                return (string)x + (string)y;
            else if (x is decimal || y is decimal)
                return Convert.ToDecimal(x) + Convert.ToDecimal(y);
            else if (x is double || y is double)
                return Convert.ToDouble(x) + Convert.ToDouble(y);
            else if (x is float || y is float)
                return Convert.ToSingle(x) + Convert.ToSingle(y);
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) + Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) + Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) + Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) + Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) + Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) + Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) + Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) + Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) + Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot add non-primitive types");
        }

        public static object SubtractPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is decimal || y is decimal)
                return Convert.ToDecimal(x) - Convert.ToDecimal(y);
            else if (x is double || y is double)
                return Convert.ToDouble(x) - Convert.ToDouble(y);
            else if (x is float || y is float)
                return Convert.ToSingle(x) - Convert.ToSingle(y);
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) - Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) - Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) - Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) - Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) - Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) - Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) - Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) - Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) - Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot subtract non-primitive types");
        }

        public static object MultiplyPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is decimal || y is decimal)
                return Convert.ToDecimal(x) * Convert.ToDecimal(y);
            else if (x is double || y is double)
                return Convert.ToDouble(x) * Convert.ToDouble(y);
            else if (x is float || y is float)
                return Convert.ToSingle(x) * Convert.ToSingle(y);
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) * Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) * Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) * Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) * Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) * Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) * Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) * Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) * Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) * Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot multiply non-primitive types");
        }

        public static object DividePrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is decimal || y is decimal)
                return Convert.ToDecimal(x) / Convert.ToDecimal(y);
            else if (x is double || y is double)
                return Convert.ToDouble(x) / Convert.ToDouble(y);
            else if (x is float || y is float)
                return Convert.ToSingle(x) / Convert.ToSingle(y);
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) / Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) / Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) / Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) / Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) / Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) / Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) / Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) / Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) / Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot divide non-primitive types");
        }

        public static object ModulusPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is decimal || y is decimal)
                return Convert.ToDecimal(x) % Convert.ToDecimal(y);
            else if (x is double || y is double)
                return Convert.ToDouble(x) % Convert.ToDouble(y);
            else if (x is float || y is float)
                return Convert.ToSingle(x) % Convert.ToSingle(y);
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) % Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) % Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) % Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) % Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) % Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) % Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) % Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) % Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) % Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot divide non-primitive types");
        }

        public static object ShiftLeftPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (y is int || y is long || y is char || y is sbyte || y is byte)
                return x switch
                {
                    ulong _ => Convert.ToUInt64(x) << Convert.ToInt32(y),
                    long _ => Convert.ToInt64(x) << Convert.ToInt32(y),
                    uint _ => Convert.ToUInt32(x) << Convert.ToInt32(y),
                    int _ => Convert.ToInt32(x) << Convert.ToInt32(y),
                    ushort _ => Convert.ToUInt16(x) << Convert.ToInt16(y),
                    short _ => Convert.ToInt16(x) << Convert.ToInt16(y),
                    char _ => Convert.ToChar(x) << Convert.ToInt32(y),
                    sbyte _ => Convert.ToSByte(x) << Convert.ToInt32(y),
                    byte _ => Convert.ToByte(x) << Convert.ToInt32(y),
                    _ => throw new EvaluationException("Cannot shift non-integer types"),
                };
            else {
                throw new EvaluationException("Cannot shift by non-integer amount");
            }
        }

        public static object ShiftRightPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (y is int || y is long || y is char || y is sbyte || y is byte)
                return x switch
                {
                    ulong _ => Convert.ToUInt64(x) >> Convert.ToInt32(y),
                    long _ => Convert.ToInt64(x) >> Convert.ToInt32(y),
                    uint _ => Convert.ToUInt32(x) >> Convert.ToInt32(y),
                    int _ => Convert.ToInt32(x) >> Convert.ToInt32(y),
                    ushort _ => Convert.ToUInt16(x) >> Convert.ToInt16(y),
                    short _ => Convert.ToInt16(x) >> Convert.ToInt16(y),
                    char _ => Convert.ToChar(x) >> Convert.ToInt32(y),
                    sbyte _ => Convert.ToSByte(x) >> Convert.ToInt32(y),
                    byte _ => Convert.ToByte(x) >> Convert.ToInt32(y),
                    _ => throw new EvaluationException("Cannot shift non-integer types"),
                };
            else {
                throw new EvaluationException("Cannot shift by non-integer amount");
            }
        }

        public static object BitwiseAndPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is string || y is string || x is decimal || y is decimal || x is double || y is double || x is float || y is float)
                throw new EvaluationException("Bitwise operations can't be performed on floating point numbers");
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) & Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) & Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) & Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) & Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) & Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) & Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) & Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) & Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) & Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot perform bitwise and on non-primitive types");
        }

        public static object BitwiseXorPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is string || y is string || x is decimal || y is decimal || x is double || y is double || x is float || y is float)
                throw new EvaluationException("Bitwise operations can't be performed on floating point numbers");
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) ^ Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) ^ Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) ^ Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) ^ Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) ^ Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) ^ Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) ^ Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) ^ Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) ^ Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot perform bitwise xor on non-primitive types");
        }

        public static object BitwiseOrPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is string || y is string || x is decimal || y is decimal || x is double || y is double || x is float || y is float)
                throw new EvaluationException("Bitwise operations can't be performed on floating point numbers");
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) | Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) | Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) | Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) | Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) | Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) | Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) | Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) | Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) | Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot perform bitwise or on non-primitive types");
        }

        public static bool LessThanPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is decimal || y is decimal)
                return Convert.ToDecimal(x) < Convert.ToDecimal(y);
            else if (x is double || y is double)
                return Convert.ToDouble(x) < Convert.ToDouble(y);
            else if (x is float || y is float)
                return Convert.ToSingle(x) < Convert.ToSingle(y);
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) < Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) < Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) < Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) < Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) < Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) < Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) < Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) < Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) < Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot compare non-primitive types");
        }

        public static bool LessThanOrEqualPrimitive(object x, object y)

        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is decimal || y is decimal)
                return Convert.ToDecimal(x) <= Convert.ToDecimal(y);
            else if (x is double || y is double)
                return Convert.ToDouble(x) <= Convert.ToDouble(y);
            else if (x is float || y is float)
                return Convert.ToSingle(x) <= Convert.ToSingle(y);
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) <= Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) <= Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) <= Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) <= Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) <= Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) <= Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) <= Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) <= Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) <= Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot compare non-primitive types");
        }

        public static bool GreaterThanPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is decimal || y is decimal)
                return Convert.ToDecimal(x) > Convert.ToDecimal(y);
            else if (x is double || y is double)
                return Convert.ToDouble(x) > Convert.ToDouble(y);
            else if (x is float || y is float)
                return Convert.ToSingle(x) > Convert.ToSingle(y);
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) > Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) > Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) > Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) > Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) > Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) > Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) > Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) > Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) > Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot compare non-primitive types");
        }

        public static bool GreaterThanOrEqualPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            if (x is decimal || y is decimal)
                return Convert.ToDecimal(x) >= Convert.ToDecimal(y);
            else if (x is double || y is double)
                return Convert.ToDouble(x) >= Convert.ToDouble(y);
            else if (x is float || y is float)
                return Convert.ToSingle(x) >= Convert.ToSingle(y);
            else if (x is ulong || y is ulong)
                return Convert.ToUInt64(x) >= Convert.ToUInt64(y);
            else if (x is long || y is long)
                return Convert.ToInt64(x) >= Convert.ToInt64(y);
            else if (x is uint || y is uint)
                return Convert.ToUInt32(x) >= Convert.ToUInt32(y);
            else if (x is int || y is int)
                return Convert.ToInt32(x) >= Convert.ToInt32(y);
            else if (x is ushort || y is ushort)
                return Convert.ToUInt16(x) >= Convert.ToUInt16(y);
            else if (x is short || y is short)
                return Convert.ToInt16(x) >= Convert.ToInt16(y);
            else if (x is char || y is char)
                return Convert.ToChar(x) >= Convert.ToChar(y);
            else if (x is byte || y is byte)
                return Convert.ToByte(x) >= Convert.ToByte(y);
            else if (x is sbyte || y is sbyte)
                return Convert.ToSByte(x) >= Convert.ToSByte(y);
            else
                throw new EvaluationException("Cannot compare non-primitive types");
        }

        public static bool EqualsPrimitive(object x, object y)
        {
            ThrowIfNotPrimitiveTypes(x, y);

            return x.Equals(y);
        }

        public static bool NotEqualsPrimitive(object x, object y)
            => !EqualsPrimitive(x, y);


        private static void ThrowIfNotPrimitiveTypes(object x, object y)
        {
            if (!IsPrimitiveType(x.GetType()) || !IsPrimitiveType(y.GetType()))
                throw new EvaluationException("Non-primitive type supplied to arithmetic operation!");
        }

        private static bool IsPrimitiveType(Type tx)
            => tx.IsPrimitive || tx == typeof(decimal) || tx == typeof(string);
    }
}
