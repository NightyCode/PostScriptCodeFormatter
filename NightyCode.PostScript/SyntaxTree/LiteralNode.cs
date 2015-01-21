namespace NightyCode.PostScript.SyntaxTree
{


    #region Namespace Imports

    #endregion


    public class LiteralNode : INode
    {
        private readonly Token _token;


        public LiteralNode(Token token)
        {
            _token = token;
        }


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
    }
}