namespace NightyCode.PostScript.SyntaxTree
{
    #region Namespace Imports

    using System.Collections.Generic;

    #endregion


    public class ProcedureNode : BlockNode
    {
        public ProcedureNode(INode startNode, INode endNode, List<INode> children)
            : base(startNode, endNode, children)
        {
        }
    }
}