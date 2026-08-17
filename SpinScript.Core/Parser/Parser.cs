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
    /// <example>
    /// <c>new Parser("@bpm = 129;")</c> tokenizes the input immediately;
    /// call <see cref="Parse"/> afterwards to build the AST.
    /// </example>
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
    /// <example>
    /// <c>"@bpm = 129; beat @kick (grid=16) { 9 }; play @kick;"</c>
    /// validates and yields a <see cref="ProgramNode"/> with three
    /// statements: an <see cref="AssignmentNode"/>, a <see cref="BeatNode"/>
    /// and a <see cref="PlayNode"/>.
    /// </example>
    public ProgramNode Parse()
    {
        var statements = new List<AstNode>();

        while (_index < _tokens.Count)
        {
            var token = _tokens[_index];

            switch (token.Type)
            {
                case TokenType.REFERENCE: statements.Add(ParseReference()); break;
                case TokenType.BEAT: statements.Add(ParseBeat()); break;
                case TokenType.LOOP: statements.Add(ParseLoop()); break;
                case TokenType.PLAY: statements.Add(ParsePlay()); break;
                case TokenType.EOF: ParseEOF(); break;
                case TokenType.SONG: statements.Add(ParseSong()); break;
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
    /// <example>
    /// <c>""</c> (empty input) tokenizes to a single EOF token, so
    /// <c>ParseEOF()</c> consumes it and returns without throwing.
    /// </example>
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
    /// Parses a <c>play</c> statement: <c>play @name (param, ...)?;</c>,
    /// where <c>@name</c> references a previously declared pattern and the
    /// optional parenthesized list is parsed by <see cref="ParseParameters"/>.
    /// </summary>
    /// <example>
    /// <c>"play @intro;"</c> → <c>PlayNode</c> with <c>PatternName == "intro"</c>
    /// and no parameters.<br/>
    /// <c>"play @chord (bpm=129, sample=@kick, free, free=true);"</c> → same
    /// node with <c>Parameters == { bpm: "129", sample: "kick", free: "true" }</c>
    /// (the duplicate <c>free</c> key here would actually throw — see
    /// <see cref="ParseParam"/>).
    /// </example>
    public PlayNode ParsePlay()
    {
        Consume(TokenType.PLAY);
        var beatRef = Consume(TokenType.REFERENCE);
        var parameters = ParseParameters();

        Consume(TokenType.SEMICOLON);

        return new PlayNode(beatRef.Value, parameters, beatRef.Line, beatRef.Column);
    }

    /// <summary>
    /// Parses the optional parenthesized parameter list that follows a
    /// <c>play</c> statement's pattern reference. Functionally identical to
    /// <see cref="ParseParams"/> — both wrap the same comma-separated
    /// <c>key=value</c> / bare-flag grammar handled by <see cref="ParseParam"/> —
    /// this copy exists specifically for <see cref="ParsePlay"/>.
    /// </summary>
    /// <example>
    /// <c>"(bpm=129, sample=@kick, free, free=true)"</c> →
    /// <c>{ bpm: "129", sample: "kick", free: "true" }</c> (again, only one
    /// of the two <c>free</c> entries may appear per call, see
    /// <see cref="ParseParam"/>).<br/>
    /// No leading <c>(</c> at all (e.g. the input is just <c>";"</c>) →
    /// returns an empty dictionary without consuming anything.
    /// </example>
    public Dictionary<string, SpinValue> ParseParameters()
    {
        var parameters = new Dictionary<string, SpinValue>();

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
    /// Parses a <c>song</c> block: <c>song (param, ...)? { statement* }</c>.
    /// The optional parenthesized parameter list is parsed by
    /// <see cref="ParseParams"/>; the body may contain <c>play</c> statements
    /// and reference assignments.
    /// </summary>
    /// <example>
    /// <c>"song { play @intro; @volume = 80; }"</c> → <c>SongNode</c> with
    /// two statements: a <see cref="PlayNode"/> and an <see cref="AssignmentNode"/>.<br/>
    /// <c>"song (bpm=129) { }"</c> → <c>SongNode</c> with an empty body
    /// (the <c>bpm</c> parameter is currently parsed but discarded).
    /// </example>
    public SongNode ParseSong()
    {
        var songToken = Consume(TokenType.SONG);
        ParseParams();
        Consume(TokenType.LBRACE);

        var statements = new List<AstNode>();

        while (!Check(TokenType.RBRACE))
        {
            var token = _tokens[_index];

            switch (token.Type)
            {
                case TokenType.PLAY: statements.Add(ParsePlay()); break;
                case TokenType.REFERENCE: statements.Add(ParseReference()); break;
                case TokenType.EOF:
                    throw new ParserException($"Unexpected EOF inside song body. Expected closing '}}'.", token.Line, token.Column);
                default:
                    throw new ParserException($"Cannot parse token '{token.Type}' with value '{token.Value}' inside song.", token.Line, token.Column);
            }
        }

        Consume(TokenType.RBRACE);

        return new SongNode(statements, songToken.Line, songToken.Column);
    }

    /// <summary>
    /// Parses a beat declaration:
    /// <c>beat @name (param=value, ...)? { step, step, ... };</c>.
    /// The parenthesized parameter list is optional (see <see cref="ParseParams"/>);
    /// the step list is a comma-separated sequence of numbers enclosed in
    /// braces (see <see cref="ParseSteps"/>).
    /// </summary>
    /// <example>
    /// <c>"beat @kick (grid=16, sample=@kick, free, free=true) { 1, 5, 9, 13 };"</c>
    /// → <c>BeatNode</c> with <c>Name == "kick"</c>,
    /// <c>Parameters == { grid: "16", sample: "kick", free: "true" }</c>
    /// and <c>Steps == ["1", "5", "9", "13"]</c>.<br/>
    /// <c>"beat @hats { };"</c> → same node shape with no parameters and
    /// an empty step list.
    /// </example>
    public AstNode ParseBeat()
    {
        Consume(TokenType.BEAT);
        var beatName = Consume(TokenType.REFERENCE);
        var parameters = ParseParams();

        Consume(TokenType.LBRACE);

        var steps = ParseSteps();

        Consume(TokenType.RBRACE);
        Consume(TokenType.SEMICOLON);

        return new BeatNode(beatName.Value, parameters, steps, beatName.Line, beatName.Column);
    }

    public AstNode ParseMelody()
    {
        Consume(TokenType.MELODY);
        var beatName = Consume(TokenType.REFERENCE);
        var parameters = ParseParams();

        Consume(TokenType.LBRACE);

        var notes = ParseNotes();

        Consume(TokenType.RBRACE);
        Consume(TokenType.SEMICOLON);

        return new MelodyNode(beatName.Value, parameters, notes, beatName.Line, beatName.Column);
    }


    private List<Note> ParseNotes()
    {
        var melody = new List<Note>();

        while (!Check(TokenType.RBRACE) && !Check(TokenType.COMMA))
        {
            if (Check(TokenType.COMMA))
            {
                Consume(TokenType.COMMA);
            }
            var noteValue = Consume(TokenType.NOTE);

            var startAt = Consume(TokenType.FRACTION); // TODO - Validate number too here
            var duration = Consume(TokenType.FRACTION);
            var parameters = ParseParameters();

            melody.Add(new Note(noteValue.Value, ParseFraction(startAt.Value), ParseFraction(duration.Value), parameters));
        }

        return melody;
    }

    private int ParseFraction(string input)
    {
        return 10;
    }

    /// <summary>
    /// Parses a loop block: <c>loop @count { statement* }</c>. The body may
    /// contain assignments, patterns, and nested loops — this method calls
    /// itself recursively when it encounters another <c>loop</c> token,
    /// which is what allows arbitrarily deep loop nesting.
    /// </summary>
    /// <example>
    /// <c>"loop @times (bpm=129, free) { play @kick; }"</c> → <c>LoopNode</c>
    /// with <c>Name == "times"</c> and a single <see cref="PlayNode"/>
    /// statement (the <c>(bpm=129, free)</c> parameters are currently parsed
    /// but discarded, see <see cref="ParseParam"/> for what each form means).<br/>
    /// <c>"loop @n { loop @m { } }"</c> → <c>LoopNode</c> whose single
    /// statement is itself a nested <c>LoopNode</c>.
    /// </example>
    public LoopNode ParseLoop()
    {
        var loopToken = Consume(TokenType.LOOP);
        var loopCountToken = Consume(TokenType.REFERENCE);
        var parameters = ParseParams();

        Consume(TokenType.LBRACE);

        var statements = new List<AstNode>();

        while (!Check(TokenType.RBRACE))
        {
            var token = _tokens[_index];

            switch (token.Type)
            {
                case TokenType.LOOP: statements.Add(ParseLoop()); break;
                case TokenType.REFERENCE: statements.Add(ParseReference()); break;
                case TokenType.BEAT: statements.Add(ParseBeat()); break;
                case TokenType.PLAY: statements.Add(ParsePlay()); break;
                case TokenType.EOF:
                    throw new ParserException($"Unexpected EOF inside loop body. Expected closing '}}'.", token.Line, token.Column);
                default:
                    throw new ParserException($"Cannot parse token '{token.Type}' with value '{token.Value}' inside loop.", token.Line, token.Column);
            }
        }

        Consume(TokenType.RBRACE);

        return new LoopNode(loopCountToken.Value, parameters, statements, loopToken.Line, loopToken.Column);
    }

    /// <summary>
    /// Parses the comma-separated list of step numbers inside a pattern's
    /// braces, e.g. <c>{ 1, 5, 9, 13 }</c>. Returns an empty list when the
    /// braces are immediately closed (an empty pattern body is valid).
    /// </summary>
    /// <example>
    /// <c>"{ 1, 5, 9, 13 }"</c> (called right after consuming the leading
    /// <c>{</c>) → <c>["1", "5", "9", "13"]</c>.<br/>
    /// <c>"{ }"</c> → <c>[]</c>.
    /// </example>
    private List<int> ParseSteps()
    {
        var steps = new List<int>();

        if (Check(TokenType.RBRACE))
        {
            return steps;
        }

        steps.Add(int.Parse(Consume(TokenType.NUMBER).Value));

        while (Match(TokenType.COMMA))
        {
            steps.Add(int.Parse(Consume(TokenType.NUMBER).Value));
        }

        return steps;
    }

    /// <summary>
    /// Parses a variable assignment: <c>@name = value;</c>, where
    /// <c>value</c> is either a number or a string literal. Used both for
    /// top-level assignments and for assignments nested inside a loop body.
    /// </summary>
    /// <example>
    /// <c>"@bpm = 129;"</c> → <c>AssignmentNode</c> with
    /// <c>Name == "bpm"</c>, <c>Value == "129"</c>.<br/>
    /// <c>"@guitarMidi = \"/guitar.mid\";"</c> → <c>Name == "guitarMidi"</c>,
    /// <c>Value == "/guitar.mid"</c>.<br/>
    /// Note this method does not (yet) accept a <c>BOOLEAN</c> token, so
    /// <c>"@muted = true;"</c> throws — unlike <see cref="ParseParam"/>,
    /// which does.
    /// </example>
    public AssignmentNode ParseReference()
    {
        var currentToken = Consume(TokenType.REFERENCE);

        var varName = currentToken.Value;
        Consume(TokenType.EQUALS);

        SpinValue value;
        if (Check(TokenType.NUMBER))
        {
            value = new SpinValue.NumberValue(double.Parse(Consume(TokenType.NUMBER).Value));
        } else if (Check(TokenType.STRING_LITERAL))
        {
            value = new SpinValue.StringValue(Consume(TokenType.STRING_LITERAL).Value);
        }
        else if (Check(TokenType.BOOLEAN))
        {
            value = new SpinValue.BooleanValue(bool.Parse(Consume(TokenType.BOOLEAN).Value));
        }
        else
        {
            throw new ParserException($"Expected a number or string after '=' but received token '{Peek()}'.", currentToken.Line, currentToken.Column);
        }
        
        Consume(TokenType.SEMICOLON);

        return new AssignmentNode(varName, value, currentToken.Line, currentToken.Column);
    }

    /// <summary>
    /// Parses the optional parenthesized parameter list of a pattern, e.g.
    /// <c>(grid=16, offset=2)</c>. The parentheses themselves are optional —
    /// when absent, an empty parameter dictionary is returned; when present,
    /// they wrap a comma-separated, variable-length list of key=value pairs
    /// (see <see cref="ParseParam"/>). Functionally identical to
    /// <see cref="ParseParameters"/>; used by <see cref="ParseSong"/>,
    /// <see cref="ParseBeat"/> and <see cref="ParseLoop"/>.
    /// </summary>
    /// <example>
    /// <c>"(grid=16, sample=@kick, free, free=true)"</c> →
    /// <c>{ grid: "16", sample: "kick", free: "true" }</c> (only one
    /// <c>free</c> entry may be present at a time, see <see cref="ParseParam"/>).<br/>
    /// No <c>(</c> at all → returns an empty dictionary without consuming
    /// anything, since the parameter list is optional.
    /// </example>
    private Dictionary<string, SpinValue> ParseParams()
    {
        var parameters = new Dictionary<string, SpinValue>();

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
    /// Parses a single parameter entry — either <c>key=value</c> or a bare
    /// <c>key</c> flag — and adds it to the given dictionary, throwing if
    /// the same key was already set earlier in the same parameter list.
    /// A bare <c>key</c> (no <c>=</c> follows it) is treated as a boolean
    /// flag and stored as the string <c>"true"</c>, since the value is
    /// implied by the parameter's mere presence.
    /// </summary>
    /// <example>
    /// <c>bpm=129</c> → value comes from a <c>NUMBER</c> token, stored as
    /// <c>"129"</c>.<br/>
    /// <c>sample=@kick</c> → value comes from a <c>REFERENCE</c> token,
    /// stored as <c>"kick"</c> (referencing a variable/pattern by name).<br/>
    /// <c>free</c> (no <c>=</c>, immediately followed by <c>,</c> or <c>)</c>)
    /// → treated as a boolean flag, stored as <c>"true"</c> without ever
    /// consuming an <c>EQUALS</c> token.<br/>
    /// <c>free=true</c> → value comes explicitly from a <c>BOOLEAN</c>
    /// token, also stored as <c>"true"</c> — same resulting value as the
    /// bare-flag form above, just spelled out.<br/>
    /// <c>label="intro"</c> → value comes from a <c>STRING_LITERAL</c>
    /// token, stored as <c>"intro"</c>.<br/>
    /// <c>bpm=129, bpm=140</c> in the same parameter list → throws
    /// <see cref="ParserException"/> on the second occurrence, since
    /// <c>bpm</c> was already set.
    /// </example>
    private void ParseParam(Dictionary<string, SpinValue> parameters)
    {
        var paramName = Consume(TokenType.IDENT);

        if (!Check(TokenType.EQUALS))
        {
            // If dont have an equals, only the IDENT, so we can assume its a boolean flag, and set it to true
            if (!parameters.TryAdd(paramName.Value, new SpinValue.BooleanValue(true)))
            {
                throw new ParserException($"Parameter '{paramName.Value}' was already set.", paramName.Line, paramName.Column);
            }

            if (Check(TokenType.COMMA) || Check(TokenType.RPAREN))
            {
                return;
            }
            else
            {
                throw new ParserException($"Expected a comma or closing parenthesis after parameter or set a new value to '{paramName.Value}' but received token '{Peek()}'.", paramName.Line, paramName.Column);
            }
        }
        Consume(TokenType.EQUALS);

        SpinValue value;
        if (Check(TokenType.STRING_LITERAL))
        {
            value = new SpinValue.StringValue(Consume(TokenType.STRING_LITERAL).Value);
        } else if (Check(TokenType.NUMBER))
        {
            value = new SpinValue.NumberValue(double.Parse(Consume(TokenType.NUMBER).Value));
        } else if (Check(TokenType.REFERENCE))
        {
            value = new SpinValue.StringValue($"@{Consume(TokenType.REFERENCE).Value}");
        }
        else if (Check(TokenType.BOOLEAN))
        {
            value = new SpinValue.BooleanValue(bool.Parse(Consume(TokenType.BOOLEAN).Value));
        }
        else
        {
            throw new ParserException($"Expected a number or string after '=' but received token '{Peek()}'.", paramName.Line, paramName.Column);
        }

        if (!parameters.TryAdd(paramName.Value, value))
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
    /// <example>
    /// Current token is <c>EQUALS</c> and <c>expected == TokenType.EQUALS</c>
    /// → returns that token and advances past it.<br/>
    /// Current token is <c>NUMBER</c> and <c>expected == TokenType.EQUALS</c>
    /// → throws <see cref="ParserException"/> without advancing.
    /// </example>
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
    /// <example>
    /// Tokens are <c>[NUMBER("129"), SEMICOLON, EOF]</c> and <c>_index == 0</c>
    /// → returns the <c>NUMBER("129")</c> token, <c>_index</c> stays at
    /// <c>0</c>.
    /// </example>
    private Token Peek() => _tokens[_index];

    /// <summary>
    /// Tests whether the current token matches the given type without
    /// consuming it. The one-token-lookahead building block behind both
    /// <see cref="Match"/> and every switch-based dispatch in this class.
    /// </summary>
    /// <example>
    /// Current token is <c>COMMA</c> → <c>Check(TokenType.COMMA)</c> is
    /// <c>true</c>, <c>Check(TokenType.RPAREN)</c> is <c>false</c>, and
    /// neither call advances <c>_index</c>.<br/>
    /// <c>_index</c> is already past the end of the token list → returns
    /// <c>false</c> instead of throwing an out-of-range exception.
    /// </example>
    private bool Check(TokenType type) => _index < _tokens.Count && Peek().Type == type;

    /// <summary>
    /// Consumes the current token only if it matches the given type,
    /// returning whether it did. Used for optional grammar elements (e.g.
    /// the trailing comma in a comma-separated list) where a mismatch is
    /// not an error, just a signal to stop.
    /// </summary>
    /// <example>
    /// Current token is <c>COMMA</c> and <c>type == TokenType.COMMA</c>
    /// → returns <c>true</c> and advances past it (used by
    /// <see cref="ParseParams"/> to loop over <c>param, param, param</c>).<br/>
    /// Current token is <c>RPAREN</c> and <c>type == TokenType.COMMA</c>
    /// → returns <c>false</c> without advancing, signaling the list is done.
    /// </example>
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
