namespace NightyCode.PostScript.Syntax
{
    #region Namespace Imports

    using System.Diagnostics;

    #endregion


    #region Namespace Imports

    #endregion


    public class OperatorNode : SyntaxBlock
    {
        #region Constructors and Destructors

        public OperatorNode(LiteralNode operatorNode, string operatorName)
        {
            OperatorName = operatorName;
            EndNode = operatorNode;
        }

        #endregion


        #region Properties

        public string OperatorAlias
        {
            get
            {
                Debug.Assert(EndNode != null, "EndNode != null");
                return EndNode.Token.Text;
            }
        }

        public string OperatorName
        {
            get;
            private set;
        }

        #endregion
    }
}