using System.Globalization;

namespace SpinScript.Lexer;

public class Lexer
{
    private readonly string _input;
    private int _index = 0;
    private List<Token> _tokens = [];
    private int _line = 0;
    private int _column = 0;

    public Lexer(string input)
    {
        _input = input;
    }

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["loop"] = TokenType.LOOP,
        ["play"] = TokenType.PLAY,
        ["song"] = TokenType.SONG,
        ["repeat"] = TokenType.REPEAT,
        ["pattern"] = TokenType.PATTERN,
    };


    public List<Token> Tokenize()
    {
        _tokens = [];
        _line = 0;
        _column = 0;
        while (_index < _input.Length)
        {
            var currentChar = _input[_index];
            if (char.IsWhiteSpace(currentChar))
            {
                _index++;
                _column++;
                if (currentChar == '\n') {
                    _line++;
                    _column = 0;
                }
                continue;
            }

            if (char.IsDigit(currentChar))
            {
                AddNumberToken();
                continue;
            }

            if (char.IsLetter(currentChar))
            {
                AddReservedWord();
                continue;
            }

            switch (currentChar)
            {
                case '@': AddVariableReferenceToken(); break;
                case '=': AddToken(TokenType.EQUALS, currentChar.ToString()); break;
                case ';': AddToken(TokenType.SEMICOLON, currentChar.ToString()); break;
                case '{': AddToken(TokenType.LBRACE, currentChar.ToString()); break;
                case '}': AddToken(TokenType.RBRACE, currentChar.ToString()); break;
                case '(': AddToken(TokenType.LPAREN, currentChar.ToString()); break;
                case ')': AddToken(TokenType.RPAREN, currentChar.ToString()); break;
                case '/': IgnoreCommentInline(); break;
                case ',': AddToken(TokenType.COMMA, currentChar.ToString()); break;
                default:
                    throw new LexerException($"Unexpected character '{currentChar}'", _line, _column);
            }
        }

        AddToken(TokenType.EOF, "");

        return _tokens;
    }

    private void IgnoreCommentInline()
    {
        _index++;
        _column++;

        if (_index >= _input.Length)
        {
            throw new LexerException(
                "Comments must have two slices '//' to inline comments or '/*' for multiline comments. Reached end of input.",
                _line, _column);
        }

        if (_input[_index] != '/' && _input[_index] != '*')
        {
            throw new LexerException(
                $"Comments must have two slices '//' to inline comments or '/*' for multiline comments. Received '{_input[_index]}'",
                _line, _column);
        }

        if (_input[_index] == '*')
        {
            _index++;
            _column++;

            while (_index < _input.Length - 1)
            {
                if (_input[_index] == '*' && _input[_index + 1] == '/')
                {
                    _index += 2;
                    _column += 2;
                    return;
                }

                if (_input[_index] == '\n')
                {
                    _line++;
                    _column = 0;
                }
                else
                {
                    _column++;
                }
                _index++;
            }

            return;
        }

        _index++;
        _column++;

        while (_index < _input.Length && _input[_index] != '\n')
        {
            _index++;
            _column++;
        }
    }

    private void AddToken(TokenType t, string v)
    {
        _tokens.Add(new Token(t, v, _line, _column));
        _column++;
        _index++;
    }

    private void AddReservedWord()
    {
        int start = _index;
        int startLine = _line;
        int startColumn = _column;
        _index++;
        _column++;
        while (_index < _input.Length &&
            (char.IsLetterOrDigit(_input[_index]) || _input[_index] == '_'))
        {
            _index++;
            _column++;
        }

        var word = _input[start.._index];

        if (Keywords.TryGetValue(word, out var keywordType))
        {
            _tokens.Add(new Token(keywordType, word, startLine, startColumn));
            return;
        }

        _tokens.Add(new Token(TokenType.IDENT, word, startLine, startColumn));
    }

    private void AddNumberToken()
    {
        var number = "";
        int startLine = _line;
        int startColumn = _column;
        for (int i = _index; i < _input.Length; i++)
        {
            if (int.TryParse(_input[i].ToString(), out int currentResult))
            {
                number += currentResult.ToString(CultureInfo.InvariantCulture);
                continue;
            }

            if (_input[i] == '.') {
                if (number.Contains('.')) {
                    throw new LexerException("The number is invalid", startLine, startColumn);
                }

                number += '.';
                continue;
            }
            break;
        }

        if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            _tokens.Add(new Token(TokenType.NUMBER, result.ToString(CultureInfo.InvariantCulture), startLine, startColumn));
        }
        else
        {
            throw new LexerException("Invalid conversion to number", startLine, startColumn);
        }

        _index += number.Length;
        _column += number.Length;
    }

    private void AddVariableReferenceToken()
    {
        int startLine = _line;
        int startColumn = _column;
        _index++;
        _column++;

        if (_index >= _input.Length || !char.IsLetter(_input[_index]))
        {
            throw new LexerException(
                "A reference name must start with a letter.", _line, _column);
        }

        int start = _index;
        while (_index < _input.Length &&
            (char.IsLetterOrDigit(_input[_index]) || _input[_index] == '_'))
        {
            _index++;
            _column++;
        }

        var reference = _input[start.._index];
        _tokens.Add(new Token(TokenType.REFERENCE, reference, startLine, startColumn));
    }
}
