namespace SpinScript.Compiler;

using System.Globalization;
using SpinScript.Analysis;
using SpinScript.Music;
using SpinScript.Parser;
using SpinScript.Parser.Ast;

public class Compiler
{
    private readonly string _input;

    private readonly Dictionary<string, SpinValue> _references = [];
    private readonly Dictionary<string, BeatNode> _beats = [];
    private readonly Dictionary<string, MelodyNode> _melodies = [];
    private readonly Dictionary<string, LoopNode> _loops = [];
    private SongNode? _song;

    // Names of the loops currently being compiled, innermost last. Used to
    // catch a loop that (directly or through another loop) plays itself,
    // which would otherwise recurse until the stack overflows.
    private readonly HashSet<string> _activeLoops = [];

    private CompileResult _compileResult = new([]);

    public CompileResult compileResult => _compileResult;

    private SongConfiguration _songConfiguration = new(120);

    private double _currentTime; // in milliseconds

    /// <summary>
    /// Warnings from the last successful <see cref="Compile"/>. When there
    /// are errors, <see cref="Compile"/> throws a <see cref="CompilerException"/>
    /// carrying all diagnostics instead.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; private set; } = [];

    /// <summary>The BPM / time signature the last <see cref="Compile"/> used.</summary>
    public SongConfiguration Configuration => _songConfiguration;

    public Compiler(string input)
    {
        _input = input;
    }

    /// <summary>
    /// Runs the whole pipeline (lexer → parser → compiler) and returns the
    /// resulting timeline. Throws <see cref="SpinScript.Lexer.LexerException"/>
    /// for a lexical error and <see cref="CompilerException"/> for anything
    /// else, both carrying the line/column of the problem. The semantic
    /// analyzer runs first, so a <see cref="CompilerException"/> normally
    /// lists every problem at once; the positioned errors thrown further
    /// down are a backstop for anything it doesn't catch.
    /// </summary>
    public CompileResult Compile() => Compile(ParseOrThrow());

    /// <summary>
    /// Compiles a single top-level beat, melody or loop on its own, as if the
    /// song were just <c>song { play @name; }</c> — for previewing one part.
    /// The real <c>song</c> block is ignored, so problems inside it don't get
    /// in the way; problems in the part itself (or anything it plays) do.
    /// </summary>
    public CompileResult CompilePart(string name)
    {
        var program = ParseOrThrow();

        var exists = program.Statements.Any(statement => statement switch
        {
            BeatNode beat => beat.Name == name,
            MelodyNode melody => melody.Name == name,
            LoopNode loop => loop.Name == name,
            _ => false,
        });
        if (!exists)
        {
            throw new CompilerException($"There's no top-level beat, melody or loop called '@{name}'.", 0, 0);
        }

        // Keep the variables and only the declarations this part reaches, so
        // a broken beat elsewhere in the file doesn't block the preview.
        var reachable = ReachableFrom(name, program);
        var statements = program.Statements
            .Where(statement => statement switch
            {
                AssignmentNode => true,
                BeatNode beat => reachable.Contains(beat.Name),
                MelodyNode melody => reachable.Contains(melody.Name),
                LoopNode loop => reachable.Contains(loop.Name),
                _ => false,
            })
            .Append(new SongNode([], [new PlayNode(name, [], 0, 0)], 0, 0))
            .ToList();

        return Compile(new ProgramNode(statements));
    }

    // Names of the top-level declarations `name` plays, directly or through
    // loops (including loops declared inline), plus `name` itself.
    private static HashSet<string> ReachableFrom(string name, ProgramNode program)
    {
        var loops = program.Statements.OfType<LoopNode>().GroupBy(loop => loop.Name).ToDictionary(g => g.Key, g => g.First());
        var reachable = new HashSet<string> { name };
        var pending = new Stack<string>([name]);

        while (pending.Count > 0)
        {
            if (!loops.TryGetValue(pending.Pop(), out var loop))
            {
                continue;
            }

            var body = new Stack<AstNode>(loop.Statements);
            while (body.Count > 0)
            {
                switch (body.Pop())
                {
                    case PlayNode play when reachable.Add(play.PatternName):
                        pending.Push(play.PatternName);
                        break;
                    case LoopNode inner:
                        foreach (var statement in inner.Statements)
                        {
                            body.Push(statement);
                        }
                        break;
                }
            }
        }

        return reachable;
    }

    private ProgramNode ParseOrThrow()
    {
        var ast = new Parser(_input).Parse();
        if (ast.HasErrors)
        {
            throw new CompilerException(ast.Errors.Select(Diagnostic.FromParserException).ToList());
        }
        return ast.Ast;
    }

    private CompileResult Compile(ProgramNode program)
    {
        var diagnostics = SemanticAnalyzer.Analyze(program);
        if (diagnostics.Any(diagnostic => diagnostic.IsError))
        {
            throw new CompilerException(diagnostics);
        }
        Diagnostics = diagnostics;

        Reset();
        RegisterStatements(program);
        UpdateConfiguration();
        CompileStatements();

        // Stable sort: events at the same instant keep their source order.
        _compileResult = new CompileResult([.. _compileResult.Events.OrderBy(EventTime)], _currentTime);
        return _compileResult;
    }

    private void Reset()
    {
        _references.Clear();
        _beats.Clear();
        _melodies.Clear();
        _loops.Clear();
        _activeLoops.Clear();
        _song = null;
        _program = null;
        _currentTime = 0;
        _compileResult = new CompileResult([]);
    }

    private static double EventTime(Event e) => e switch
    {
        SoundEvent sound => sound.Time,
        MelodyEvent melody => melody.Time,
        _ => 0,
    };

    private void UpdateConfiguration()
    {
        var bpm = 120.0;
        if (_references.TryGetValue("bpm", out var bpmValue))
        {
            if (bpmValue is not SpinValue.NumberValue { Value: > 0 } number)
            {
                throw Error(FindAssignment("bpm"), $"@bpm must be a number greater than zero, got {bpmValue}.");
            }
            bpm = number.Value;
        }

        var beatsPerBar = 4;
        if (_references.TryGetValue("beatsPerBar", out var beatsPerBarValue))
        {
            if (beatsPerBarValue is not SpinValue.NumberValue { Value: > 0 } number || number.Value != Math.Floor(number.Value))
            {
                throw Error(FindAssignment("beatsPerBar"), $"@beatsPerBar must be a whole number greater than zero, got {beatsPerBarValue}.");
            }
            beatsPerBar = (int)number.Value;
        }

        _songConfiguration = new SongConfiguration(bpm, beatsPerBar);
    }

    private ProgramNode? _program;

    private AssignmentNode? FindAssignment(string name) =>
        _program?.Statements.OfType<AssignmentNode>().LastOrDefault(a => a.Name == name);

    private void RegisterStatements(ProgramNode ast)
    {
        _program = ast;

        foreach (var statement in ast.Statements)
        {
            switch (statement)
            {
                case AssignmentNode assignment:
                    _references[assignment.Name] = assignment.Value;
                    break;
                case BeatNode beat:
                    _beats[beat.Name] = beat;
                    break;
                case LoopNode loop:
                    _loops[loop.Name] = loop;
                    break;
                case MelodyNode melody:
                    _melodies[melody.Name] = melody;
                    break;
                case PlayNode play:
                    // `play` only makes sense inside a loop or the song: at the
                    // top level there's no timeline position to play it at.
                    throw Error(play, $"'play @{play.PatternName}' must be inside a loop or the song block.");
                case SongNode song:
                    if (_song != null)
                    {
                        throw Error(song, "Only one song block is allowed per file.");
                    }
                    _song = song;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown statement type: {statement.GetType().Name}");
            }
        }
    }

    private void CompileStatements()
    {
        if (_song == null)
        {
            throw new CompilerException("No song block found. Add `song { play @something; }` to hear anything.", 0, 0);
        }

        // The song sequences everything it plays: each statement starts
        // where the previous one ended.
        foreach (var statement in _song.Statements)
        {
            CompileStatement(statement);
        }
    }

    private void CompileStatement(AstNode statement)
    {
        switch (statement)
        {
            case AssignmentNode:
                // Assignments inside loops/songs are accepted by the grammar
                // but don't affect compilation (yet).
                break;
            case BeatNode beat:
                _currentTime += CompileBeat(beat);
                break;
            case MelodyNode melody:
                _currentTime += CompileMelody(melody);
                break;
            case LoopNode loop:
                CompileLoop(loop);
                break;
            case PlayNode play:
                CompilePlay(play);
                break;
            default:
                throw new InvalidOperationException($"Unknown statement type: {statement.GetType().Name}");
        }
    }

    private void CompilePlay(PlayNode play)
    {
        AstNode target;
        if (_loops.TryGetValue(play.PatternName, out var loop))
        {
            target = loop;
        }
        else if (_beats.TryGetValue(play.PatternName, out var beat))
        {
            target = beat;
        }
        else if (_melodies.TryGetValue(play.PatternName, out var melody))
        {
            target = melody;
        }
        else
        {
            throw Error(play, $"'@{play.PatternName}' is not a beat, melody or loop declared at the top level.");
        }

        var repeatCount = GetWholeNumber(play.Parameters, "repeat", 1, play, min: 0);

        // Each repetition picks up where the previous one ended: a beat
        // advances one bar, a melody as many whole bars as it lasts, and a
        // loop however long its own body takes (see CompileLoop).
        for (var i = 0; i < repeatCount; i++)
        {
            CompileStatement(target);
        }
    }

    public void CompileLoop(LoopNode loop)
    {
        if (!_activeLoops.Add(loop.Name))
        {
            throw Error(loop, $"Loop '@{loop.Name}' plays itself (directly or through another loop), which would never end.");
        }

        try
        {
            CompileLoopBody(loop);
        }
        finally
        {
            _activeLoops.Remove(loop.Name);
        }
    }

    private void CompileLoopBody(LoopNode loop)
    {
        double? explicitBars = loop.Parameters.ContainsKey("bars")
            ? GetPositiveNumber(loop.Parameters, "bars", 1, loop)
            : null;
        var startTime = _currentTime;
        var furthestTime = startTime;

        foreach (var statement in loop.Statements)
        {
            if (!IsLoopReference(statement))
            {
                // Patterns (bare or via `play`) are parallel: they always
                // layer on top of this loop's own start time, regardless of
                // what a preceding sibling statement left _currentTime at.
                _currentTime = startTime;
            }
            // Loops (bare or via `play`) are sequential: each one picks up
            // from wherever the previous statement left _currentTime.

            CompileStatement(statement);

            // Track the furthest point any single statement reached, since
            // parallel statements (e.g. two bare patterns with different
            // `repeat` counts) can each advance _currentTime by a different
            // amount; the loop's own duration must cover the longest one,
            // not just whichever statement happened to run last.
            furthestTime = Math.Max(furthestTime, _currentTime);
        }

        if (explicitBars.HasValue)
        {
            // Explicit `bars=` is an authoritative override: force the loop
            // to occupy exactly that many bars, regardless of what its body
            // actually advanced _currentTime by.
            _currentTime = startTime + explicitBars.Value * _songConfiguration.BarDurationMs;
        }
        else if (furthestTime == startTime)
        {
            // Nothing in the body advanced time on its own — default to a
            // single bar.
            _currentTime = startTime + _songConfiguration.BarDurationMs;
        }
        else
        {
            _currentTime = furthestTime;
        }
    }

    // A statement counts as a "loop reference" (sequential) when it either
    // is a loop body itself or is a `play` of something registered as a
    // loop. Everything else (bare patterns, `play` of a pattern) is treated
    // as parallel content that always starts at the enclosing loop's start.
    private bool IsLoopReference(AstNode statement)
    {
        return statement switch
        {
            LoopNode => true,
            PlayNode play => _loops.ContainsKey(play.PatternName),
            _ => false,
        };
    }

    /// <summary>
    /// Emits one <see cref="MelodyEvent"/> per note, offset by the current
    /// timeline position, and returns how long the melody occupies on the
    /// timeline: its last note's end, rounded up to whole bars (at least
    /// one), so a melody repeated or followed by something else stays on
    /// the bar grid.
    /// </summary>
    public double CompileMelody(MelodyNode melody)
    {
        var parameters = melody.Parameters;

        var rootNote = GetString(parameters, "rootNote", "C4", melody);
        if (!Lexer.Lexer.IsNote(rootNote))
        {
            throw Error(melody, $"rootNote '{rootNote}' of melody '@{melody.Name}' is not a note like C4 or F#3.");
        }

        // `instrument` is accepted as an alias of `sample` (older examples use it).
        var sampleKey = parameters.ContainsKey("sample") ? "sample" : "instrument";
        var sample = GetRequiredString(parameters, sampleKey, melody, $"Melody '@{melody.Name}' needs a sample, e.g. (sample=@piano).");

        var barMs = _songConfiguration.BarDurationMs;
        var melodyEnd = 0.0;

        foreach (var note in melody.Notes)
        {
            var start = FractionToTime(note.FractionStart);
            var duration = FractionToTime(note.FractionDuration);
            var rate = CalcRate(note.NoteName, rootNote);

            _compileResult.Events.Add(new MelodyEvent(sample, _currentTime + start, duration, note.NoteName, rate));
            melodyEnd = Math.Max(melodyEnd, start + duration);
        }

        // Tiny epsilon so floating point noise (e.g. 3 × 1/3 bar) doesn't
        // round a melody that ends exactly on a bar line up to the next one.
        var bars = Math.Max(1, Math.Ceiling(melodyEnd / barMs - 1e-9));
        return bars * barMs;
    }

    /// <summary>MIDI number of a note (C4 = 60); the octave defaults to 4.</summary>
    public static int NoteToMidi(string note) => Pitch.ToMidi(note);

    public static double CalcRate(string desiredNote, string rootNote)
    {
        var semitones = NoteToMidi(desiredNote) - NoteToMidi(rootNote);
        return Math.Pow(2, semitones / 12.0);
    }

    /// <summary>
    /// Converts a time written in bars (<c>1</c>, <c>1/4</c>, <c>3/2</c>) to
    /// milliseconds at the current BPM.
    /// </summary>
    public double FractionToTime(string fraction)
    {
        if (fraction.Contains('/'))
        {
            var nums = fraction.Split('/');

            if (double.TryParse(nums[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var n1) &&
                double.TryParse(nums[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var n2) &&
                n2 != 0)
            {
                return n1 / n2 * _songConfiguration.BarDurationMs;
            }
        }
        else if (double.TryParse(fraction, NumberStyles.Float, CultureInfo.InvariantCulture, out var bars))
        {
            return _songConfiguration.BarDurationMs * bars;
        }

        throw new InvalidOperationException($"Invalid time notation: '{fraction}'. Expected a fraction like '1/4' or a plain number.");
    }

    /// <summary>
    /// Emits one <see cref="SoundEvent"/> per step, offset by the current
    /// timeline position, and returns how long a beat lasts: always one bar.
    /// </summary>
    public double CompileBeat(BeatNode beat)
    {
        var parameters = beat.Parameters;

        var sample = GetRequiredString(parameters, "sample", beat, $"Beat '@{beat.Name}' needs a sample, e.g. (sample=@kick).");
        var grid = GetWholeNumber(parameters, "grid", 16, beat, min: 1);
        var barMs = _songConfiguration.BarDurationMs;
        var stepMs = barMs / grid;

        foreach (var step in beat.Steps.Distinct().Order())
        {
            if (step < 1 || step > grid)
            {
                throw Error(beat, $"Step {step} of beat '@{beat.Name}' is outside its grid (1 to {grid}).");
            }

            var startTime = _currentTime + stepMs * (step - 1);
            _compileResult.Events.Add(new SoundEvent(sample, startTime, 100));
        }

        return barMs;
    }

    // -----------------------------------------------------------------------
    // Parameter helpers: read a parameter, resolve `@references`, check its
    // type, and report a positioned error when something's off.
    // -----------------------------------------------------------------------

    private SpinValue? Resolve(Dictionary<string, SpinValue> parameters, string key, AstNode node)
    {
        if (!parameters.TryGetValue(key, out var value))
        {
            return null;
        }

        var seen = new HashSet<string>();
        while (value is SpinValue.ReferenceValue reference)
        {
            if (!seen.Add(reference.Name) || !_references.TryGetValue(reference.Name, out var referenced))
            {
                throw Error(node, $"'@{reference.Name}' (used in {key}=) is not declared. Declare it with `@{reference.Name} = ...;`.");
            }
            value = referenced;
        }

        return value;
    }

    private string GetString(Dictionary<string, SpinValue> parameters, string key, string fallback, AstNode node)
    {
        return Resolve(parameters, key, node) switch
        {
            null => fallback,
            SpinValue.StringValue s => s.Value,
            var other => throw Error(node, $"Parameter '{key}' must be a string, got {other}."),
        };
    }

    private string GetRequiredString(Dictionary<string, SpinValue> parameters, string key, AstNode node, string missingMessage)
    {
        return Resolve(parameters, key, node) switch
        {
            null => throw Error(node, missingMessage),
            SpinValue.StringValue s => s.Value,
            var other => throw Error(node, $"Parameter '{key}' must be a string, got {other}."),
        };
    }

    private double GetPositiveNumber(Dictionary<string, SpinValue> parameters, string key, double fallback, AstNode node)
    {
        return Resolve(parameters, key, node) switch
        {
            null => fallback,
            SpinValue.NumberValue { Value: > 0 } n => n.Value,
            var other => throw Error(node, $"Parameter '{key}' must be a number greater than zero, got {other}."),
        };
    }

    private int GetWholeNumber(Dictionary<string, SpinValue> parameters, string key, int fallback, AstNode node, int min)
    {
        return Resolve(parameters, key, node) switch
        {
            null => fallback,
            SpinValue.NumberValue n when n.Value >= min && n.Value == Math.Floor(n.Value) => (int)n.Value,
            var other => throw Error(node, $"Parameter '{key}' must be a whole number of at least {min}, got {other}."),
        };
    }

    private static CompilerException Error(AstNode? node, string message)
    {
        var (line, column) = node switch
        {
            AssignmentNode n => (n.Line, n.Column),
            BeatNode n => (n.Line, n.Column),
            MelodyNode n => (n.Line, n.Column),
            LoopNode n => (n.Line, n.Column),
            PlayNode n => (n.Line, n.Column),
            SongNode n => (n.Line, n.Column),
            _ => (0, 0),
        };

        return new CompilerException(message, line, column);
    }
}
