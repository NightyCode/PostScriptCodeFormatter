namespace NightyCode.PostScript.Syntax
{
    #region Namespace Imports

    using System.Collections.Generic;

    #endregion


    public class ProcedureNode : BlockNode
    {
        #region Constructors and Destructors

        public ProcedureNode(INode startNode, INode endNode, List<INode> children)
            : base(startNode, endNode, children)
        {
        }

        #endregion
    }
}