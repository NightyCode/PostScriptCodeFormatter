namespace NightyCode.PostScript
{
    #region Namespace Imports

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using NightyCode.PostScript.Syntax;

    #endregion


    #region Namespace Imports

    #endregion


    public class PostScriptFormatter
    {
        #region Constants and Fields

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

        private static readonly List<string> _breakBeforeLiterals = new List<string>
        {
            "begin",
            "end",
            "save",
            "restore",
            "gsave",
            "grestore"
        };

        #endregion


        #region Constructors and Destructors

        public PostScriptFormatter()
        {
            FormatCode = true;
            RemoveOperatorAliases = true;
            MaxLineLength = 125;
        }

        #endregion


        #region Properties

        public bool FormatCode
        {
            get;
            set;
        }

        public int MaxLineLength
        {
            get;
            set;
        }

        public bool RemoveOperatorAliases
        {
            get;
            set;
        }

        #endregion


        #region Public Methods

        public string Format(string postScript)
        {
            var reader = new PostScriptReader(postScript);

            List<Token> tokens = reader.ReadToEnd().ToList();

            SyntaxBlock tree = tokens.Parse();

            if (RemoveOperatorAliases)
            {
                RemoveAliases(tokens);
            }

            var result = new StringBuilder();

            if (FormatCode)
            {
                tree.Group("%%BeginProlog", "%%EndProlog");
                tree.Group("%%BeginSetup", "%%EndSetup");
                tree.Group("%%BeginPageSetup", "%%EndPageSetup");
                tree.Group("%%BeginDefaults", "%%EndDefaults");

                tree.Group("begin", "end");
                tree.Group("save", "restore");
                tree.Group("gsave", "grestore");

                Format(0, result, tree);
            }
            else
            {
                WriteUnformattedCode(tree.ToTokens(), result);
            }

            return result.ToString();
        }

        #endregion


        #region Methods

        private bool BreakLineAfter(SyntaxNode node)
        {
            var commentNode = node as CommentNode;
            if (commentNode != null)
            {
                return true;
            }

            var literalNode = node as LiteralNode;
            if (literalNode != null && _breakAfterLiterals.Contains(node.Text))
            {
                return true;
            }

            return false;
        }


        private bool BreakLineBefore(SyntaxNode node)
        {
            var commentNode = node as CommentNode;
            if (commentNode != null && node.Text.StartsWith("%%", StringComparison.Ordinal))
            {
                return true;
            }

            var literalNode = node as LiteralNode;
            if (literalNode != null && _breakBeforeLiterals.Contains(node.Text))
            {
                return true;
            }

            return false;
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

            for (var index = 0; index < tokens.Count; index++)
            {
                Token token = tokens[index];

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


        private void Format(int level, StringBuilder result, SyntaxBlock tree)
        {
            var codeLine = new StringBuilder();

            Action appendLine = () =>
            {
                string[] lines = codeLine.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                var indent = new string(' ', level * 2);

                foreach (string line in lines)
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

            foreach (SyntaxNode node in tree.Nodes)
            {
                string text = node.Text;
                int currentLineLength = codeLine.Length;

                var blockNode = node as SyntaxBlock;

                if (blockNode != null && (blockNode is ProcedureNode || blockNode is RegionBlock))
                {
                    bool isRegion = blockNode is RegionBlock;

                    if (isRegion || blockNode.Nodes.Count > 2
                        || blockNode.Nodes.Any(n => n is ProcedureNode || n is RegionBlock)
                        || (currentLineLength + text.Length + 1) >= MaxLineLength)
                    {
                        if (blockNode.StartNode != null)
                        {
                            appendLine();
                            append(blockNode.StartNode.Text);
                            appendLine();
                        }

                        Format(level + 1, result, blockNode);

                        if (blockNode.EndNode != null)
                        {
                            appendLine();
                            append(blockNode.EndNode.Text);
                            appendLine();
                        }

                        continue;
                    }
                }

                if (BreakLineBefore(node) || (currentLineLength + text.Length + 1) >= MaxLineLength)
                {
                    appendLine();
                }

                append(text);

                if (BreakLineAfter(node) || currentLineLength >= MaxLineLength)
                {
                    appendLine();
                }
            }

            appendLine();
        }


        private void RemoveAliases(List<Token> tokens)
        {
            while (true)
            {
                Tuple<string, string> alias = FindAlias(tokens);

                if (alias == null)
                {
                    break;
                }

                for (var index = 0; index < tokens.Count; index++)
                {
                    Token token = tokens[index];

                    if (token.Type != TokenType.Literal || token.Text != alias.Item1)
                    {
                        continue;
                    }

                    tokens[index] = new Token(
                        TokenType.Literal,
                        alias.Item2,
                        token.Line,
                        token.Column,
                        token.WhitespaceBefore);
                }
            }
        }


        private void WriteUnformattedCode(IEnumerable<Token> tokens, StringBuilder result)
        {
            Token previousToken = null;

            foreach (Token token in tokens)
            {
                if (previousToken != null && previousToken.Line < token.Line)
                {
                    for (var i = 0; i < token.Line - previousToken.Line; i++)
                    {
                        result.AppendLine();
                    }
                }

                result.Append(token.WhitespaceBefore + token.Text);

                previousToken = token;
            }
        }

        #endregion
    }
}