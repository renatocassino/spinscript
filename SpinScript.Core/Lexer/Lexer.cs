using System.Globalization;
using System.IO.Pipelines;

namespace SpinScript.Lexer;

public class Lexer
{
    private readonly string _input;
    private int _index;
    private List<Token> _tokens = [];
    private int _line;
    private int _column;

    public Lexer(string input)
    {
        _input = input;
    }

    private static readonly Dictionary<string, TokenType> Keywords = new()
    {
        ["loop"] = TokenType.LOOP,
        ["play"] = TokenType.PLAY,
        ["song"] = TokenType.SONG,
        ["beat"] = TokenType.BEAT,
        ["melody"] = TokenType.MELODY,
    };

    private static readonly Dictionary<string, TokenType> BooleanKeywords = new()
    {
        ["true"] = TokenType.BOOLEAN,
        ["false"] = TokenType.BOOLEAN,
    };

    private static readonly HashSet<char> NoteLetters = ['A', 'B', 'C', 'D', 'E', 'F', 'G'];
    private static readonly HashSet<char> OctaveDigits = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9'];


    public List<Token> Tokenize()
    {
        _index = 0;
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
                if (TryAddBoolean() || TryAddKeyword() || TryAddNote() || TryAddPatternGrid())
                {
                    continue;
                }

                AddReservedWord();
                continue;
            }

            switch (currentChar)
            {
                case '@': AddVariableReferenceToken(); break;
                case '"':
                case '\'': AddStringToken(); break;
                case '=': AddToken(TokenType.EQUALS, currentChar.ToString()); break;
                case ';': AddToken(TokenType.SEMICOLON, currentChar.ToString()); break;
                case '{': AddToken(TokenType.LBRACE, currentChar.ToString()); break;
                case '}': AddToken(TokenType.RBRACE, currentChar.ToString()); break;
                case '(': AddToken(TokenType.LPAREN, currentChar.ToString()); break;
                case ')': AddToken(TokenType.RPAREN, currentChar.ToString()); break;
                case '/': SkipComment(); break;
                case ',': AddToken(TokenType.COMMA, currentChar.ToString()); break;
                default:
                    throw new LexerException($"Unexpected character '{currentChar}'", _line, _column);
            }
        }

        AddToken(TokenType.EOF, "");

        return _tokens;
    }

    private void SkipComment()
    {
        int startLine = _line;
        int startColumn = _column;

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

            throw new LexerException(
                "Unterminated multiline comment. Expected closing '*/'.",
                startLine, startColumn);
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

    private bool TryAddBoolean()
    {
        int start = _index;
        int startLine = _line;
        int startColumn = _column;
        _index++;
        _column++;

        while (_index < _input.Length &&
            (char.IsLetterOrDigit(_input[_index]) || _input[_index] == '_' || _input[_index] == '#'))
        {
            _index++;
            _column++;
        }

        var word = _input[start.._index];
        if (BooleanKeywords.TryGetValue(word, out var booleanType))
        {
            _tokens.Add(new Token(TokenType.BOOLEAN, word, startLine, startColumn));
            return true;
        }

        _index = start;
        _line = startLine;
        _column = startColumn;

        return false;
    }

    private bool TryAddKeyword()
    {
        int start = _index;
        int startLine = _line;
        int startColumn = _column;
        _index++;
        _column++;

        while (_index < _input.Length &&
            (char.IsLetterOrDigit(_input[_index]) || _input[_index] == '_' || _input[_index] == '#'))
        {
            _index++;
            _column++;
        }

        var word = _input[start.._index];
        if (Keywords.TryGetValue(word, out var keywordType))
        {
            _tokens.Add(new Token(keywordType, word, startLine, startColumn));
            return true;
        }

        _index = start;
        _line = startLine;
        _column = startColumn;

        return false;
    }

    private bool TryAddNote()
    {
        int start = _index;
        int startLine = _line;
        int startColumn = _column;
        _index++;
        _column++;

        while (_index < _input.Length &&
            (char.IsLetterOrDigit(_input[_index]) || _input[_index] == '#'))
        {
            _index++;
            _column++;
        }

        var word = _input[start.._index];

        if (CheckIsNote(word))
        {
            _tokens.Add(new Token(TokenType.NOTE, word, startLine, startColumn));
            return true;
        }

        _index = start;
        _line = startLine;
        _column = startColumn;

        return false;
    }

    private bool TryAddPatternGrid()
    {
        int start = _index;
        int startLine = _line;
        int startColumn = _column;
        _index++;
        _column++;

        while (_index < _input.Length &&
            (char.IsLetterOrDigit(_input[_index]) || _input[_index] == '|' || _input[_index] == '.'))
        {
            _index++;
            _column++;
        }

        var word = _input[start.._index];

        if (CheckIsPatternGrid(word))
        {
            _tokens.Add(new Token(TokenType.NOTE, word, startLine, startColumn));
            return true;
        }

        _index = start;
        _line = startLine;
        _column = startColumn;

        return false;
    }

    private bool CheckIsPatternGrid(string pattern)
    {
        if (pattern.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < pattern.Length; i++)
        {
            var currentChar = pattern[i];
            if (currentChar != 'x' && currentChar != '.' && currentChar != '-' && currentChar != '|')
            {
                return false;
            }
        }

        return true;
    }

    private void AddReservedWord()
    {
        int start = _index;
        int startLine = _line;
        int startColumn = _column;
        _index++;
        _column++;

        while (_index < _input.Length &&
            (char.IsLetterOrDigit(_input[_index]) || _input[_index] == '_' || _input[_index] == '#'))
        {
            _index++;
            _column++;
        }

        var word = _input[start.._index];

        _tokens.Add(new Token(TokenType.IDENT, word, startLine, startColumn));
    }

    public bool CheckIsNote(string note)
    {
        if (note.Length == 0)
        {
            return false;
        }

        if (!NoteLetters.Contains(note[0]))
        {
            return false;
        }

        if (note.Length == 1)
        {
            return true;
        }

        if (note.Length == 2)
        {
            return OctaveDigits.Contains(note[1]) || note[1] == '#' || note[1] == 'b';
        }

        if (note.Length == 3)
        {
            var secondChar = note[1];
            return (secondChar == '#' || secondChar == 'b') && OctaveDigits.Contains(note[2]);
        }

        return false;
    }

    private void AddNumberToken()
    {
        var number = "";
        int startLine = _line;
        int startColumn = _column;
        var kind = "integer";
        for (int i = _index; i < _input.Length; i++)
        {
            if (int.TryParse(_input[i].ToString(), out int _currentResult))
            {
                number += _input[i];
                continue;
            }

            if (_input[i] == '.') {
                kind = "double";
                if (number.Contains('.')) {
                    throw new LexerException("The number is invalid", startLine, startColumn);
                }

                number += '.';
                continue;
            }

            if (_input[i] == '/')
            {
                kind = "fraction";
                if (number.Contains('/'))
                {
                    throw new LexerException("Time notes must have only one slash", startLine, startColumn);
                }

                number += '/';
                continue;
            }
            break;
        }

        switch (kind)
        {
            case "integer":
                if (int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out var _numberInt))
                {
                    _tokens.Add(new Token(TokenType.NUMBER, _numberInt.ToString(CultureInfo.InvariantCulture), startLine, startColumn));
                    break;
                }
                
                throw new LexerException("Invalid conversion to integer", startLine, startColumn);
            case "double":
                if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
                {
                    _tokens.Add(new Token(TokenType.NUMBER, result.ToString(CultureInfo.InvariantCulture), startLine, startColumn));
                    break;
                }
                throw new LexerException("Invalid conversion to double", startLine, startColumn);
            case "fraction":
                _tokens.Add(new Token(TokenType.FRACTION, number, startLine, startColumn));
                break;
            default:
                throw new LexerException("Kind of number is invalid", startLine, startColumn);
        }

        _index += number.Length;
        _column += number.Length;
    }

    private void AddStringToken()
    {
        int startLine = _line;
        int startColumn = _column;
        char quoteChar = _input[_index];
        _index++;
        _column++;

        var strValue = "";
        while (_index < _input.Length && _input[_index] != quoteChar)
        {
            strValue += _input[_index];
            _index++;
            _column++;
        }

        if (_index >= _input.Length)
        {
            throw new LexerException("Unterminated string literal", startLine, startColumn);
        }

        // Consume the closing quote
        _index++;
        _column++;

        _tokens.Add(new Token(TokenType.STRING_LITERAL, strValue, startLine, startColumn));
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
