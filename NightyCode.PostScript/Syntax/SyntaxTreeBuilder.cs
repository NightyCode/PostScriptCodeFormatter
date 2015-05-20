namespace NightyCode.PostScript.Syntax
{
    #region Namespace Imports

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using JetBrains.Annotations;

    #endregion


    public static class SyntaxTreeBuilder
    {
        #region Public Methods

        public static void Group(this SyntaxBlock node, string startLiteral, string endLiteral)
        {
            List<LiteralNode> nodes =
                node.Nodes.OfType<LiteralNode>().Where(n => n.Text == startLiteral || n.Text == endLiteral).ToList();

            if (nodes.Count >= 2 && nodes.Count % 2 == 0)
            {
                int startIndex = -1;
                int endIndex = -1;

                for (var i = 0; i < nodes.Count - 1; i ++)
                {
                    LiteralNode blockStart = nodes[i];

                    if (blockStart.Text != startLiteral)
                    {
                        continue;
                    }

                    LiteralNode blockEnd = nodes[i + 1];

                    if (blockEnd.Text != endLiteral)
                    {
                        continue;
                    }

                    startIndex = node.IndexOfNode(blockStart);
                    endIndex = node.IndexOfNode(blockEnd);

                    break;
                }

                if (startIndex != -1 && endIndex != -1)
                {
                    int childNodeCount = endIndex - (startIndex + 1);
                    List<SyntaxNode> children = node.GetNodesRange(startIndex + 1, childNodeCount);
                    SyntaxNode startNode = node.Nodes[startIndex];
                    SyntaxNode endNode = node.Nodes[endIndex];

                    node.RemoveNodesRange(startIndex, childNodeCount + 2);

                    var syntaxBlock = new RegionBlock { StartNode = startNode, EndNode = endNode };

                    foreach (SyntaxNode child in children)
                    {
                        syntaxBlock.AddNode(child);
                    }

                    node.InsertNode(startIndex, syntaxBlock);

                    Group(node, startLiteral, endLiteral);

                    return;
                }
            }

            foreach (SyntaxBlock blockNode in node.Nodes.OfType<SyntaxBlock>())
            {
                Group(blockNode, startLiteral, endLiteral);
            }
        }


        public static SyntaxBlock Parse(this IEnumerable<Token> tokens)
        {
            IEnumerator<Token> enumerator = tokens.GetEnumerator();

            var root = new ProcedureNode();

            while (enumerator.MoveNext())
            {
                root.AddNode(CreateNode(enumerator, enumerator.Current));
            }

            root.Analyze();

            return root;
        }


        public static IEnumerable<Token> ToTokens(this SyntaxNode node)
        {
            var blockNode = node as SyntaxBlock;

            if (blockNode != null)
            {
                return blockNode.ToTokens();
            }

            var literalNode = (LiteralNode)node;

            return new List<Token> { literalNode.Token };
        }

        #endregion


        #region Methods

        private static void Analyze(this SyntaxBlock tree)
        {
            for (var index = 0; index < tree.Nodes.Count; index++)
            {
                SyntaxNode currentNode = tree.Nodes[index];
                var procedureNode = currentNode as ProcedureNode;

                if (procedureNode != null)
                {
                    procedureNode.Analyze();

                    continue;
                }

                var literalNode = (LiteralNode)currentNode;

                if (literalNode is StringNode || literalNode is CommentNode)
                {
                    continue;
                }

                OperatorNode operatorNode = null;

                if (literalNode.Text == "load")
                {
                    operatorNode = ProcessLoadOperator(tree.Nodes, index, currentNode);
                }
                else if (literalNode.Text == "bind")
                {
                    operatorNode = ProcessBindOperator(tree.Nodes, index, currentNode);
                }
                else if (literalNode.Text == "def")
                {
                    operatorNode = ProcessDefOperator(tree.Nodes, index, currentNode);
                }

                if (operatorNode == null)
                {
                    continue;
                }

                index = index - (operatorNode.Nodes.Count - 1);
                tree.RemoveNodesRange(index, operatorNode.Nodes.Count);
                tree.InsertNode(index, operatorNode);
            }
        }


        private static SyntaxNode BuildProcedureNode(IEnumerator<Token> enumerator, Token startToken)
        {
            var procedureNode = new ProcedureNode { StartNode = new LiteralNode(startToken) };

            while (true)
            {
                if (!enumerator.MoveNext())
                {
                    break;
                }

                Token token = enumerator.Current;

                if (token.Type == TokenType.ProcedureEnd)
                {
                    procedureNode.EndNode = new LiteralNode(token);
                    break;
                }

                procedureNode.AddNode(CreateNode(enumerator, token));
            }

            return procedureNode;
        }


        private static SyntaxNode CreateNode(IEnumerator<Token> enumerator, Token token)
        {
            switch (token.Type)
            {
                case TokenType.Comment:
                    return new CommentNode(token);

                case TokenType.String:
                    return new StringNode(token);

                case TokenType.ProcedureStart:
                    return BuildProcedureNode(enumerator, token);

                default:
                    return new LiteralNode(token);
            }
        }


        private static OperatorNode ProcessBindOperator(
            IReadOnlyList<SyntaxNode> nodes,
            int index,
            SyntaxNode bindOperatorNode)
        {
            SyntaxNode procedureNode = nodes.TryGetNode<ProcedureNode>(index - 1);
            SyntaxNode loadOperatorNode = nodes.TryGetOperatorNode(index - 1, "load");

            if (procedureNode == null && loadOperatorNode == null)
            {
                return null;
            }

            var operatorNode = new OperatorNode();
            operatorNode.AddNode(procedureNode ?? loadOperatorNode);
            operatorNode.AddNode(bindOperatorNode);

            return operatorNode;
        }


        private static OperatorNode ProcessDefOperator(
            IReadOnlyList<SyntaxNode> nodes,
            int index,
            SyntaxNode currentNode)
        {
            SyntaxNode valueNode = null;
            var literalNode = nodes.TryGetNode<LiteralNode>(index - 1);
            var operatorNode = nodes.TryGetNode<OperatorNode>(index - 1);

            if (operatorNode != null)
            {
                if (operatorNode.OperatorName == "load" || operatorNode.OperatorName == "bind")
                {
                    valueNode = operatorNode;
                }
            }
            else
            {
                valueNode = literalNode;
            }

            var keyNode = nodes.TryGetNode<LiteralNode>(index - 2);

            if (keyNode == null || valueNode == null)
            {
                return null;
            }

            var defOperatorNode = new OperatorNode();
            defOperatorNode.AddNode(keyNode);
            defOperatorNode.AddNode(valueNode);
            defOperatorNode.AddNode(currentNode);

            return defOperatorNode;
        }


        private static OperatorNode ProcessLoadOperator(
            IReadOnlyList<SyntaxNode> nodes,
            int index,
            SyntaxNode loadOperatorNode)
        {
            LiteralNode nameNode = nodes.TryGetLiteralNameNode(index - 1);

            if (nameNode == null)
            {
                return null;
            }

            var operatorNode = new OperatorNode();
            operatorNode.AddNode(nameNode);
            operatorNode.AddNode(loadOperatorNode);

            return operatorNode;
        }


        private static IEnumerable<Token> ToTokens(this SyntaxBlock node)
        {
            if (node.StartNode != null)
            {
                foreach (Token token in node.StartNode.ToTokens())
                {
                    yield return token;
                }
            }

            foreach (SyntaxNode child in node.Nodes)
            {
                foreach (Token token in child.ToTokens())
                {
                    yield return token;
                }
            }

            if (node.EndNode == null)
            {
                yield break;
            }

            foreach (Token token in node.EndNode.ToTokens())
            {
                yield return token;
            }
        }


        [CanBeNull]
        private static LiteralNode TryGetLiteralNameNode(this IReadOnlyList<SyntaxNode> nodes, int index)
        {
            var literalNode = nodes.TryGetNode<LiteralNode>(index);

            if (literalNode == null)
            {
                return null;
            }

            return literalNode.Text.StartsWith("/", StringComparison.Ordinal) ? literalNode : null;
        }


        [CanBeNull]
        private static LiteralNode TryGetLiteralNode(this SyntaxBlock tree, int index, string name)
        {
            return tree.Nodes.TryGetLiteralNode(index, name);
        }


        [CanBeNull]
        private static LiteralNode TryGetLiteralNode(this IReadOnlyList<SyntaxNode> nodes, int index, string name)
        {
            var literalNode = nodes.TryGetNode<LiteralNode>(index);

            if (literalNode == null)
            {
                return null;
            }

            return literalNode.Text == name ? literalNode : null;
        }


        [CanBeNull]
        private static TNode TryGetNode<TNode>(this SyntaxBlock tree, int index) where TNode : SyntaxNode
        {
            return tree.Nodes.TryGetNode<TNode>(index);
        }


        [CanBeNull]
        private static TNode TryGetNode<TNode>(this IReadOnlyList<SyntaxNode> nodes, int index) where TNode : SyntaxNode
        {
            if (index < 0 || index >= nodes.Count)
            {
                return null;
            }

            return nodes[index] as TNode;
        }


        [CanBeNull]
        private static OperatorNode TryGetOperatorNode(this IReadOnlyList<SyntaxNode> nodes, int index, string name)
        {
            var literalNode = nodes.TryGetNode<OperatorNode>(index);

            if (literalNode == null)
            {
                return null;
            }

            return literalNode.Text == name ? literalNode : null;
        }

        #endregion
    }
}