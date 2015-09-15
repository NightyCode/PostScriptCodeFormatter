namespace NightyCode.PostScript
{
    #region Namespace Imports

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    #endregion


    public class PostScriptReader : IDisposable
    {
        #region Constants and Fields

        private readonly TextReader _reader;
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private int _currentCharacter;
        private int _currentColumn;
        private int _currentLine;
        private List<EmbeddedStream> _embeddedStreams;
        private readonly List<string> _embeddedStreamStartTokens = new List<string> { "doNimage", "beginimage", "Y" };
        private EmbeddedStream? _nextEmbeddedStream;
        private IEnumerator<Token> _tokenEnumerator;
        private string _whitespaceCharacters = string.Empty;

        #endregion


        #region Constructors and Destructors

        public PostScriptReader(Stream stream)
        {
            _reader = new StreamReader(stream);
        }

        #endregion


        #region Properties

        public bool EndOfStream
        {
            get;
            private set;
        }

        #endregion


        #region Public Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public Token Read()
        {
            if (_tokenEnumerator == null)
            {
                _tokenEnumerator = ReadTokens().GetEnumerator();
            }

            EndOfStream = !_tokenEnumerator.MoveNext();

            return EndOfStream ? null : _tokenEnumerator.Current;
        }


        public IEnumerable<Token> ReadToEnd()
        {
            while (true)
            {
                Token token = Read();

                if (token == null)
                {
                    yield break;
                }

                yield return token;
            }
        }

        #endregion


        #region Methods

        private Token CreateToken(TokenType tokenType, string text, int line, int column)
        {
            int lastNewLine = _whitespaceCharacters.LastIndexOf('\n');

            if (lastNewLine >= 0)
            {
                _whitespaceCharacters = _whitespaceCharacters.Substring(lastNewLine + 1);
            }

            var token = new Token(tokenType, text, line, column, _whitespaceCharacters);
            _whitespaceCharacters = string.Empty;

            return token;
        }


        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _reader.Dispose();
        }


        private EmbeddedStream? GetNextEmbeddedStream()
        {
            if (_embeddedStreams == null)
            {
                return null;
            }

            if (_nextEmbeddedStream == null)
            {
                return null;
            }

            int currentStreamIndex = _embeddedStreams.IndexOf(_nextEmbeddedStream.Value);

            if (currentStreamIndex < _embeddedStreams.Count - 1)
            {
                return _embeddedStreams[currentStreamIndex + 1];
            }

            return null;
        }


        private void OnNextCharacter()
        {
            if (_currentLine == 0 && _currentColumn == 0)
            {
                _currentLine = 1;
                _currentColumn = 1;
            }
            else
            {
                _currentColumn++;
            }

            if (_currentCharacter == '\n')
            {
                _currentLine++;
                _currentColumn = 1;
            }
        }


        private EmbeddedStream ParseEmbeddedStreamLocation(string location)
        {
            location = location.Trim('[', ']');

            string[] split = location.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            return new EmbeddedStream
            {
                Start = new TextPosition { Line = int.Parse(split[0]), Column = int.Parse(split[1]) },
                End = new TextPosition { Line = int.Parse(split[2]), Column = int.Parse(split[3]) }
            };
        }


        private void ParseEmbeddedStreamsCommment(string embeddedCommentsString)
        {
            try
            {
                string[] locations = embeddedCommentsString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                var embeddedStreams = new List<EmbeddedStream>();

                foreach (string location in locations)
                {
                    embeddedStreams.Add(ParseEmbeddedStreamLocation(location));
                }

                _embeddedStreams = embeddedStreams.OrderBy(s => s.Start.Line).ToList();

                if (_embeddedStreams.Count > 0)
                {
                    _nextEmbeddedStream = _embeddedStreams.First();
                }
            }
            catch (Exception e)
            {
                throw new PostScriptReaderException("Failed to parse embedded streams comment.", e);
            }
        }


        private int PeekCharacter(out int line, out int column)
        {
            int character = PeekCharacter();
            line = _currentLine;
            column = _currentColumn + 1;

            if (_currentCharacter != '\n')
            {
                return character;
            }

            line++;
            column = 1;

            return character;
        }


        private int PeekCharacter()
        {
            int character = _reader.Peek();
            return character == '\r' ? '\n' : character;
        }


        private Token ReadAscii85String(bool isEmbeddedStream = false)
        {
            _stringBuilder.Clear();

            var endOfString = false;

            int line;
            int column;

            if (!isEmbeddedStream)
            {
                _stringBuilder.Append("<~");
                line = _currentLine;
                column = _currentColumn - 2;
            }
            else
            {
                PeekCharacter(out line, out column);
            }

            while (true)
            {
                int character = ReadCharacter();

                if (character == -1)
                {
                    break;
                }

                _stringBuilder.Append((char)character);

                if (endOfString)
                {
                    if (character != '>')
                    {
                        throw new PostScriptReaderException(
                            string.Format("Unexpected character sequence '~{0}' in ASCII85 encoded string.", character));
                    }

                    return CreateToken(
                        isEmbeddedStream ? TokenType.RawData : TokenType.String,
                        _stringBuilder.ToString(),
                        line,
                        column);
                }

                if (character == '~')
                {
                    endOfString = true;
                }
            }

            throw new PostScriptReaderException("Unexpected end of stream while reading ASCII85 encoded string.");
        }


        private int ReadCharacter()
        {
            if (_currentCharacter == -1)
            {
                return _currentCharacter;
            }

            OnNextCharacter();

            _currentCharacter = _reader.Read();

            // Form Feed (FF)
            if (_currentCharacter == 0x0c)
            {
                _currentCharacter = '\n';
            }
            else if (_currentCharacter == '\r')
            {
                _currentCharacter = '\n';

                if (_reader.Peek() == '\n')
                {
                    _reader.Read();
                }
            }

            return _currentCharacter;
        }


        private Token ReadComment()
        {
            int line;
            int column;
            string text = ReadLine(out line, out column);

            var embeddedStreamsComment = "%#EmbeddedStreams:";

            if (text.StartsWith(embeddedStreamsComment, StringComparison.Ordinal))
            {
                ParseEmbeddedStreamsCommment(text.Substring(embeddedStreamsComment.Length));
            }

            return CreateToken(TokenType.Comment, text, line, column);
        }


        private Token ReadHexString()
        {
            _stringBuilder.Clear();

            _stringBuilder.Append("<");

            int line = _currentLine;
            int column = _currentColumn - 1;

            while (true)
            {
                int character = ReadCharacter();

                if (character == -1)
                {
                    break;
                }

                _stringBuilder.Append((char)character);

                if (character == '>')
                {
                    return CreateToken(TokenType.String, _stringBuilder.ToString(), line, column);
                }
            }

            throw new PostScriptReaderException("Unexpected end of stream while reading hex encoded string.");
        }


        private string ReadLine(out int line, out int startColumn)
        {
            line = 0;
            startColumn = 0;

            if (_currentCharacter == -1)
            {
                return string.Empty;
            }

            OnNextCharacter();

            line = _currentLine;
            startColumn = _currentColumn;

            string text = _reader.ReadLine();

            _currentColumn += text.Length;
            _currentCharacter = '\n';

            return text;
        }


        private Token ReadLiteral()
        {
            int line = _currentLine;
            int column = _currentColumn;

            var stop = false;
            var isLiteralName = false;

            _stringBuilder.Clear();

            do
            {
                int character = PeekCharacter();

                switch (character)
                {
                    case '\n':
                    case '\t':
                    case ' ':
                    case 0:
                    case -1:
                        stop = true;
                        break;

                    case '(':
                    case ')':
                    case '{':
                    case '}':
                    case '<':
                    case '>':
                    case '[':
                    case ']':
                    case '%':
                        stop = true;
                        break;

                    case '/':
                        if (_stringBuilder.Length == 0)
                        {
                            _stringBuilder.Append((char)character);
                            ReadCharacter();

                            isLiteralName = true;
                        }
                        else
                        {
                            stop = true;
                        }
                        break;

                    default:
                        _stringBuilder.Append((char)character);
                        ReadCharacter();
                        break;
                }

                if (_stringBuilder.Length != 1)
                {
                    continue;
                }

                line = _currentLine;
                column = _currentColumn;
            }
            while (!stop);

            string value = _stringBuilder.ToString();

            if (isLiteralName)
            {
                return CreateToken(TokenType.LiteralName, value, line, column);
            }

            var tokenType = TokenType.ExecutableName;

            int integer;
            double real;

            if (int.TryParse(value, out integer))
            {
                tokenType = TokenType.IntegerNumber;
            }
            else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out real))
            {
                tokenType = TokenType.RealNumber;
            }
            else if (value.Contains("#"))
            {
                string[] strings = value.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

                int radix;
                if (strings.Length == 2 && int.TryParse(strings[0], out radix))
                {
                    try
                    {
                        Radix.Decode(strings[1], radix, out real);
                        tokenType = TokenType.IntegerNumber;
                    }
                    catch
                    {
                        tokenType = TokenType.ExecutableName;
                    }
                }
            }

            return CreateToken(tokenType, value, line, column);
        }


        private Token ReadString()
        {
            _stringBuilder.Clear();

            // read starting parenthesis.
            _stringBuilder.Append((char)ReadCharacter());

            int line = _currentLine;
            int column = _currentColumn;

            int previousCharacter = -1;
            var openParenthesesCount = 0;

            while (true)
            {
                int character = ReadCharacter();

                if (character == -1)
                {
                    break;
                }

                _stringBuilder.Append((char)character);

                if (previousCharacter != '\\')
                {
                    if (character == '(')
                    {
                        openParenthesesCount++;
                    }

                    if (character == ')')
                    {
                        if (openParenthesesCount == 0)
                        {
                            break;
                        }

                        openParenthesesCount--;
                    }
                }

                if (character != '\n')
                {
                    previousCharacter = character;
                }
            }

            if (openParenthesesCount > 0)
            {
                throw new PostScriptReaderException("Unexpected end of stream while reading string.");
            }

            return CreateToken(TokenType.String, _stringBuilder.ToString(), line, column);
        }


        private string ReadTo(TextPosition textPosition)
        {
            var stringBuilder = new StringBuilder();

            while (_currentLine < textPosition.Line - 1)
            {
                int line;
                int column;
                stringBuilder.AppendLine(ReadLine(out line, out column));
            }

            do
            {
                stringBuilder.Append((char)ReadCharacter());
            }
            while (_currentColumn != textPosition.Column);

            return stringBuilder.ToString();
        }


        private IEnumerable<Token> ReadTokens()
        {
            while (true)
            {
                int nextCharacterLine;
                int nextCharacterColumn;
                int character = PeekCharacter(out nextCharacterLine, out nextCharacterColumn);

                if (character == -1)
                {
                    break;
                }

                if (_nextEmbeddedStream != null)
                {
                    EmbeddedStream stream = _nextEmbeddedStream.Value;

                    if (stream.Start.Line == nextCharacterLine && stream.Start.Column == nextCharacterColumn)
                    {
                        _nextEmbeddedStream = GetNextEmbeddedStream();

                        int line = nextCharacterLine;
                        int column = nextCharacterColumn;
                        yield return CreateToken(TokenType.RawData, ReadTo(stream.End), line, column);

                        continue;
                    }
                }

                // TODO: also check resource DSC.
                if (_tokenEnumerator.Current != null
                    && _embeddedStreamStartTokens.Contains(_tokenEnumerator.Current.Text))
                {
                    SkipWhitespaceCharacters();

                    yield return ReadAscii85String(true);
                }

                switch (character)
                {
                    case '%':
                        yield return ReadComment();
                        break;

                    case '\n':
                    case '\t':
                    case ' ':
                    case 0:
                        // go forward
                        _whitespaceCharacters += (char)ReadCharacter();
                        break;

                    case '(':
                        yield return ReadString();
                        break;

                    case '<':
                        // Read the < character from stream
                        ReadCharacter();

                        // Peek next character
                        character = PeekCharacter();

                        if (character == '~')
                        {
                            // Read the second ~ character from stream
                            ReadCharacter();

                            yield return ReadAscii85String();
                        }
                        else if (character == '<')
                        {
                            // Read the second < character from stream
                            ReadCharacter();

                            yield return CreateToken(TokenType.DictionaryStart, "<<", _currentLine, _currentColumn - 2);
                        }
                        else
                        {
                            yield return ReadHexString();
                        }

                        break;

                    case '>':
                        // Read the > character from stream
                        ReadCharacter();

                        // Peek next character
                        character = PeekCharacter();

                        if (character != '>')
                        {
                            throw new PostScriptReaderException(
                                string.Format("Unexpected charcater sequence '<{0}'.", character));
                        }

                        // Read the second > character from stream
                        ReadCharacter();

                        yield return CreateToken(TokenType.DictionaryEnd, ">>", _currentLine, _currentColumn - 2);
                        break;

                    case '[':
                        // Read the [ character from stream
                        ReadCharacter();

                        yield return CreateToken(TokenType.ArrayStart, "[", _currentLine, _currentColumn - 1);
                        break;

                    case ']':
                        // Read the ] character from stream
                        ReadCharacter();

                        yield return CreateToken(TokenType.ArrayEnd, "]", _currentLine, _currentColumn - 1);
                        break;

                    case '{':
                        // Read the { character from stream
                        ReadCharacter();

                        yield return CreateToken(TokenType.ProcedureStart, "{", _currentLine, _currentColumn - 1);
                        break;

                    case '}':
                        // Read the { character from stream
                        ReadCharacter();

                        yield return CreateToken(TokenType.ProcedureEnd, "}", _currentLine, _currentColumn - 1);
                        break;

                    default:
                        yield return ReadLiteral();
                        break;
                }
            }
        }


        private void SkipWhitespaceCharacters()
        {
            while (true)
            {
                int nextCharacterLine;
                int nextCharacterColumn;
                int character = PeekCharacter(out nextCharacterLine, out nextCharacterColumn);

                if (character == -1)
                {
                    break;
                }

                if (char.IsWhiteSpace((char)character) || character == 0)
                {
                    _whitespaceCharacters += (char)ReadCharacter();
                }
                else
                {
                    break;
                }
            }
        }

        #endregion


        private struct EmbeddedStream
        {
            #region Properties

            public TextPosition End
            {
                get;
                set;
            }

            public TextPosition Start
            {
                get;
                set;
            }

            #endregion
        }


        private struct TextPosition
        {
            #region Properties

            public int Column
            {
                get;
                set;
            }

            public int Line
            {
                get;
                set;
            }

            #endregion
        }
    }
}