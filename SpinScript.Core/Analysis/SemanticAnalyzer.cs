namespace SpinScript.Analysis;

using SpinScript.Lexer;
using SpinScript.Parser;
using SpinScript.Parser.Ast;

/// <summary>
/// Validates a syntactically valid <see cref="ProgramNode"/> before it
/// reaches the compiler: references that point nowhere, parameters with the
/// wrong type, steps outside their grid, notes the compiler can't pitch,
/// loops that play themselves forever, and so on. Never throws — every
/// problem becomes a <see cref="Diagnostic"/>, so one pass reports all of
/// them. Errors mean the compiler would crash or produce nonsense; warnings
/// mean the file compiles but something in it has no effect.
/// </summary>
public sealed class SemanticAnalyzer
{
    private enum ParamKind
    {
        PositiveInt,
        WholeNumber,
        Integer,
        PositiveNumber,
        String,
        Note,
    }

    // What the compiler actually reads from each construct. Anything else
    // parses fine but is ignored, which is worth a warning.
    private static readonly Dictionary<string, ParamKind> BeatParams = new() { ["sample"] = ParamKind.String, ["grid"] = ParamKind.PositiveInt };
    // `instrument` is the compiler's alias for a melody's `sample`.
    private static readonly Dictionary<string, ParamKind> MelodyParams = new() { ["sample"] = ParamKind.String, ["instrument"] = ParamKind.String, ["rootNote"] = ParamKind.Note };
    private static readonly Dictionary<string, ParamKind> LoopParams = new() { ["bars"] = ParamKind.PositiveNumber };
    private static readonly Dictionary<string, ParamKind> PlayParams = new() { ["repeat"] = ParamKind.WholeNumber, ["transpose"] = ParamKind.Integer };
    private static readonly Dictionary<string, ParamKind> NoParams = new();

    // Parameter names people reach for that mean something else here.
    private static readonly Dictionary<string, string> ParamAliases = new() { ["instrument"] = "sample" };

    private static readonly HashSet<string> ConfigVariables = ["bpm", "beatsPerBar"];

    private const int DefaultGrid = 16;

    private readonly List<Diagnostic> _diagnostics = [];
    private readonly Dictionary<string, AssignmentNode> _variables = [];
    private readonly Dictionary<string, AstNode> _declarations = [];
    private readonly HashSet<string> _inlineDeclarations = [];
    private readonly HashSet<string> _usedVariables = [];
    private SongNode? _song;

    private SemanticAnalyzer()
    {
    }

    public static IReadOnlyList<Diagnostic> Analyze(ProgramNode program) => new SemanticAnalyzer().Run(program);

    /// <summary>
    /// Lexes, parses and analyzes <paramref name="source"/>, returning every
    /// diagnostic found. Semantic analysis only runs when the source parses
    /// cleanly — on a half-parsed AST it would mostly report noise caused by
    /// the syntax errors themselves.
    /// </summary>
    public static IReadOnlyList<Diagnostic> Check(string source)
    {
        ParseResult result;
        try
        {
            result = new Parser(source).Parse();
        }
        catch (LexerException exception)
        {
            return [Diagnostic.FromLexerException(exception)];
        }

        if (result.HasErrors)
        {
            return result.Errors.Select(Diagnostic.FromParserException).ToList();
        }

        return Analyze(result.Ast);
    }

    private List<Diagnostic> Run(ProgramNode program)
    {
        RegisterTopLevel(program);
        CheckConfiguration();

        foreach (var statement in program.Statements)
        {
            switch (statement)
            {
                case BeatNode or MelodyNode or LoopNode:
                    CheckStatement(statement);
                    break;
                case SongNode song:
                    CheckSong(song);
                    break;
            }
        }

        if (_song is null)
        {
            _diagnostics.Add(Diagnostic.Error("missing-song", "No 'song' block found. A file needs exactly one 'song { ... }' to know what to play.", 0, 0));
        }
        else
        {
            CheckUnplayedDeclarations();
        }

        CheckLoopCycles();
        CheckUnusedVariables();

        return _diagnostics
            .OrderBy(diagnostic => diagnostic.Line)
            .ThenBy(diagnostic => diagnostic.Column)
            .ToList();
    }

    // -----------------------------------------------------------------------
    // Declarations
    // -----------------------------------------------------------------------

    private void RegisterTopLevel(ProgramNode program)
    {
        foreach (var statement in program.Statements)
        {
            switch (statement)
            {
                case AssignmentNode assignment:
                    if (_variables.TryGetValue(assignment.Name, out var previous))
                    {
                        _diagnostics.Add(Diagnostic.Warning("redefined-variable",
                            $"Variable '@{assignment.Name}' was already set on line {previous.Line + 1}; this value replaces it.",
                            assignment.Line, assignment.Column));
                    }
                    _variables[assignment.Name] = assignment;
                    break;
                case BeatNode or MelodyNode or LoopNode:
                    var (name, line, column) = Identify(statement);
                    if (_declarations.TryGetValue(name, out var existing))
                    {
                        _diagnostics.Add(Diagnostic.Error("duplicate-declaration",
                            $"'@{name}' is already declared as a {KindOf(existing)} on line {Identify(existing).Line + 1}.",
                            line, column));
                    }
                    else
                    {
                        _declarations[name] = statement;
                    }
                    if (statement is LoopNode loop)
                    {
                        RegisterInlineDeclarations(loop);
                    }
                    break;
                case PlayNode play:
                    _diagnostics.Add(Diagnostic.Error("play-outside-loop",
                        $"'play @{play.PatternName}' can only be used inside a 'loop' or the 'song'.",
                        play.Line, play.Column));
                    break;
                case SongNode song:
                    if (_song is null)
                    {
                        _song = song;
                    }
                    else
                    {
                        _diagnostics.Add(Diagnostic.Error("duplicate-song",
                            $"Only one 'song' block is allowed; the first one is on line {_song.Line + 1}.",
                            song.Line, song.Column));
                    }
                    break;
            }
        }
    }

    private void RegisterInlineDeclarations(LoopNode loop)
    {
        foreach (var statement in loop.Statements)
        {
            if (statement is BeatNode or MelodyNode or LoopNode)
            {
                _inlineDeclarations.Add(Identify(statement).Name);
            }
            if (statement is LoopNode inner)
            {
                RegisterInlineDeclarations(inner);
            }
        }
    }

    private void CheckConfiguration()
    {
        if (_variables.TryGetValue("bpm", out var bpm))
        {
            if (bpm.Value is not SpinValue.NumberValue { Value: > 0 })
            {
                _diagnostics.Add(Diagnostic.Error("invalid-config", $"'@bpm' must be a number greater than 0, got {bpm.Value}.", bpm.Line, bpm.Column));
            }
        }

        if (_variables.TryGetValue("beatsPerBar", out var beatsPerBar) && !IsPositiveInt(beatsPerBar.Value))
        {
            _diagnostics.Add(Diagnostic.Error("invalid-config", $"'@beatsPerBar' must be a whole number of at least 1, got {beatsPerBar.Value}.", beatsPerBar.Line, beatsPerBar.Column));
        }
    }

    // -----------------------------------------------------------------------
    // Statements
    // -----------------------------------------------------------------------

    private void CheckStatement(AstNode statement)
    {
        switch (statement)
        {
            case BeatNode beat:
                CheckBeat(beat);
                break;
            case MelodyNode melody:
                CheckMelody(melody);
                break;
            case LoopNode loop:
                CheckParameters(loop.Parameters, LoopParams, $"loop '@{loop.Name}'", loop.Line, loop.Column);
                foreach (var inner in loop.Statements)
                {
                    CheckStatement(inner);
                }
                break;
            case PlayNode play:
                CheckPlay(play);
                break;
            case AssignmentNode assignment:
                _diagnostics.Add(Diagnostic.Warning("ignored-assignment",
                    $"'@{assignment.Name}' is assigned inside a block, where assignments are ignored. Move it to the top of the file.",
                    assignment.Line, assignment.Column));
                break;
        }
    }

    private void CheckSong(SongNode song)
    {
        CheckParameters(song.Parameters, NoParams, "'song'", song.Line, song.Column);
        foreach (var statement in song.Statements)
        {
            CheckStatement(statement);
        }
    }

    private void CheckBeat(BeatNode beat)
    {
        var owner = $"beat '@{beat.Name}'";
        var parameters = CheckParameters(beat.Parameters, BeatParams, owner, beat.Line, beat.Column);
        RequireSample(beat.Parameters, owner, beat.Line, beat.Column);

        var grid = parameters.TryGetValue("grid", out var gridValue) && IsPositiveInt(gridValue)
            ? (int)gridValue.AsNumber()
            : DefaultGrid;

        var seen = new HashSet<int>();
        foreach (var step in beat.Steps)
        {
            if (step < 1 || step > grid)
            {
                _diagnostics.Add(Diagnostic.Error("step-out-of-grid",
                    $"Step {step} is outside the grid of {owner}, which has steps 1 to {grid}.",
                    beat.Line, beat.Column));
            }
            else if (!seen.Add(step))
            {
                _diagnostics.Add(Diagnostic.Warning("duplicate-step",
                    $"Step {step} appears more than once in {owner}.",
                    beat.Line, beat.Column));
            }
        }
    }

    private void CheckMelody(MelodyNode melody)
    {
        var owner = $"melody '@{melody.Name}'";
        CheckParameters(melody.Parameters, MelodyParams, owner, melody.Line, melody.Column);
        RequireSample(melody.Parameters, owner, melody.Line, melody.Column);

        foreach (var note in melody.Notes)
        {
            CheckTime(note.FractionStart, "start", note);
            if (CheckTime(note.FractionDuration, "duration", note) == 0)
            {
                _diagnostics.Add(Diagnostic.Warning("silent-note",
                    $"Note '{note.NoteName}' has a duration of 0 and won't be heard.",
                    note.Line, note.Column));
            }

            foreach (var name in note.Parameters.Keys)
            {
                _diagnostics.Add(Diagnostic.Warning("unknown-parameter",
                    $"Per-note parameters aren't supported yet; '{name}' on note '{note.NoteName}' is ignored.",
                    note.Line, note.Column));
            }
        }
    }

    // Returns the time as a fraction of a bar, or null when it's invalid.
    private double? CheckTime(string value, string what, Note note)
    {
        var parts = value.Split('/');
        if (parts.Length == 2 && double.TryParse(parts[0], out var numerator) && double.TryParse(parts[1], out var denominator))
        {
            if (denominator == 0)
            {
                _diagnostics.Add(Diagnostic.Error("invalid-time",
                    $"The {what} of note '{note.NoteName}' divides by zero ('{value}').",
                    note.Line, note.Column));
                return null;
            }
            return numerator / denominator;
        }

        return double.TryParse(value, out var whole) ? whole : null;
    }

    private void CheckPlay(PlayNode play)
    {
        var name = play.PatternName;
        CheckParameters(play.Parameters, PlayParams, $"'play @{name}'", play.Line, play.Column);

        if (_declarations.TryGetValue(name, out var target))
        {
            if (target is BeatNode && play.Parameters.ContainsKey("transpose"))
            {
                _diagnostics.Add(Diagnostic.Warning("ignored-transpose",
                    $"'transpose' has no effect on beat '@{name}': beats aren't pitched, only melodies are transposed.",
                    play.Line, play.Column));
            }
            return;
        }

        string message;
        if (_variables.ContainsKey(name))
        {
            _usedVariables.Add(name);
            message = $"'@{name}' is a variable, not a beat, melody or loop, so it can't be played.";
        }
        else if (_inlineDeclarations.Contains(name))
        {
            message = $"'@{name}' is declared inside a loop; only beats, melodies and loops declared at the top of the file can be played.";
        }
        else
        {
            message = $"Unknown beat, melody or loop '@{name}'.{Suggest(name, _declarations.Keys, name => $"@{name}")}";
        }

        _diagnostics.Add(Diagnostic.Error("unknown-reference", message, play.Line, play.Column));
    }

    // -----------------------------------------------------------------------
    // Parameters
    // -----------------------------------------------------------------------

    /// <summary>
    /// Validates each parameter against what the compiler expects and
    /// returns them with <c>@variable</c> references resolved to the
    /// variable's value (references that can't be resolved are left out).
    /// </summary>
    private Dictionary<string, SpinValue> CheckParameters(
        Dictionary<string, SpinValue> parameters,
        Dictionary<string, ParamKind> expected,
        string owner,
        int line,
        int column)
    {
        var resolved = new Dictionary<string, SpinValue>();

        foreach (var (name, rawValue) in parameters)
        {
            var value = ResolveValue(rawValue, name, owner, line, column);

            if (!expected.TryGetValue(name, out var kind))
            {
                var hint = ParamAliases.TryGetValue(name, out var alias) && expected.ContainsKey(alias)
                    ? $" Did you mean '{alias}'?"
                    : Suggest(name, expected.Keys, key => key);
                _diagnostics.Add(Diagnostic.Warning("unknown-parameter",
                    $"Parameter '{name}' isn't used by {owner} and will be ignored.{hint}",
                    line, column));
                continue;
            }

            if (value is null)
            {
                continue;
            }

            var problem = kind switch
            {
                ParamKind.PositiveInt when !IsPositiveInt(value) => "must be a whole number of at least 1",
                ParamKind.WholeNumber when value is not SpinValue.NumberValue { Value: >= 0 } whole || whole.Value != Math.Floor(whole.Value) =>
                    "must be a whole number of at least 0",
                ParamKind.Integer when value is not SpinValue.NumberValue integer || integer.Value != Math.Floor(integer.Value) =>
                    "must be a whole number of semitones, like 12 or -2",
                ParamKind.PositiveNumber when value is not SpinValue.NumberValue { Value: > 0 } => "must be a number greater than 0",
                ParamKind.String when value is not SpinValue.StringValue => "must be a string",
                ParamKind.Note when value is not SpinValue.StringValue { Value: var note } || !Lexer.IsNote(note) =>
                    "must be a note like \"C4\" or \"F#3\"",
                _ => null,
            };

            if (problem is null)
            {
                resolved[name] = value;
            }
            else
            {
                _diagnostics.Add(Diagnostic.Error("invalid-parameter",
                    $"Parameter '{name}' of {owner} {problem}, got {value}.",
                    line, column));
            }
        }

        return resolved;
    }

    // `sample=@kick` reaches the AST as a ReferenceValue; look the variable up.
    private SpinValue? ResolveValue(SpinValue value, string parameter, string owner, int line, int column)
    {
        if (value is not SpinValue.ReferenceValue { Name: var name })
        {
            return value;
        }

        if (_variables.TryGetValue(name, out var variable))
        {
            _usedVariables.Add(name);
            return variable.Value;
        }

        var message = _declarations.TryGetValue(name, out var declaration)
            ? $"Parameter '{parameter}' of {owner} points at '@{name}', which is a {KindOf(declaration)}, not a variable."
            : $"Parameter '{parameter}' of {owner} uses '@{name}', which isn't defined.{Suggest(name, _variables.Keys, name => $"@{name}")}";
        _diagnostics.Add(Diagnostic.Error("unknown-reference", message, line, column));
        return null;
    }

    private void RequireSample(Dictionary<string, SpinValue> parameters, string owner, int line, int column)
    {
        if (!parameters.ContainsKey("sample") && !(owner.StartsWith("melody", StringComparison.Ordinal) && parameters.ContainsKey("instrument")))
        {
            _diagnostics.Add(Diagnostic.Error("missing-sample",
                $"{char.ToUpperInvariant(owner[0])}{owner[1..]} needs a sample, e.g. (sample={(owner.StartsWith("beat", StringComparison.Ordinal) ? "@kick" : "@piano")}).",
                line, column));
        }
    }

    // -----------------------------------------------------------------------
    // Whole-program checks
    // -----------------------------------------------------------------------

    private void CheckUnplayedDeclarations()
    {
        var reached = new HashSet<string>();
        var pending = new Stack<AstNode>(_song!.Statements);

        while (pending.Count > 0)
        {
            switch (pending.Pop())
            {
                case PlayNode play when _declarations.TryGetValue(play.PatternName, out var target) && reached.Add(play.PatternName):
                    pending.Push(target);
                    break;
                case LoopNode loop:
                    foreach (var statement in loop.Statements)
                    {
                        pending.Push(statement);
                    }
                    break;
            }
        }

        foreach (var (name, declaration) in _declarations)
        {
            if (!reached.Contains(name))
            {
                var (_, line, column) = Identify(declaration);
                _diagnostics.Add(Diagnostic.Warning("unused-declaration",
                    $"{char.ToUpperInvariant(KindOf(declaration)[0])}{KindOf(declaration)[1..]} '@{name}' is declared but never played.",
                    line, column));
            }
        }
    }

    /// <summary>
    /// A loop that (directly or through other loops) plays itself would make
    /// the compiler recurse forever. Walks the "loop plays loop" graph and
    /// reports each back edge at the <c>play</c> that closes the cycle.
    /// </summary>
    private void CheckLoopCycles()
    {
        var finished = new HashSet<string>();
        var path = new List<string>();

        foreach (var name in _declarations.Keys)
        {
            Visit(name);
        }

        void Visit(string name)
        {
            if (finished.Contains(name) || _declarations[name] is not LoopNode loop)
            {
                return;
            }

            path.Add(name);
            foreach (var play in PlaysInside(loop))
            {
                var target = play.PatternName;
                if (!_declarations.ContainsKey(target))
                {
                    continue;
                }

                var index = path.IndexOf(target);
                if (index >= 0)
                {
                    var cycle = string.Join(" → ", path.Skip(index).Append(target).Select(n => $"@{n}"));
                    _diagnostics.Add(Diagnostic.Error("cycle",
                        $"Loop '@{target}' ends up playing itself ({cycle}), which would never finish.",
                        play.Line, play.Column));
                }
                else
                {
                    Visit(target);
                }
            }
            path.RemoveAt(path.Count - 1);
            finished.Add(name);
        }
    }

    private static IEnumerable<PlayNode> PlaysInside(LoopNode loop)
    {
        foreach (var statement in loop.Statements)
        {
            if (statement is PlayNode play)
            {
                yield return play;
            }
            else if (statement is LoopNode inner)
            {
                foreach (var nested in PlaysInside(inner))
                {
                    yield return nested;
                }
            }
        }
    }

    private void CheckUnusedVariables()
    {
        foreach (var (name, assignment) in _variables)
        {
            if (!ConfigVariables.Contains(name) && !_usedVariables.Contains(name))
            {
                _diagnostics.Add(Diagnostic.Warning("unused-variable",
                    $"Variable '@{name}' is never used.",
                    assignment.Line, assignment.Column));
            }
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static (string Name, int Line, int Column) Identify(AstNode node) => node switch
    {
        BeatNode beat => (beat.Name, beat.Line, beat.Column),
        MelodyNode melody => (melody.Name, melody.Line, melody.Column),
        LoopNode loop => (loop.Name, loop.Line, loop.Column),
        _ => throw new ArgumentException($"Not a declaration: {node.GetType().Name}", nameof(node)),
    };

    private static string KindOf(AstNode node) => node switch
    {
        BeatNode => "beat",
        MelodyNode => "melody",
        LoopNode => "loop",
        _ => "statement",
    };

    private static bool IsPositiveInt(SpinValue value) =>
        value is SpinValue.NumberValue { Value: >= 1 } number && number.Value == Math.Floor(number.Value);

    private static string Suggest(string name, IEnumerable<string> candidates, Func<string, string> format)
    {
        var best = candidates
            .Select(candidate => (candidate, distance: EditDistance(name.ToLowerInvariant(), candidate.ToLowerInvariant())))
            .Where(match => match.distance <= (name.Length <= 2 ? 1 : Math.Max(2, name.Length / 3)))
            .OrderBy(match => match.distance)
            .Select(match => match.candidate)
            .FirstOrDefault();

        return best is null ? "" : $" Did you mean '{format(best)}'?";
    }

    private static int EditDistance(string a, string b)
    {
        var previous = Enumerable.Range(0, b.Length + 1).ToArray();
        for (var i = 1; i <= a.Length; i++)
        {
            var current = new int[b.Length + 1];
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
            }
            previous = current;
        }
        return previous[b.Length];
    }
}
