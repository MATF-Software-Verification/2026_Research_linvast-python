using System;

namespace LINVAST.Exceptions
{
    public sealed class UnknownOperatorException : ArgumentException
    {
        public string? Symbol { get; }


        public UnknownOperatorException()
        {

        }

        public UnknownOperatorException(string symbol)
            : base($"Unknown operator: {symbol}")
        {
            this.Symbol = symbol;
        }

        public UnknownOperatorException(string symbol, Exception? innerException)
            : base($"Unknown operator {symbol}", innerException)
        {
            this.Symbol = symbol;
        }

        public UnknownOperatorException(string symbol, string? paramName)
            : base($"Unknown operator {symbol}", paramName)
        {
            this.Symbol = symbol;
        }
    }
}
