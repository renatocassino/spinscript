namespace SpinScript.Parser;

using Ast;
using SpinScript.Lexer;

/// <summary>
/// Recursive-descent parser that consumes the token stream produced by the
/// <see cref="Lexer"/> and builds the SpinScript abstract syntax tree
/// (see <c>Ast.cs</c>). Each grammar production (assignment, pattern, loop,
/// play, ...) has a corresponding <c>Parse*</c> method below; the low-level
/// helpers at the bottom of the class (<see cref="Consume"/>, <see cref="Peek"/>,
/// <see cref="Check"/>, <see cref="Match"/>) are the shared primitives every
/// production is built on top of.
/// </summary>
public class Parser
{
    private int _index;
    private readonly List<Token> _tokens = [];

    /// <summary>
    /// Tokenizes the given source text up front so the parser can operate
    /// purely on the resulting token list instead of interleaving lexing
    /// and parsing.
    /// </summary>
    public Parser(string input)
    {
        _tokens = new Lexer(input).Tokenize();
        _index = 0;
    }

    /// <summary>
    /// Entry point of the grammar: <c>program := statement* EOF</c>.
    /// Repeatedly dispatches on the current token's type to the matching
    /// top-level statement parser (assignment, pattern, loop, play) until
    /// the EOF token is reached, collecting every parsed node into the
    /// resulting <see cref="ProgramNode"/>.
    /// </summary>
    public ProgramNode Parse()
    {
        var statements = new List<AstNode>();

        while (_index < _tokens.Count)
        {
            var token = _tokens[_index];

            switch (token.Type)
            {
                case TokenType.REFERENCE: statements.Add(ParseReference()); break;
                case TokenType.PATTERN: statements.Add(ParsePattern()); break;
                case TokenType.LOOP: statements.Add(ParseLoop()); break;
                case TokenType.PLAY: statements.Add(ParsePlay()); break;
                case TokenType.EOF: ParseEOF(); break;
                default:
                    throw new ParserException($"Cannot parse token '{token.Type}' with value '{token.Value}'", token.Line, token.Column);
            }
        }

        return new ProgramNode(statements);
    }

    /// <summary>
    /// Consumes the EOF token and asserts it is truly the last token in the
    /// stream. This guards against a malformed token list where extra
    /// tokens follow EOF, which should never happen coming from the lexer
    /// but is validated here as a defensive terminal check.
    /// </summary>
    public void ParseEOF()
    {
        Consume(TokenType.EOF);
        _index++;
        if (_index < _tokens.Count)
        {
            throw new ParserException($"Cannot read more tokens after EOF. Found more '{_tokens.Count}' tokens.", _tokens[_index].Line, _tokens[_index].Column);
        }
    }

    /// <summary>
    /// Intended to parse a <c>play</c> statement: <c>play @name pattern-list;</c>,
    /// where the pattern list references previously declared patterns, each
    /// optionally followed by its own arguments. Work in progress — the body
    /// currently does not compile, see <see cref="ParseParam"/> usage below.
    /// </summary>
    public PlayNode ParsePlay()
    {
        Consume(TokenType.PLAY);
        var patternRef = Consume(TokenType.REFERENCE);
        var parameters = ParseParameters();

        Consume(TokenType.SEMICOLON);

        return new PlayNode(patternRef.Value, parameters, patternRef.Line, patternRef.Column);
    }

    public Dictionary<string, string> ParseParameters()
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

    /// <summary>
    /// Parses a pattern declaration:
    /// <c>pattern @name (param=value, ...)? { step, step, ... };</c>.
    /// The parenthesized parameter list is optional (see <see cref="ParseParams"/>);
    /// the step list is a comma-separated sequence of numbers enclosed in
    /// braces (see <see cref="ParseSteps"/>).
    /// </summary>
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

        return new PatternNode(patternName.Value, parameters, steps, patternName.Line, patternName.Column);
    }

    /// <summary>
    /// Parses a loop block: <c>loop @count { statement* }</c>. The body may
    /// contain assignments, patterns, and nested loops — this method calls
    /// itself recursively when it encounters another <c>loop</c> token,
    /// which is what allows arbitrarily deep loop nesting.
    /// </summary>
    public LoopNode ParseLoop()
    {
        var loopToken = Consume(TokenType.LOOP);
        var loopCountToken = Consume(TokenType.REFERENCE);

        Consume(TokenType.LBRACE);

        var statements = new List<AstNode>();

        while (!Check(TokenType.RBRACE))
        {
            var token = _tokens[_index];

            switch (token.Type)
            {
                case TokenType.LOOP: statements.Add(ParseLoop()); break;
                case TokenType.REFERENCE: statements.Add(ParseReference()); break;
                case TokenType.PATTERN: statements.Add(ParsePattern()); break;
                case TokenType.PLAY: statements.Add(ParsePlay()); break;
                case TokenType.EOF:
                    throw new ParserException($"Unexpected EOF inside loop body. Expected closing '}}'.", token.Line, token.Column);
                default:
                    throw new ParserException($"Cannot parse token '{token.Type}' with value '{token.Value}' inside loop.", token.Line, token.Column);
            }
        }

        Consume(TokenType.RBRACE);

        return new LoopNode(loopCountToken.Value, statements, loopToken.Line, loopToken.Column);
    }

    /// <summary>
    /// Parses the comma-separated list of step numbers inside a pattern's
    /// braces, e.g. <c>{ 1, 5, 9, 13 }</c>. Returns an empty list when the
    /// braces are immediately closed (an empty pattern body is valid).
    /// </summary>
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

    /// <summary>
    /// Parses a variable assignment: <c>@name = value;</c>, where
    /// <c>value</c> is either a number or a string literal. Used both for
    /// top-level assignments and for assignments nested inside a loop body.
    /// </summary>
    public AssignmentNode ParseReference()
    {
        var currentToken = Consume(TokenType.REFERENCE);

        var varName = currentToken.Value;
        Consume(TokenType.EQUALS);

        Token value;
        if (Check(TokenType.NUMBER))
        {
            value = Consume(TokenType.NUMBER);
        } else if (Check(TokenType.STRING))
        {
            value = Consume(TokenType.STRING);
        }
        else
        {
            throw new ParserException($"Expected a number or string after '=' but received token '{Peek()}'.", currentToken.Line, currentToken.Column);
        }
        
        Consume(TokenType.SEMICOLON);

        return new AssignmentNode(varName, value.Value, currentToken.Line, currentToken.Column);
    }

    /// <summary>
    /// Parses the optional parenthesized parameter list of a pattern, e.g.
    /// <c>(grid=16, offset=2)</c>. The parentheses themselves are optional —
    /// when absent, an empty parameter dictionary is returned; when present,
    /// they wrap a comma-separated, variable-length list of key=value pairs
    /// (see <see cref="ParseParam"/>).
    /// </summary>
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

    /// <summary>
    /// Parses a single <c>key=value</c> parameter entry (value is a number
    /// or a string literal) and adds it to the given dictionary, throwing
    /// if the same key was already set earlier in the same parameter list.
    /// </summary>
    private void ParseParam(Dictionary<string, string> parameters)
    {
        var paramName = Consume(TokenType.IDENT);
        Consume(TokenType.EQUALS);

        Token value;
        if (Check(TokenType.STRING))
        {
            value = Consume(TokenType.STRING);
        } else if (Check(TokenType.NUMBER))
        {
            value = Consume(TokenType.NUMBER);
        }
        else
        {
            throw new ParserException($"Expected a number or string after '=' but received token '{Peek()}'.", paramName.Line, paramName.Column);
        }

        if (!parameters.TryAdd(paramName.Value, value.Value))
        {
            throw new ParserException($"Parameter '{paramName.Value}' was already set.", paramName.Line, paramName.Column);
        }
    }

    /// <summary>
    /// Core token-stream primitive: asserts the current token matches the
    /// expected type, advances past it, and returns it. Every grammar
    /// production ultimately bottoms out in a call to this method to
    /// consume the terminals it expects.
    /// </summary>
    public Token Consume(TokenType expected)
    {
        var currentToken = _tokens[_index];

        if (expected != currentToken.Type)
        {
            throw new ParserException($"Expected token '{expected}' but received token '{currentToken}'.", currentToken.Line, currentToken.Column);
        }

        _index++;
        return currentToken;
    }

    /// <summary>
    /// Looks at the current token without consuming it. This is what lets
    /// callers decide which production to take (e.g. whether an assignment
    /// value is a number or a string) before committing the index forward.
    /// </summary>
    private Token Peek() => _tokens[_index];

    /// <summary>
    /// Tests whether the current token matches the given type without
    /// consuming it. The one-token-lookahead building block behind both
    /// <see cref="Match"/> and every switch-based dispatch in this class.
    /// </summary>
    private bool Check(TokenType type) => _index < _tokens.Count && Peek().Type == type;

    /// <summary>
    /// Consumes the current token only if it matches the given type,
    /// returning whether it did. Used for optional grammar elements (e.g.
    /// the trailing comma in a comma-separated list) where a mismatch is
    /// not an error, just a signal to stop.
    /// </summary>
    private bool Match(TokenType type)
    {
        if (!Check(type))
        {
            return false;
        }

        _index++;
        return true;
    }
}
