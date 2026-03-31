using System.IO;
using Antlr4.Runtime;
using LINVAST.Exceptions;

namespace LINVAST.Builders
{
    public sealed class ThrowExceptionErrorListener : BaseErrorListener, IAntlrErrorListener<int>
    {
        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken symbol, int ln, int col, string msg, RecognitionException e)
            => throw new SyntaxErrorException(msg, ln, col, e);

        public void SyntaxError(TextWriter output, IRecognizer recognizer, int symbol, int ln, int col, string msg, RecognitionException e)
            => throw new SyntaxErrorException(msg, ln, col, e);
    }
}
