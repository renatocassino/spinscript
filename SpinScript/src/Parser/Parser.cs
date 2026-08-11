namespace SpinScript.Parser;

using SpinScript.Lexer;
using Ast;

public class Parser
{
    public int index;
    private readonly List<Token> tokens = [];

    public Parser(string input)
    {
        tokens = new Lexer(input).Tokenize();
        index = 0;
    }

    public ProgramNode Parse()
    {
        var statements = new List<AstNode>();

        while (index < tokens.Count)
        {
            var token = tokens[index];

            switch (token.Type)
            {
                case TokenType.REFERENCE: statements.Add(ParseReference()); break;
                case TokenType.EOF: ParseEOF(); break;
                default:
                    throw new ParserException($"Cannot parse token '{token.Type}' with value '{token.Value}'");
            }
        }

        return new ProgramNode(statements);
    }

    public void ParseEOF()
    {
        Consume(TokenType.EOF);
        index++;
        if (index < tokens.Count)
        {
            throw new ParserException($"Cannot read more tokens after EOF. Found more '{tokens.Count}' tokens.");
        }
    }

    public AssignmentNode ParseReference()
    {
        var currentToken = Consume(TokenType.REFERENCE);

        var varName = currentToken.Value;
        Consume(TokenType.EQUALS);
        var value = Consume(TokenType.NUMBER);
        Consume(TokenType.SEMICOLON);

        return new AssignmentNode(varName, value.Value);
    }

    public Token Consume(TokenType expected)
    {
        var firstToken = tokens[index];
        index++;

        if (expected == firstToken.Type)
        {
            return firstToken;
        }

        throw new ParserException($"Expected token '{expected}' but received token '{firstToken}'.");
    }
}
