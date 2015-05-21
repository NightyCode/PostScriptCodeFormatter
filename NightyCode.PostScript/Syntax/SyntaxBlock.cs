namespace NightyCode.PostScript.Syntax
{
    #region Namespace Imports

    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    #endregion


    public abstract class SyntaxBlock : SyntaxNode
    {
        #region Constants and Fields

        private readonly List<SyntaxNode> _nodes = new List<SyntaxNode>();
        private LiteralNode _endNode;
        private LiteralNode _startNode;

        #endregion


        #region Properties

        [CanBeNull]
        public LiteralNode EndNode
        {
            get
            {
                return _endNode;
            }

            internal set
            {
                _endNode = value;

                if (_endNode != null)
                {
                    _endNode.Parent = this;
                }
            }
        }

        public IReadOnlyList<SyntaxNode> Nodes
        {
            get
            {
                return _nodes;
            }
        }

        [CanBeNull]
        public LiteralNode StartNode
        {
            get
            {
                return _startNode;
            }

            internal set
            {
                _startNode = value;

                if (_startNode != null)
                {
                    _startNode.Parent = this;
                }
            }
        }

        public override string Text
        {
            get
            {
                string startText = string.Empty;
                string endText = string.Empty;

                if (StartNode != null)
                {
                    startText = StartNode.Text + " ";
                }

                if (EndNode != null)
                {
                    endText = " " + EndNode.Text;
                }

                return startText + string.Join(" ", Nodes.Select(c => c.Text)) + endText;
            }
        }

        #endregion


        #region Public Methods

        public void AddNode(SyntaxNode node)
        {
            node.Parent = this;
            _nodes.Add(node);
        }


        public List<SyntaxNode> GetNodesRange(int index, int count)
        {
            return _nodes.GetRange(index, count);
        }


        public int IndexOfNode(SyntaxNode node)
        {
            return _nodes.IndexOf(node);
        }


        public void InsertNode(int index, SyntaxNode node)
        {
            node.Parent = this;
            _nodes.Insert(index, node);
        }


        public void RemoveNode(SyntaxNode node)
        {
            if (_nodes.Remove(node))
            {
                node.Parent = null;
            }
        }


        public void RemoveNodesRange(int index, int count)
        {
            _nodes.RemoveRange(index, count);
        }

        #endregion
    }
}