namespace NightyCode.PostScript
{
    #region Namespace Imports

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    #endregion


    public class PostScriptReader : IDisposable
    {
        #region Constants and Fields

        private readonly TextReader _reader;
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private int _currentCharacter;
        private int _currentColumn = -1;
        private int _currentLine = -1;
        private IEnumerator<Token> _tokenEnumerator;
        private string _whitespaceCharacters = string.Empty;

        #endregion


        #region Constructors and Destructors

        public PostScriptReader(Stream stream)
        {
            _reader = new StreamReader(stream);
        }


        public PostScriptReader(string postScript)
        {
            _reader = new StringReader(postScript);
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


        private void OnNextCharacter()
        {
            if (_currentLine == -1 && _currentColumn == -1)
            {
                _currentLine = 0;
                _currentColumn = 0;
            }
            else
            {
                _currentColumn++;
            }

            if (_currentCharacter == '\n')
            {
                _currentLine++;
                _currentColumn = 0;
            }
        }


        private int PeekCharacter()
        {
            int character = _reader.Peek();
            return character == '\r' ? '\n' : character;
        }


        private Token ReadAscii85String()
        {
            _stringBuilder.Clear();

            _stringBuilder.Append("<~");

            var endOfString = false;

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

                if (endOfString)
                {
                    if (character != '>')
                    {
                        throw new SyntaxErrorException(
                            string.Format("Unexpected character sequence '~{0}' in ASCII85 encoded string.", character));
                    }

                    return CreateToken(TokenType.String, _stringBuilder.ToString(), line, column);
                }

                if (character == '~')
                {
                    endOfString = true;
                }
            }

            throw new SyntaxErrorException("Unexpected end of stream while reading ASCII85 encoded string.");
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

            return CreateToken(TokenType.Comment, text, line, column);
        }


        private Token ReadHexString()
        {
            _stringBuilder.Clear();

            _stringBuilder.Append("<");

            int line = _currentLine;
            int column = _currentColumn;

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

            throw new SyntaxErrorException("Unexpected end of stream while reading hex encoded string.");
        }


        private string ReadLine(out int line, out int startColumn)
        {
            line = -1;
            startColumn = -1;

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
            int column = _currentColumn + 1;

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
                column = _currentColumn + 1;
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
            else if (double.TryParse(value, out real))
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
            int column = _currentColumn + 1;

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
                throw new SyntaxErrorException("Unexpected end of stream while reading string.");
            }

            return CreateToken(TokenType.String, _stringBuilder.ToString(), line, column);
        }


        private IEnumerable<Token> ReadTokens()
        {
            while (true)
            {
                int character = PeekCharacter();

                if (character == -1)
                {
                    break;
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

                            yield return CreateToken(TokenType.DictionaryStart, "<<", _currentLine, _currentColumn - 1);
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
                            throw new SyntaxErrorException(
                                string.Format("Unexpected charcater sequence '<{0}'.", character));
                        }

                        // Read the second > character from stream
                        ReadCharacter();

                        yield return CreateToken(TokenType.DictionaryEnd, ">>", _currentLine, _currentColumn - 1);
                        break;

                    case '[':
                        // Read the [ character from stream
                        ReadCharacter();

                        yield return CreateToken(TokenType.ArrayStart, "[", _currentLine, _currentColumn);
                        break;

                    case ']':
                        // Read the ] character from stream
                        ReadCharacter();

                        yield return CreateToken(TokenType.ArrayEnd, "]", _currentLine, _currentColumn);
                        break;

                    case '{':
                        // Read the { character from stream
                        ReadCharacter();

                        yield return CreateToken(TokenType.ProcedureStart, "{", _currentLine, _currentColumn);
                        break;

                    case '}':
                        // Read the { character from stream
                        ReadCharacter();

                        yield return CreateToken(TokenType.ProcedureEnd, "}", _currentLine, _currentColumn);
                        break;

                    default:
                        yield return ReadLiteral();
                        break;
                }
            }
        }

        #endregion
    }
}