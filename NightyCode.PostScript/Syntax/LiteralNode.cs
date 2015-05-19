namespace NightyCode.PostScript.Syntax
{


    #region Namespace Imports

    #endregion


    public class LiteralNode : INode
    {
        #region Constants and Fields

        private readonly Token _token;

        #endregion


        #region Constructors and Destructors

        public LiteralNode(Token token)
        {
            _token = token;
        }

        #endregion


        #region Properties

        public string Text
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