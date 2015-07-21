namespace NightyCode.PostScript
{
    #region Namespace Imports

    using System;

    #endregion


    public class PostScriptFormatterException : Exception
    {
        #region Constructors and Destructors

        public PostScriptFormatterException()
        {
        }


        public PostScriptFormatterException(string message)
            : base(message)
        {
        }


        public PostScriptFormatterException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        #endregion
    }
}