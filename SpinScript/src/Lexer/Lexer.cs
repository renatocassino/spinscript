namespace SpinScript.Lexer;

public class Lexer
{
    private readonly string input;
    private int index = 0;
    private List<Token> tokens = [];

    public Lexer(string input)
    {
        this.input = input;
    }

    public List<Token> Tokenize()
    {
        tokens = [];
        while (index < input.Length)
        {
            var currentChar = input[index];
            if (char.IsWhiteSpace(currentChar)) {
                index++;
                continue;
            }

            if (char.IsDigit(currentChar)) {
                AddNumberToken();
                continue;
            }

            if (char.IsLetter(currentChar)) {
                AddReservedWord();
                continue;
            }

            switch (currentChar) {
                case '@': AddVariableReferenceToken(); break;
                case '=': AddToken(TokenType.EQUALS, currentChar.ToString()); break;
                case ';': AddToken(TokenType.SEMICOLON, currentChar.ToString()); break;
                case '{': AddToken(TokenType.LBRACE, currentChar.ToString()); break;
                case '}': AddToken(TokenType.RBRACE, currentChar.ToString()); break;
                case '(': AddToken(TokenType.LPAREN, currentChar.ToString()); break;
                case ')': AddToken(TokenType.RPAREN, currentChar.ToString()); break;
                default:

                    throw new LexerException($"Unexpected character '{currentChar}' at index {index}");
                    break;
            }
        }

        tokens.Add(new Token(TokenType.EOF, ""));

        return tokens;
    }

    private void AddToken(TokenType t, string v) {
        tokens.Add(new Token(t, v));
        index++;
    }

    private void AddReservedWord()
    {

    }

    private void AddNumberToken()
    {
        var number = "";
        for (int i = index; i < input.Length; i++)
        {
            if (int.TryParse(input[i].ToString(), out int currentResult)) {
                number += currentResult.ToString();
                continue;
            }
            break;
        }

        if (int.TryParse(number, out int result)) {
            tokens.Add(new Token(TokenType.NUMBER, result.ToString()));
        } else {
            throw new Exception("Invalid convertion to integer");
        }
        index += number.Length;
    }

    private void AddVariableReferenceToken()
    {
        index++;

        if (index >= input.Length || !char.IsLetter(input[index]))
        {
            throw new LexerException(
                $"A reference name must start with a letter at index {index}.");
        }

        int start = index;
        while (index < input.Length &&
            (char.IsLetterOrDigit(input[index]) || input[index] == '_'))
        {
            index++;
        }

        var reference = input[start..index];
        tokens.Add(new Token(TokenType.REFERENCE, reference));

    }
}
