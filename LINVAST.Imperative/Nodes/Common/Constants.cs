using System;
using System.Text.RegularExpressions;
using LINVAST.Exceptions;

namespace LINVAST.Imperative.Nodes.Common
{
    internal static class Constants
    {
        private static readonly Regex _intRegex =
            new(@"^(?<value>(0|[1-9]\d*))(?<suffix>u?l{0,2})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex _intORegex =
            new(@"^(?<value>(0[0-7]*))(?<suffix>u?l{0,2})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex _intHRegex =
            new(@"^(?<value>(0x[0-9a-f]+))(?<suffix>u?l{0,2})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex _floatRegex =
            new(@"^(?<value>([0-9]*\.?[0-9]+([e][-+]?[0-9]+)?))(?<suffix>[flmd]?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex _charRegex =
            new(@"^'(?<value>.)'$", RegexOptions.Compiled | RegexOptions.IgnoreCase);


        public static bool TryConvert(string str, out object? literal, out string? suffix)
        {
            literal = suffix = null;
            str = str.ToLowerInvariant();
            if (string.Equals(str, "null", StringComparison.InvariantCultureIgnoreCase))
                return true;
            if (TryConvertToInt(_intRegex, str, 10, out literal, out suffix))
                return true;
            if (TryConvertToInt(_intHRegex, str, 16, out literal, out suffix))
                return true;
            if (TryConvertToInt(_intORegex, str, 8, out literal, out suffix))
                return true;
            if (TryConvertToFloat(str, out literal, out suffix))
                return true;
            if (TryConvertToBool(str, out literal))
                return true;
            if (TryConvertToChar(str, out literal))
                return true;
            return false;
        }


        private static bool TryConvertToInt(Regex matchRegex, string str, int @base, out object? literal, out string? suffix)
        {
            literal = suffix = null;

            Match m = matchRegex.Match(str);
            if (!m.Success)
                return false;

            string value = m.Groups["value"].Value;
            suffix = m.Groups["suffix"]?.Value;
            if (string.IsNullOrWhiteSpace(suffix))
                suffix = null;  // Out parameter

            if (suffix is null) {
                if (TryPerformConverter(Convert.ToInt32, value, @base, out int? res_int))
                    literal = res_int;
                else if (TryPerformConverter(Convert.ToByte, value, @base, out byte? res_byte))
                    literal = res_byte;
                else if (TryPerformConverter(Convert.ToInt16, value, @base, out short? res_short))
                    literal = res_short;
                else if (TryPerformConverter(Convert.ToUInt16, value, @base, out ushort? res_ushort))
                    literal = res_ushort;
                else if (TryPerformConverter(Convert.ToUInt32, value, @base, out uint? res_uint))
                    literal = res_uint;
                else if (TryPerformConverter(Convert.ToInt64, value, @base, out long? res_long))
                    literal = res_long;
                else if (TryPerformConverter(Convert.ToUInt64, value, @base, out ulong? res_ulong))
                    literal = res_ulong;
                else
                    throw new SyntaxErrorException("Literal does not convert to any known integral type");
            } else {
                switch (suffix.ToUpper()) {
                    case "U":
                        if (!TryPerformConverter(Convert.ToUInt32, value, @base, out uint? uint_suffixed))
                            throw new SyntaxErrorException("Cannot parse literal as unsigned int");
                        literal = uint_suffixed;
                        break;
                    case "L":
                    case "LL":
                        if (!TryPerformConverter(Convert.ToInt64, value, @base, out long? long_suffixed))
                            throw new SyntaxErrorException("Cannot parse literal as long int");
                        literal = long_suffixed;
                        break;
                    case "UL":
                    case "ULL":
                        if (!TryPerformConverter(Convert.ToUInt64, value, @base, out ulong? ulong_suffixed))
                            throw new SyntaxErrorException("Cannot parse literal as unsigned long int");
                        literal = ulong_suffixed;
                        break;
                    default:
                        throw new SyntaxErrorException("Invalid literal suffix");
                }
            }

            return true;
        }

        private static bool TryConvertToFloat(string str, out object? literal, out string? suffix)
        {
            literal = suffix = null;

            Match m = _floatRegex.Match(str);
            if (!m.Success)
                return false;

            string value = m.Groups["value"].Value;
            suffix = m.Groups["suffix"]?.Value;
            if (string.IsNullOrWhiteSpace(suffix))
                suffix = null;  // Out parameter

            if (suffix is null) {
                if (TryPerformConverter(Convert.ToDouble, value, out double? res_double))
                    literal = res_double;
                else if (TryPerformConverter(Convert.ToSingle, value, out float? res_float))
                    literal = res_float;
                else literal = TryPerformConverter(Convert.ToDecimal, value, out decimal? res_decimal)
                    ? res_decimal
                    : throw new SyntaxErrorException("Literal does not convert to any known floating type");
            } else {
                switch (suffix.ToUpper()) {
                    case "F":
                        if (!TryPerformConverter(Convert.ToSingle, value, out float? float_suffixed))
                            throw new SyntaxErrorException("Cannot parse literal as single precision float");
                        literal = float_suffixed;
                        break;
                    case "L":
                    case "D":
                        if (!TryPerformConverter(Convert.ToDouble, value, out double? double_suffixed))
                            throw new SyntaxErrorException("Cannot parse literal as double precision float");
                        literal = double_suffixed;
                        break;
                    case "M":
                        if (!TryPerformConverter(Convert.ToDecimal, value, out decimal? decimal_suffixed))
                            throw new SyntaxErrorException("Cannot parse literal as decimal precision float");
                        literal = decimal_suffixed;
                        break;
                    default:
                        throw new SyntaxErrorException("Invalid literal suffix");
                }
            }

            return true;
        }

        private static bool TryConvertToBool(string str, out object? literal)
        {
            literal = null;
            if (!bool.TryParse(str, out bool value))
                return false;
            literal = value;
            return true;
        }

        private static bool TryConvertToChar(string str, out object? literal)
        {
            literal = null;

            Match m = _charRegex.Match(str);
            if (!m.Success)
                return false;

            string @char = m.Groups["value"].Value;
            if (!char.TryParse(@char, out char value))
                return false;
            literal = value;
            return true;
        }

        private static bool TryPerformConverter<T>(Func<string?, T> converter, string str, out T? result)
            where T : struct
        {
            result = null;
            try {
                result = converter(str);
                return true;
            } catch (FormatException) {
                return false;
            }
        }

        private static bool TryPerformConverter<T>(Func<string?, int, T> converter, string str, int @base, out T? result)
            where T : struct
        {
            result = null;
            try {
                result = converter(str, @base);
                return true;
            } catch (FormatException) {
                return false;
            }
        }
    }
}
