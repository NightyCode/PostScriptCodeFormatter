namespace NightyCode.PostScript.Syntax
{
    #region Namespace Imports

    using System.Linq;

    #endregion


    public class OperatorNode : SyntaxBlock
    {
        #region Properties

        public string OperatorName
        {
            get
            {
                return Nodes.Last().Text;
            }
        }

        #endregion
    }
}