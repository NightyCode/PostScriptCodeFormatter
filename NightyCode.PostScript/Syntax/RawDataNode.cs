namespace NightyCode.PostScript.Syntax
{
    public class RawDataNode : SyntaxNode
    {
        #region Constants and Fields

        private readonly Token _token;

        #endregion


        #region Constructors and Destructors

        public RawDataNode(Token token)
        {
            _token = token;
        }

        #endregion


        #region Properties

        public override string Text
        {
            get
            {
                return _token.Text;
            }
        }

        public Token Token
        {
            get
            {
                return _token;
            }
        }

        #endregion
    }
}