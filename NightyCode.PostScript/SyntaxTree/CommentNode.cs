namespace NightyCode.PostScript.SyntaxTree
{
    public class CommentNode : LiteralNode
    {
        public CommentNode(Token token)
            : base(token)
        {
        }
    }
}