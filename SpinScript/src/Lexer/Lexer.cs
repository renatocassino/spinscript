using System.Globalization;

namespace SpinScript.Lexer;

public class Lexer
{
    private readonly string _input;
    private int _index = 0;
    private List<Token> _tokens = [];

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
        while (_index < _input.Length)
        {
            var currentChar = _input[_index];
            if (char.IsWhiteSpace(currentChar))
            {
                _index++;
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
                    throw new LexerException($"Unexpected character '{currentChar}' at index {_index}");
                    break;
            }
        }

        _tokens.Add(new Token(TokenType.EOF, ""));

        return _tokens;
    }

    private void IgnoreCommentInline()
    {
        _index++;

        if (_index >= _input.Length || (_input[_index] != '/' && _input[_index] != '*'))
        {
            throw new LexerException($"Comments must have two slices '//' to inline comments or '/*' for multiline comments. Received '{_input[_index]}'");
        }

        if (_input[_index] == '*')
        {
            _index++;

            while (_index < _input.Length - 1)
            {
                if (_input[_index] == '*' && _input[_index + 1] == '/')
                {
                    _index += 2;
                    return;
                }

                _index++;
            }

            return;
        }

        _index++;

        while (_index < _input.Length && _input[_index] != '\n')
        {
            _index++;
        }
    }

    private void AddToken(TokenType t, string v)
    {
        _tokens.Add(new Token(t, v));
        _index++;
    }

    private void AddReservedWord()
    {
        int start = _index;
        _index++;
        while (_index < _input.Length &&
            (char.IsLetterOrDigit(_input[_index]) || _input[_index] == '_'))
        {
            _index++;
        }

        var word = _input[start.._index];

        if (Keywords.TryGetValue(word, out var keywordType))
        {
            _tokens.Add(new Token(keywordType, word));
            return;
        }

        _tokens.Add(new Token(TokenType.IDENT, word));
    }

    private void AddNumberToken()
    {
        var number = "";
        for (int i = _index; i < _input.Length; i++)
        {
            if (int.TryParse(_input[i].ToString(), out int currentResult))
            {
                number += currentResult.ToString(CultureInfo.InvariantCulture);
                continue;
            }
            break;
        }

        if (int.TryParse(number, out var result))
        {
            _tokens.Add(new Token(TokenType.NUMBER, result.ToString(CultureInfo.InvariantCulture)));
        }
        else
        {
            throw new LexerException("Invalid convertion to integer");
        }

        _index += number.Length;
    }

    private void AddVariableReferenceToken()
    {
        _index++;

        if (_index >= _input.Length || !char.IsLetter(_input[_index]))
        {
            throw new LexerException(
                $"A reference name must start with a letter at index {_index}.");
        }

        int start = _index;
        while (_index < _input.Length &&
            (char.IsLetterOrDigit(_input[_index]) || _input[_index] == '_'))
        {
            _index++;
        }

        var reference = _input[start.._index];
        _tokens.Add(new Token(TokenType.REFERENCE, reference));

    }
}
