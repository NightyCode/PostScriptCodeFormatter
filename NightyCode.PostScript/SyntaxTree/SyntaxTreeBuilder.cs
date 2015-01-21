namespace NightyCode.PostScript.SyntaxTree
{
    #region Namespace Imports

    using System.Collections.Generic;
    using System.Linq;

    #endregion


    public static class SyntaxTreeBuilder
    {
        public static IEnumerable<Token> GetTokens(PostScriptReader reader)
        {
            while (true)
            {
                Token token = reader.Read();

                if (token == null)
                {
                    yield break;
                }

                yield return token;
            }
        }


        public static BlockNode Build(PostScriptReader reader)
        {
            return Build(GetTokens(reader));
        }


        public static BlockNode Build(IEnumerable<Token> tokens)
        {
            IEnumerator<Token> enumerator = tokens.GetEnumerator();

            BlockNode root = new BlockNode();

            while (enumerator.MoveNext())
            {
                root.Children.Add(CreateNode(enumerator, enumerator.Current));
            }

            return root;
        }


        public static void GroupNodes(BlockNode node, string startLiteral, string endLiteral)
        {
            List<LiteralNode> nodes = node.Children.OfType<LiteralNode>().Where(n => n.Text == startLiteral || n.Text == endLiteral).ToList();

            if (nodes.Count >= 2 && nodes.Count % 2 == 0)
            {
                int startIndex = -1;
                int endIndex = -1;

                for (int i = 0; i < nodes.Count - 1; i ++)
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

                    startIndex = node.Children.IndexOf(blockStart);
                    endIndex = node.Children.IndexOf(blockEnd);

                    break;
                }

                if (startIndex != -1 && endIndex != -1)
                {
                    int childNodeCount = endIndex - (startIndex + 1);
                    List<INode> children = node.Children.GetRange(startIndex + 1, childNodeCount);
                    INode startNode = node.Children[startIndex];
                    INode endNode = node.Children[endIndex];
                    node.Children.RemoveRange(startIndex, childNodeCount + 2);

                    node.Children.Insert(startIndex, new BlockNode(startNode, endNode, children));

                    GroupNodes(node, startLiteral, endLiteral);

                    return;
                }
            }

            foreach (var blockNode in node.Children.OfType<BlockNode>())
            {
                GroupNodes(blockNode, startLiteral, endLiteral);
            }
        }


        private static INode CreateNode(IEnumerator<Token> enumerator, Token token)
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


        private static INode BuildProcedureNode(IEnumerator<Token> enumerator, Token startToken)
        {
            Token endToken = null;
            List<INode> children = new List<INode>();

            while (true)
            {
                if (!enumerator.MoveNext())
                {
                    break;
                }

                Token token = enumerator.Current;

                if (token.Type == TokenType.ProcedureEnd)
                {
                    endToken = token;
                    break;
                }

                children.Add(CreateNode(enumerator, token));
            }

            var startNode = new LiteralNode(startToken);

            LiteralNode endNode = null;
            if (endToken != null)
            {
                endNode = new LiteralNode(endToken);
            }

            return new ProcedureNode(startNode, endNode, children);
        }
    }
}