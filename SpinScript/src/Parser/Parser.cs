namespace SpinScript.Parser;

using Ast;
using SpinScript.Lexer;

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
                case TokenType.PATTERN: statements.Add(ParsePattern()); break;
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

    public AstNode ParsePattern()
    {
        Consume(TokenType.PATTERN);
        var patternName = Consume(TokenType.REFERENCE);

        var parameters = ParseParams();

        var steps = new List<string>();
        Consume(TokenType.LBRACE);
        steps = ParseSteps();
        Consume(TokenType.RBRACE);
        Consume(TokenType.SEMICOLON);

        return new PatternNode(patternName.Value, parameters, steps);
    }

    private List<string> ParseSteps()
    {
        var steps = new List<string>();

        if (Check(TokenType.RBRACE))
        {
            return steps;
        }

        steps.Add(Consume(TokenType.NUMBER).Value);

        while (Match(TokenType.COMMA))
        {
            steps.Add(Consume(TokenType.NUMBER).Value);
        }

        return steps;
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

    // Parênteses são opcionais; se existirem, os parâmetros dentro
    // são uma lista de tamanho dinâmico separada por vírgula.
    private Dictionary<string, string> ParseParams()
    {
        var parameters = new Dictionary<string, string>();

        if (!Match(TokenType.LPAREN))
        {
            return parameters;
        }

        if (!Check(TokenType.RPAREN))
        {
            ParseParam(parameters);

            while (Match(TokenType.COMMA))
            {
                ParseParam(parameters);
            }
        }

        Consume(TokenType.RPAREN);
        return parameters;
    }

    private void ParseParam(Dictionary<string, string> parameters)
    {
        var paramName = Consume(TokenType.IDENT);
        Consume(TokenType.EQUALS);
        var value = Consume(TokenType.NUMBER);

        if (!parameters.TryAdd(paramName.Value, value.Value))
        {
            throw new ParserException($"Parameter '{paramName.Value}' was already set.");
        }
    }

    public Token Consume(TokenType expected)
    {
        var currentToken = tokens[index];

        if (expected != currentToken.Type)
        {
            throw new ParserException($"Expected token '{expected}' but received token '{currentToken}'.");
        }

        index++;
        return currentToken;
    }

    // Olha o próximo token sem consumir - é o que permite decidir
    // qual caminho seguir (=, (, {, ;) antes de comprometer o índice.
    private Token Peek() => tokens[index];

    private bool Check(TokenType type) => index < tokens.Count && Peek().Type == type;

    private bool Match(TokenType type)
    {
        if (!Check(type))
        {
            return false;
        }

        index++;
        return true;
    }
}
