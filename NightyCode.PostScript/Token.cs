namespace NightyCode.PostScript
{
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

        public string Text
        {
            get;
            private set;
        }

        public TokenType Type
        {
            get;
            private set;
        }

        public string WhitespaceBefore
        {
            get;
            private set;
        }

        #endregion
    }
}