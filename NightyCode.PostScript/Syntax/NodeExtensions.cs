namespace NightyCode.PostScript.Syntax
{
    public static class NodeExtensions
    {
        #region Public Methods

        public static bool IsLiteral(this SyntaxNode node, string name)
        {
            var literalNode = node as LiteralNode;

            if (literalNode == null)
            {
                return false;
            }

            return literalNode.Text == name;
        }

        #endregion
    }
}