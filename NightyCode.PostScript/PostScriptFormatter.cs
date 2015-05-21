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

            ScriptNode scriptNode = tokens.Parse();

            if (RemoveOperatorAliases)
            {
                scriptNode.InlineDefinitions();
            }

            var result = new StringBuilder();

            if (FormatCode)
            {
                scriptNode.Group("%%BeginProlog", "%%EndProlog");
                scriptNode.Group("%%BeginSetup", "%%EndSetup");
                scriptNode.Group("%%BeginPageSetup", "%%EndPageSetup");
                scriptNode.Group("%%BeginDefaults", "%%EndDefaults");

                scriptNode.Group("begin", "end");
                scriptNode.Group("save", "restore");
                scriptNode.Group("gsave", "grestore");

                Format(0, result, scriptNode);
            }
            else
            {
                WriteUnformattedCode(scriptNode.ToTokens(), result);
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