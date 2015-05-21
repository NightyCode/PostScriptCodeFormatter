namespace NightyCode.PostScript.Syntax
{
    #region Namespace Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using JetBrains.Annotations;

    #endregion


    public static partial class SyntaxTreeBuilder
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
                    var startNode = (LiteralNode)node.Nodes[startIndex];
                    var endNode = (LiteralNode)node.Nodes[endIndex];

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


        public static void InlineDefinitions(this ScriptNode scriptNode)
        {
            InlineDefinitions((SyntaxBlock)scriptNode);

            foreach (KeyValuePair<string, List<string>> alias in scriptNode.OperatorAliases)
            {
                if (alias.Value.Count != 1)
                {
                    continue;
                }

                List<OperatorNode> defines = scriptNode.Defines[alias.Key];

                foreach (OperatorNode define in defines)
                {
                    Debug.Assert(define.Parent != null, "define.Parent != null");
                    define.Parent.RemoveNode(define);
                }
            }
        }


        public static ScriptNode Parse(this IEnumerable<Token> tokens)
        {
            IEnumerator<Token> enumerator = tokens.GetEnumerator();

            var scriptNode = new ScriptNode();

            while (enumerator.MoveNext())
            {
                scriptNode.AddNode(CreateNode(enumerator, enumerator.Current));
            }

            scriptNode.Analyze();

            foreach (var alias in scriptNode.OperatorAliases.ToList())
            {
                if (scriptNode.Defines[alias.Key].Count == alias.Value.Count)
                {
                    continue;
                }

                scriptNode.OperatorAliases.Remove(alias.Key);
            }

            return scriptNode;
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

                var nameNode = currentNode as NameNode;

                if (nameNode == null || !nameNode.IsExecutable)
                {
                    continue;
                }

                OperatorNode operatorNode = null;

                string operatorName = nameNode.Text;
                ScriptNode scriptNode = tree.GetScriptNode();

                ResolveOperatorName(ref operatorName, scriptNode);

                if (operatorName == "load")
                {
                    operatorNode = ProcessLoadOperator(tree.Nodes, index, nameNode);
                }
                else if (operatorName == "bind")
                {
                    operatorNode = ProcessBindOperator(tree.Nodes, index, nameNode);
                }
                else if (operatorName == "def")
                {
                    operatorNode = ProcessDefOperator(tree.Nodes, index, nameNode);

                    if (operatorNode != null)
                    {
                        scriptNode.AddDefinition(operatorNode);

                        var loadOperator = GetDefinitionValue(operatorNode) as OperatorNode;

                        if (loadOperator != null && loadOperator.OperatorName == "load")
                        {
                            operatorName = loadOperator.Nodes[0].Text.Substring(1);

                            if (ResolveOperatorName(ref operatorName, scriptNode))
                            {
                                scriptNode.AddOperatorAlias(operatorNode, operatorName);
                            }
                        }
                    }
                }

                if (operatorNode == null)
                {
                    continue;
                }

                index = index - operatorNode.Nodes.Count;
                tree.RemoveNodesRange(index, operatorNode.Nodes.Count + 1);
                tree.InsertNode(index, operatorNode);
            }
        }


        private static SyntaxNode BuildProcedureNode(IEnumerator<Token> enumerator, Token startToken)
        {
            var procedureNode = new ProcedureNode { StartNode = new NameNode(startToken) };

            while (true)
            {
                if (!enumerator.MoveNext())
                {
                    break;
                }

                Token token = enumerator.Current;

                if (token.Type == TokenType.ProcedureEnd)
                {
                    procedureNode.EndNode = new NameNode(token);
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

                case TokenType.IntegerNumber:
                    return new IntegerNumberNode(token);

                case TokenType.RealNumber:
                    return new RealNumberNode(token);

                default:
                    return new NameNode(token);
            }
        }


        private static List<OperatorNode> GetDefines(this SyntaxNode node, string key)
        {
            var defines = new List<OperatorNode>();

            ScriptNode procedureNode = node.GetScriptNode();

            if (procedureNode == null)
            {
                return defines;
            }

            List<OperatorNode> procedureDefines;
            if (procedureNode.Defines.TryGetValue(key, out procedureDefines))
            {
                defines.AddRange(procedureDefines);
            }

            return defines;
        }


        private static SyntaxNode GetDefinitionValue(OperatorNode defOperatorNode)
        {
            SyntaxNode valueNode = defOperatorNode.Nodes[1];

            var operatorNode = valueNode as OperatorNode;

            if (operatorNode == null)
            {
                return valueNode;
            }

            if (operatorNode.OperatorName == "bind")
            {
                return operatorNode.Nodes[0];
            }

            if (operatorNode.OperatorName == "load")
            {
                return operatorNode;
            }

            return valueNode;
        }


        private static List<string> GetOperatorAliases(this SyntaxNode node, string key)
        {
            var aliases = new List<string>();

            ScriptNode procedureNode = node.GetScriptNode();

            if (procedureNode == null)
            {
                return aliases;
            }

            List<string> procedureAliases;
            if (procedureNode.OperatorAliases.TryGetValue(key, out procedureAliases))
            {
                aliases.AddRange(procedureAliases);
            }

            return aliases;
        }


        private static ScriptNode GetScriptNode(this SyntaxNode node)
        {
            while (node != null)
            {
                if (node is ScriptNode)
                {
                    break;
                }

                node = node.Parent;
            }

            return (ScriptNode)node;
        }


        private static void InlineDefinitions(SyntaxBlock parentNode)
        {
            var operatorNode = parentNode as OperatorNode;

            if (operatorNode != null && operatorNode.OperatorName != operatorNode.OperatorAlias)
            {
                Debug.Assert(operatorNode.EndNode != null, "operatorNode.EndNode != null");
                operatorNode.EndNode.Token.Text = operatorNode.OperatorName;
            }

            for (var i = 0; i < parentNode.Nodes.Count; i++)
            {
                SyntaxNode node = parentNode.Nodes[i];

                var syntaxBlock = node as SyntaxBlock;

                if (syntaxBlock != null)
                {
                    InlineDefinitions(syntaxBlock);
                    continue;
                }

                var nameNode = node as NameNode;

                if (nameNode == null)
                {
                    continue;
                }

                string name = nameNode.Text;

                if (ResolveOperatorName(ref name, parentNode.GetScriptNode()))
                {
                    nameNode.Token.Text = name;
                }
            }
        }


        private static OperatorNode ProcessBindOperator(
            IReadOnlyList<SyntaxNode> nodes,
            int index,
            LiteralNode currentNode)
        {
            SyntaxNode procedureNode = nodes.TryGetNode<ProcedureNode>(index - 1);
            SyntaxNode loadOperatorNode = nodes.TryGetOperatorNode(index - 1, "load");

            if (procedureNode == null && loadOperatorNode == null)
            {
                return null;
            }

            var operatorNode = new OperatorNode(currentNode, "bind");
            operatorNode.AddNode(procedureNode ?? loadOperatorNode);

            return operatorNode;
        }


        private static OperatorNode ProcessDefOperator(
            IReadOnlyList<SyntaxNode> nodes,
            int index,
            LiteralNode currentNode)
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

            var keyNode = nodes.TryGetNode<NameNode>(index - 2);

            if (keyNode == null || keyNode.IsExecutable || valueNode == null)
            {
                return null;
            }

            var defOperatorNode = new OperatorNode(currentNode, "def");
            defOperatorNode.AddNode(keyNode);
            defOperatorNode.AddNode(valueNode);

            return defOperatorNode;
        }


        private static OperatorNode ProcessLoadOperator(
            IReadOnlyList<SyntaxNode> nodes,
            int index,
            LiteralNode currentNode)
        {
            var nameNode = nodes.TryGetNode<NameNode>(index - 1);

            if (nameNode == null || nameNode.IsExecutable)
            {
                return null;
            }

            var operatorNode = new OperatorNode(currentNode, "load");
            operatorNode.AddNode(nameNode);

            return operatorNode;
        }


        private static bool ResolveOperatorName(ref string operatorName, ProcedureNode parentProcedure)
        {
            var isLiteralName = false;
            string name = operatorName;

            if (operatorName.StartsWith("/", StringComparison.Ordinal))
            {
                isLiteralName = true;
                name = operatorName.Substring(1);
            }

            if (_operatorNames.Contains(name))
            {
                return true;
            }

            List<string> aliases = GetOperatorAliases(parentProcedure, name).Distinct().ToList();

            if (aliases.Count != 1)
            {
                return false;
            }

            operatorName = aliases[0];

            if (isLiteralName)
            {
                operatorName = "/" + operatorName;
            }

            return true;
        }


        private static NameNode ToExecutable(this NameNode nameNode)
        {
            if (nameNode.IsExecutable)
            {
                return nameNode;
            }

            Token token = nameNode.Token;

            token = new Token(
                TokenType.ExecutableName,
                token.Text.Substring(1),
                token.Line,
                token.Column,
                token.WhitespaceBefore);

            return new NameNode(token);
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