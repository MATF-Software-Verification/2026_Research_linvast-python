using System;

namespace LINVAST.Exceptions
{
    public class SemanticErrorException : Exception
    {
        private static string FormErrorMessage(string msg, int line)
            => $"L{line}: {msg}";


        public SemanticErrorException(string message)
            : base(message)
        {

        }

        public SemanticErrorException(string message, Exception? innerException)
            : base(message, innerException)
        {

        }

        public SemanticErrorException(string message, int line)
            : base(FormErrorMessage(message, line))
        {

        }

        public SemanticErrorException(string message, int line, Exception? innerException)
            : base(FormErrorMessage(message, line), innerException)
        {

        }
    }
}
