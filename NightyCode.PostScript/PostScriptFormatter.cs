namespace NightyCode.PostScript
{
    #region Namespace Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using NightyCode.PostScript.Syntax;

    using PCLStorage;

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

        public bool AddTracing
        {
            get;
            set;
        }

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

        public async Task<string> Format(string postScript)
        {
            var reader = new PostScriptReader(postScript);

            List<Token> tokens = reader.ReadToEnd().ToList();

            ScriptNode scriptNode = tokens.Parse();

            if (RemoveOperatorAliases)
            {
                scriptNode.InlineDefinitions();
            }

            if (AddTracing)
            {
                await InsertTracingCode(scriptNode).ConfigureAwait(false);
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

        private static void AddTracePoint(
            SyntaxBlock parent,
            int index,
            LiteralNode forNode,
            string traceName,
            ICollection<string> speciallyLoggedOperators)
        {
            Token token = forNode.Token;
            int line = token.Line;
            int column = token.Column;

            if (string.IsNullOrEmpty(token.WhitespaceBefore))
            {
                token.WhitespaceBefore = " ";
            }

            var logFunction = "#Log";

            if (speciallyLoggedOperators.Contains(traceName))
            {
                logFunction += "_" + traceName;
            }

            parent.InsertNode(index, new NameNode(new Token(TokenType.ExecutableName, logFunction, line, column, " ")));

            string logText = string.Format("([{0},{1}] {2})", line, column, traceName);
            parent.InsertNode(index, new StringNode(new Token(TokenType.String, logText, line, column, string.Empty)));
        }


        private static void AddTracePoint(
            SyntaxBlock parent,
            SyntaxNode beforeNode,
            NameNode forNode,
            ICollection<string> speciallyLoggedOperators)
        {
            string traceName;
            forNode.ResolveOperatorName(out traceName);

            AddTracePoint(parent, beforeNode, forNode, traceName, speciallyLoggedOperators);
        }


        private static void AddTracePoint(
            SyntaxBlock parent,
            SyntaxNode beforeNode,
            LiteralNode forNode,
            string traceName,
            ICollection<string> speciallyLoggedOperators)
        {
            AddTracePoint(parent, parent.IndexOfNode(beforeNode), forNode, traceName, speciallyLoggedOperators);
        }


        private static void AddTracePoints(SyntaxBlock syntaxBlock, List<string> speciallyLoggedOperators)
        {
            bool isProcedure = syntaxBlock is ProcedureNode;
            var nameNode = syntaxBlock.StartNode as NameNode;

            if (!isProcedure && nameNode != null && nameNode.IsExecutable)
            {
                AddTracePoint(syntaxBlock.Parent, syntaxBlock, nameNode, speciallyLoggedOperators);
            }

            for (var i = 0; i < syntaxBlock.Nodes.Count; i++)
            {
                SyntaxNode node = syntaxBlock.Nodes[i];

                nameNode = node as NameNode;

                if (nameNode != null && nameNode.IsExecutable)
                {
                    AddTracePoint(syntaxBlock, nameNode, nameNode, speciallyLoggedOperators);
                }

                var block = node as SyntaxBlock;

                if (block != null)
                {
                    AddTracePoints(block, speciallyLoggedOperators);
                }

                if (node != syntaxBlock.Nodes[i])
                {
                    i += 2;
                }
            }

            nameNode = syntaxBlock.EndNode as NameNode;

            if (isProcedure || nameNode == null || !nameNode.IsExecutable)
            {
                return;
            }

            string name;

            var operatorNode = syntaxBlock as OperatorNode;

            if (operatorNode != null)
            {
                name = operatorNode.OperatorName;
            }
            else
            {
                nameNode.ResolveOperatorName(out name);
            }

            AddTracePoint(syntaxBlock, syntaxBlock.Nodes.Count, nameNode, name, speciallyLoggedOperators);
        }


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


        private void InsertCodeBlock(ScriptNode scriptNode, ScriptNode codeToInsert)
        {
            SyntaxBlock tracingBlock = new RegionBlock();
            tracingBlock.AddNodeRange(codeToInsert.Nodes);

            CommentNode prologStart =
                scriptNode.Descendants().OfType<CommentNode>().FirstOrDefault(c => c.Text == "%%BeginProlog");

            SyntaxBlock parent = scriptNode;
            var insertIndex = 0;

            if (prologStart != null)
            {
                parent = prologStart.Parent;
                Debug.Assert(parent != null, "parent != null");
                insertIndex = parent.Nodes.ToList().IndexOf(prologStart);

                if (insertIndex == -1)
                {
                    insertIndex = 0;
                }
                else
                {
                    insertIndex++;
                }
            }

            parent.InsertNode(insertIndex, tracingBlock);
        }


        private async Task InsertTracingCode(ScriptNode scriptNode)
        {
            ScriptNode tracingCode = await LoadTracingCode().ConfigureAwait(false);

            List<string> speciallyLoggedOperators =
                tracingCode.Defines.Where(p => p.Key.StartsWith("#Log_", StringComparison.Ordinal))
                    .Select(p => p.Key.Substring(5))
                    .ToList();

            AddTracePoints(scriptNode, speciallyLoggedOperators);
            InsertCodeBlock(scriptNode, tracingCode);
        }


        private async Task<ScriptNode> LoadTracingCode()
        {
            // TODO: get source file from parameters.
            IFile traceCodeFile = await FileSystem.Current.GetFileFromPathAsync("TracingCode.ps").ConfigureAwait(false);

            Stream stream = await traceCodeFile.OpenAsync(FileAccess.Read).ConfigureAwait(false);

            using (var streamReader = new StreamReader(stream))
            {
                string tracingCode = streamReader.ReadToEnd();

                var reader = new PostScriptReader(tracingCode);

                List<Token> tokens = reader.ReadToEnd().ToList();

                return tokens.Parse();
            }
        }


        private void WriteUnformattedCode(IEnumerable<Token> tokens, StringBuilder result)
        {
            Token previousToken = null;

            foreach (Token token in tokens)
            {
                if (previousToken != null)
                {
                    int previousTokenEndLine = previousToken.Line + previousToken.NumTokenLines;

                    if (previousTokenEndLine < token.Line)
                    {
                        for (var i = 0; i < token.Line - previousTokenEndLine; i++)
                        {
                            result.AppendLine();
                        }
                    }
                    else if (previousTokenEndLine > token.Line)
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