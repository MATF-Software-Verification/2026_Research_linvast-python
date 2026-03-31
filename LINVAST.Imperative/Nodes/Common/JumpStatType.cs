using System;

namespace LINVAST.Imperative.Nodes.Common
{
    public enum JumpStatType
    {
        Return,
        Continue,
        Break,
        Goto
    }

    public static class JumpStatementTypeConverter
    {
        public static JumpStatType FromString(string str)
        {
            return str switch
            {
                "return" => JumpStatType.Return,
                "continue" => JumpStatType.Continue,
                "break" => JumpStatType.Break,
                "goto" => JumpStatType.Goto,
                _ => throw new ArgumentException("Invalid jump statement token"),
            };
        }

        public static string ToStringToken(this JumpStatType type)
        {
            return type switch
            {
                JumpStatType.Break => "break",
                JumpStatType.Continue => "continue",
                JumpStatType.Goto => "goto",
                JumpStatType.Return => "return",
                _ => throw new ArgumentException("Invalid jump statement value"),
            };
        }
    }
}
