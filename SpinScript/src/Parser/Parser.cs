namespace SpinScript.Parser;

using SpinScript.Lexer;

public class Parser
{
    private Dictionary<string, string> variables = new();
    private List<Token> tokens = [];

    public Parser(string input)
    {
        tokens = new Lexer(input).Tokenize();
    }

    public void Parse()
    {
        while (true)
        {
            if (tokens.Count == 0)
            {
                break;
            }
            var token = tokens[0];

            switch (token.Type)
            {
                case TokenType.REFERENCE: ParseReference(); break;
                case TokenType.EOF: ParseEOF(); break;
                default:
                    throw new ParserException($"Cannot parse token {token.Type} with value {token.Value}");
            }
        }
    }

    public void ParseEOF()
    {
        Consume(TokenType.EOF);
        if (tokens.Count > 0)
        {
            throw new ParserException($"Cannot read more tokens after EOF. Found more {tokens.Count} tokens.");
        }
    }

    public void ParseReference()
    {
        var currentToken = Consume(TokenType.REFERENCE);

        var varName = currentToken.Value;
        Consume(TokenType.EQUALS);
        var value = Consume(TokenType.NUMBER);
        Consume(TokenType.SEMICOLON);

        if (variables.ContainsKey(varName))
        {
            throw new ParserException($"Variable '{varName}' is already defined");
        }

        variables[varName] = value.Value;
    }

    public Token Consume(TokenType expected)
    {
        var firstToken = tokens[0];
        tokens.RemoveAt(0);

        if (expected == firstToken.Type)
        {
            return firstToken;
        }

        throw new ParserException($"Expected token {expected} but received token {firstToken}.");
    }
}
