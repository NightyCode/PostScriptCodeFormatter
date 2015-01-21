namespace NightyCode.PostScript
{
    public class Token
    {
        public Token(TokenType type, string text, int line, int column)
        {
            Type = type;
            Text = text;
            Line = line;
            Column = column;
        }


        public TokenType Type
        {
            get;
            private set;
        }

        public string Text
        {
            get;
            private set;
        }

        public int Line
        {
            get;
            private set;
        }

        public int Column
        {
            get;
            private set;
        }
    }
}