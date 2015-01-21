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
        private readonly TextReader _reader;
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private IEnumerator<Token> _tokenEnumerator;
        private int _currentLine = -1;
        private int _currentColumn = -1;
        private int _currentCharacter;


        public PostScriptReader(Stream stream)
        {
            _reader = new StreamReader(stream);
        }


        public PostScriptReader(string postScript)
        {
            _reader = new StringReader(postScript);
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public bool EndOfStream
        {
            get;
            private set;
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


        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _reader.Dispose();
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
                        ReadCharacter();
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

                            yield return new Token(TokenType.DictionaryStart, "<<", _currentLine, _currentColumn);
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

                        yield return new Token(TokenType.DictionaryEnd, ">>", _currentLine, _currentColumn);
                        break;

                    case '[':
                        // Read the [ character from stream
                        ReadCharacter();

                        yield return new Token(TokenType.ArrayStart, "[", _currentLine, _currentColumn);
                        break;

                    case ']':
                        // Read the ] character from stream
                        ReadCharacter();

                        yield return new Token(TokenType.ArrayEnd, "]", _currentLine, _currentColumn);
                        break;

                    case '{':
                        // Read the { character from stream
                        ReadCharacter();

                        yield return new Token(TokenType.ProcedureStart, "{", _currentLine, _currentColumn);
                        break;

                    case '}':
                        // Read the { character from stream
                        ReadCharacter();

                        yield return new Token(TokenType.ProcedureEnd, "}", _currentLine, _currentColumn);
                        break;

                    default:
                        yield return ReadLiteral();
                        break;
                }
            }
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


        private Token ReadLiteral()
        {
            bool stop = false;

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
                        // Consume a whitespace character
                        ReadCharacter();
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
            }
            while (!stop);

            return new Token(TokenType.Literal, _stringBuilder.ToString(), _currentLine, _currentColumn);
        }


        private Token ReadHexString()
        {
            _stringBuilder.Clear();

            _stringBuilder.Append("<");

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
                    return new Token(TokenType.String, _stringBuilder.ToString(), _currentLine, _currentColumn);
                }
            }

            throw new SyntaxErrorException("Unexpected end of stream while reading hex encoded string.");
        }


        private Token ReadAscii85String()
        {
            _stringBuilder.Clear();

            _stringBuilder.Append("<~");

            bool endOfString = false;

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

                    return new Token(TokenType.String, _stringBuilder.ToString(), _currentLine, _currentColumn);
                }

                if (character == '~')
                {
                    endOfString = true;
                }
            }

            throw new SyntaxErrorException("Unexpected end of stream while reading ASCII85 encoded string.");
        }


        private Token ReadString()
        {
            _stringBuilder.Clear();

            // read starting parenthesis.
            _stringBuilder.Append((char)ReadCharacter());

            int previousCharacter = -1;
            int openParenthesesCount = 0;
            while (true)
            {
                var character = ReadCharacter();

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

            return new Token(TokenType.String, _stringBuilder.ToString(), _currentLine, _currentColumn);
        }


        private Token ReadComment()
        {
            int line;
            int column;
            string text = ReadLine(out line, out column);

            return new Token(TokenType.Comment, text, line, column);
        }
    }
}