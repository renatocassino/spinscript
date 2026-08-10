namespace SpinScript.Lexer;
using System.Text.RegularExpressions;

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
                case ' ': break;
                case '@': AddVariableReferenceToken(); break;
                case '=': AddEqualToken(); break;
                case ';': AddSemiColon(); break;
                default: break;
            }

            if (int.TryParse(currentChar.ToString(), out int result)) {
                AddNumberToken();
            }
            index++;
        }

        tokens.Add(new Token(TokenType.EOF, @"EOF"));

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
        index += number.Length - 1;
    }

    private void AddVariableReferenceToken()
    {
        var reference = "";
        for (int i = index + 1; i < input.Length; i++)
        {
            if (Regex.IsMatch(input[i].ToString(), "^[a-zA-Z0-9]$")) {
                reference += input[i];
                continue;
            }
            break;
        }

        if (reference.Length == 0) {
            throw new Exception("Invalid variable reference");
        }
        index += reference.Length;
        tokens.Add(new Token(TokenType.REFERENCE, reference));
    }
}
