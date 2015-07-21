namespace NightyCode.PostScript
{
    #region Namespace Imports

    using System;

    #endregion


    internal class PostScriptReaderException : Exception
    {
        #region Constructors and Destructors

        public PostScriptReaderException()
        {
        }


        public PostScriptReaderException(string message)
            : base(message)
        {
        }


        public PostScriptReaderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion
    }
}