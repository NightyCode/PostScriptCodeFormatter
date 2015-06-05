namespace NightyCode.PostScript.Syntax
{
    public class NameNode : LiteralNode
    {
        #region Constructors and Destructors

        public NameNode(Token token)
            : base(token)
        {
        }

        #endregion


        #region Properties

        public bool IsExecutable
        {
            get
            {
                return Token.Type == TokenType.ExecutableName || Token.Type == TokenType.ArrayStart
                       || Token.Type == TokenType.ArrayEnd || Token.Type == TokenType.DictionaryStart
                       || Token.Type == TokenType.DictionaryEnd || Token.Type == TokenType.ProcedureStart
                       || Token.Type == TokenType.ProcedureEnd;
            }
        }

        #endregion
    }
}