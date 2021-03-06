﻿namespace NightyCode.PostScript
{
    #region Namespace Imports

    using System.Linq;

    #endregion


    public class Token
    {
        #region Constructors and Destructors

        public Token(TokenType type, string text, int line, int column, string whitespaceBefore)
        {
            Type = type;
            Text = text;
            Line = line;
            Column = column;
            WhitespaceBefore = whitespaceBefore;
        }

        #endregion


        #region Properties

        public int Column
        {
            get;
            private set;
        }

        public int Line
        {
            get;
            private set;
        }

        public int NumTokenLines
        {
            get
            {
                return Text.Cast<char>().Count(c => c == '\n');
            }
        }

        public string Text
        {
            get;
            internal set;
        }

        public TokenType Type
        {
            get;
            private set;
        }

        public string WhitespaceBefore
        {
            get;
            internal set;
        }

        #endregion
    }
}