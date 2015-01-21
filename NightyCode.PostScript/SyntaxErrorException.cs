namespace NightyCode.PostScript
{
    #region Namespace Imports

    using System;

    #endregion


    internal class SyntaxErrorException : Exception
    {
        public SyntaxErrorException()
        {
        }


        public SyntaxErrorException(string message)
            : base(message)
        {
        }


        public SyntaxErrorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}