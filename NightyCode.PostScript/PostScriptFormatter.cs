namespace NightyCode.PostScript
{
    #region Namespace Imports

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using NightyCode.PostScript.SyntaxTree;

    #endregion


    #region Namespace Imports

    #endregion


    public class PostScriptFormatter
    {
        private static readonly List<string> _breakBeforeLiterals = new List<string>
        {
            "begin",
            "end",
            "save",
            "restore",
            "gsave",
            "grestore"
        };

        private static readonly List<string> _breakAfterLiterals = new List<string>
        {
            "begin",
            "end",
            "def",
            "save",
            "restore",
            "gsave",
            "grestore"
        };


        public string Format(string postScript)
        {
            PostScriptReader reader = new PostScriptReader(postScript);

            List<Token> tokens = reader.ReadToEnd().ToList();

            UnfoldDefinitions(tokens);

            BlockNode tree = SyntaxTreeBuilder.Build(tokens);

            SyntaxTreeBuilder.GroupNodes(tree, "%%BeginProlog", "%%EndProlog");
            SyntaxTreeBuilder.GroupNodes(tree, "%%BeginSetup", "%%EndSetup");
            SyntaxTreeBuilder.GroupNodes(tree, "%%BeginPageSetup", "%%EndPageSetup");
            SyntaxTreeBuilder.GroupNodes(tree, "%%BeginDefaults", "%%EndDefaults");

            SyntaxTreeBuilder.GroupNodes(tree, "begin", "end");
            SyntaxTreeBuilder.GroupNodes(tree, "save", "restore");
            SyntaxTreeBuilder.GroupNodes(tree, "gsave", "grestore");

            StringBuilder result = new StringBuilder();
            Format(0, result, tree);

            return result.ToString();
        }


        private static void Format(int indentSize, StringBuilder result, BlockNode tree)
        {
            StringBuilder codeLine = new StringBuilder();

            Action appendLine = () =>
            {
                string[] lines = codeLine.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                string indent = new string(' ', indentSize * 2);

                foreach (var line in lines)
                {
                    result.AppendLine(indent + line);
                }

                codeLine.Clear();
            };

            Action<string> append = text =>
            {
                if (codeLine.Length > 0)
                {
                    codeLine.Append(' ');
                }

                codeLine.Append(text);
            };

            const int maxLineLength = 125;

            foreach (var node in tree.Children)
            {
                string text = node.Text;
                int currentLineLength = codeLine.Length;

                BlockNode blockNode = node as BlockNode;

                if (blockNode != null)
                {
                    bool isProcedure = blockNode is ProcedureNode;

                    if (!isProcedure || blockNode.Children.Count > 2
                        || (currentLineLength + text.Length + 1) >= maxLineLength)
                    {
                        appendLine();
                        append(blockNode.StartNode.Text);
                        appendLine();

                        Format(indentSize + 1, result, blockNode);
                        appendLine();
                        append(blockNode.EndNode.Text);
                        appendLine();

                        continue;
                    }
                }

                if (BreakLineBefore(node) || (currentLineLength + text.Length + 1) >= maxLineLength)
                {
                    appendLine();
                }

                append(text);

                if (BreakLineAfter(node) || currentLineLength >= maxLineLength)
                {
                    appendLine();
                }
            }

            appendLine();
        }


        private void UnfoldDefinitions(List<Token> tokens)
        {
            while (true)
            {
                Tuple<string, string> alias = FindAlias(tokens);

                if (alias == null)
                {
                    break;
                }

                for (int index = 0; index < tokens.Count; index++)
                {
                    var token = tokens[index];

                    if (token.Type != TokenType.Literal || token.Text != alias.Item1)
                    {
                        continue;
                    }

                    tokens[index] = new Token(TokenType.Literal, alias.Item2, token.Line, token.Column);
                }
            }
        }


        private Tuple<string, string> FindAlias(List<Token> tokens)
        {
            Func<int, bool> isLiteralName = i =>
            {
                Token token = tokens.ElementAtOrDefault(i);

                if (token == null || token.Type != TokenType.Literal)
                {
                    return false;
                }

                return token.Text.StartsWith("/", StringComparison.Ordinal);
            };

            Func<int, string, bool> isExecutableName = (i, name) =>
            {
                Token token = tokens.ElementAtOrDefault(i);

                if (token == null || token.Type != TokenType.Literal)
                {
                    return false;
                }

                return token.Text == name;
            };

            for (int index = 0; index < tokens.Count; index++)
            {
                var token = tokens[index];

                if (token.Type == TokenType.Comment && token.Text.StartsWith("%%EndProlog", StringComparison.Ordinal))
                {
                    break;
                }

                if (token.Type != TokenType.Literal || token.Text != "def")
                {
                    continue;
                }

                if (!isExecutableName(index - 1, "load") || !isLiteralName(index - 2) || !isLiteralName(index - 3))
                {
                    continue;
                }

                string operatorName = tokens[index - 2].Text.Substring(1);
                string aliasName = tokens[index - 3].Text.Substring(1);

                tokens.RemoveRange(index - 3, 4);

                return new Tuple<string, string>(aliasName, operatorName);
            }

            return null;
        }


        private static bool BreakLineBefore(INode node)
        {
            CommentNode commentNode = node as CommentNode;
            if (commentNode != null && node.Text.StartsWith("%%", StringComparison.Ordinal))
            {
                return true;
            }

            LiteralNode literalNode = node as LiteralNode;
            if (literalNode != null && _breakBeforeLiterals.Contains(node.Text))
            {
                return true;
            }

            return false;
        }


        private static bool BreakLineAfter(INode node)
        {
            CommentNode commentNode = node as CommentNode;
            if (commentNode != null)
            {
                return true;
            }

            LiteralNode literalNode = node as LiteralNode;
            if (literalNode != null && _breakAfterLiterals.Contains(node.Text))
            {
                return true;
            }

            return false;
        }
    }
}