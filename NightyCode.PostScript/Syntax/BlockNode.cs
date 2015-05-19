namespace NightyCode.PostScript.Syntax
{
    #region Namespace Imports

    using System.Collections.Generic;
    using System.Linq;

    #endregion


    public class BlockNode : INode
    {
        #region Constructors and Destructors

        public BlockNode()
        {
            Children = new List<INode>();
        }


        public BlockNode(INode startNode, INode endNode, List<INode> children)
            : this()
        {
            StartNode = startNode;
            EndNode = endNode;

            Children.AddRange(children);
        }

        #endregion


        #region Properties

        public List<INode> Children
        {
            get;
            private set;
        }

        public INode EndNode
        {
            get;
            set;
        }

        public INode StartNode
        {
            get;
            set;
        }

        public virtual string Text
        {
            get
            {
                return StartNode.Text + " " + string.Join(" ", Children.Select(c => c.Text)) + " " + EndNode.Text;
            }
        }

        #endregion
    }
}