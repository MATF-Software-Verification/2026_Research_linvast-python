using System;

namespace LINVAST.Exceptions
{
    public sealed class EvaluationException : ArgumentException
    {
        public EvaluationException()
            : base()
        {

        }

        public EvaluationException(string? message)
            : base(message)
        {

        }

        public EvaluationException(string? message, Exception? innerException)
            : base(message, innerException)
        {

        }

        public EvaluationException(string? message, string? paramName)
            : base(message, paramName)
        {

        }
    }
}
