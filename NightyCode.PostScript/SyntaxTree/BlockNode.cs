namespace NightyCode.PostScript.SyntaxTree
{
    #region Namespace Imports

    using System.Collections.Generic;
    using System.Linq;

    #endregion


    public class BlockNode : INode
    {
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


        public INode StartNode
        {
            get;
            set;
        }

        public INode EndNode
        {
            get;
            set;
        }


        public List<INode> Children
        {
            get;
            private set;
        }

        public virtual string Text
        {
            get
            {
                return StartNode.Text + " " + string.Join(" ", Children.Select(c => c.Text)) + " " + EndNode.Text;
            }
        }
    }
}