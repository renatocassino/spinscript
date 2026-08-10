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
            switch (currentChar) {
                case ' ': index++; break;
                case '@': AddVariableReferenceToken(); break;
                case '=': AddEqualToken(); break;
                case ';': AddSemiColon(); break;
                default: 
                    if (char.IsDigit(currentChar))
                        AddNumberToken();
                    else
                        index++;
                    break;
            }
        }

        tokens.Add(new Token(TokenType.EOF, ""));

        return tokens;
    }

    private void AddSemiColon() {
        tokens.Add(new Token(TokenType.SEMICOLON, ";"));
        index++;
    }

    private void AddEqualToken()
    {
        tokens.Add(new Token(TokenType.EQUALS, "="));
        index++;
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
