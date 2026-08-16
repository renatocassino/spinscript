namespace SpinScript.Interpreter;

using SpinScript.Parser;
using SpinScript.Parser.Ast;

public class Interpreter
{
    private readonly string _input;
    private readonly Parser _parser;

    private Dictionary<string, SpinValue> _references = new Dictionary<string, SpinValue>();
    private Dictionary<string, PatternNode> _patterns = new Dictionary<string, PatternNode>();
    private Dictionary<string, LoopNode> _loops = new Dictionary<string, LoopNode>();
    private SongNode? song;

    private InterpretResult _interpretResult = new InterpretResult(new List<SoundEvent>());

    public InterpretResult interpretResult => _interpretResult;

    private SongConfiguration _songConfiguration = new SongConfiguration(120);

    private int _currentTime = 0; // in milliseconds

    public Interpreter(string input)
    {
        _input = input;
        _parser = new Parser(input);
    }

    public void Interpret()
    {
        var ast = _parser.Parse();
        RegisterStatements(ast);
        UpdateConfiguration();
        InterpretStatements();
    }

    public void UpdateConfiguration()
    {
        _songConfiguration = new SongConfiguration(120); // Default BPM
        if (_references.ContainsKey("bpm"))
        {
            var bpmValue = _references["bpm"];
            if (bpmValue is SpinValue.NumberValue numberValue)
            {
                _songConfiguration = new SongConfiguration((int)numberValue.Value);
                Console.WriteLine($"BPM set to: {numberValue.Value}");
            }
            else
            {
                throw new InvalidOperationException($"Expected NumberValue for bpm, got {bpmValue.GetType().Name}");
            }
        }

        if (_references.ContainsKey("beatsPerBar"))
        {
            var beatsPerBarValue = _references["beatsPerBar"];
            if (beatsPerBarValue is SpinValue.NumberValue numberValue)
            {
                _songConfiguration = new SongConfiguration(_songConfiguration.BPM, (int)numberValue.Value);
                Console.WriteLine($"Beats per bar: {numberValue.Value}");
            }
            else
            {
                throw new InvalidOperationException($"Expected NumberValue for beatsPerBar, got {beatsPerBarValue.GetType().Name}");
            }
        }
    }

    private void RegisterStatements(ProgramNode ast)
    {
        ast.Statements.ForEach(statement =>
        {
            switch (statement)
            {
                case AssignmentNode assignment:
                    _references[assignment.Name] = assignment.Value;
                    break;
                case PatternNode pattern:
                    _patterns[pattern.Name] = pattern;
                    break;
                case LoopNode loop:
                    _loops[loop.Name] = loop;
                    break;
                case PlayNode play:
                    // Probably there are something wrong, because play exists only in loops and songs, so it should be handled in those cases.
                    throw new InvalidOperationException("PlayNode should not be at the top level.");
                    break;
                case SongNode song:
                    if (this.song != null)
                    {
                        throw new InvalidOperationException("Multiple song nodes are not allowed.");
                    }
                    this.song = song;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown statement type: {statement.GetType().Name}");
            }
        });
    }

    private void InterpretStatements()
    {
        if (song == null)
        {
            throw new InvalidOperationException("No song node found.");
        }

        _interpretResult = new InterpretResult(new List<SoundEvent>());

        foreach (var statement in song.Statements)
        {
            InterpretStatement(statement);
        }
    }

    private void InterpretStatement(AstNode statement)
    {
        switch (statement)
        {
            case AssignmentNode assignment:
                Console.WriteLine($"Assignment: @{assignment.Name} = {assignment.Value}");
                // _references[assignment.Name] = assignment.Value;
                break;
            case PatternNode pattern:
                Console.WriteLine($"Pattern: {pattern.Name}");
                // Return value (its bar duration) is intentionally discarded:
                // sibling `play @pattern;` statements inside the same loop
                // body are meant to layer on the same bar (see InterpretLoop's
                // "body didn't advance time" fallback), not play back to back.
                InterpretPattern(pattern);
                break;
            case LoopNode loop:
                Console.WriteLine($"Loop: {loop.Name}");
                InterpretLoop(loop);
                break;
            case PlayNode play:
                var isLoop = _loops.ContainsKey(play.PatternName);
                var isPattern = _patterns.ContainsKey(play.PatternName);
                var parameters = play.Parameters;

                var repeatCount = 1;
                if (parameters.ContainsKey("repeat"))
                {
                    repeatCount = parameters["repeat"].AsInt();
                }

                AstNode patternOrLoop;
                if (isLoop)
                {
                    patternOrLoop = _loops[play.PatternName];
                } else if (isPattern)
                {
                    patternOrLoop = _patterns[play.PatternName];
                } else
                {
                    throw new InvalidOperationException($"PlayNode references unknown pattern or loop: {play.PatternName}");
                }

                for (var i = 0; i < repeatCount; i++)
                {
                    Console.WriteLine($"Play: {play.PatternName} (repeat {i + 1}/{repeatCount})");
                    if (isLoop || isPattern)
                    {
                        Console.WriteLine($"Play Loop/Pattern: {play.PatternName}");
                        InterpretStatement(patternOrLoop);
                    }
                    else
                    {
                        throw new InvalidOperationException($"PlayNode references unknown pattern or loop: {play.PatternName}");
                    }
                }
                break;
            default:
                throw new InvalidOperationException($"Unknown statement type: {statement.GetType().Name}");
        }
    }

    public void InterpretLoop(LoopNode loop)
    {
        var explicitBars = loop.Parameters.ContainsKey("bars")
            ? loop.Parameters["bars"].AsInt()
            : (int?)null;
        var startTime = _currentTime;

        foreach (var statement in loop.Statements)
        {
            InterpretStatement(statement);
        }

        if (explicitBars.HasValue)
        {
            // Explicit `bars=` is an authoritative override: force the loop
            // to occupy exactly that many bars, regardless of what its body
            // actually advanced _currentTime by.
            _currentTime = startTime + explicitBars.Value * (int)_songConfiguration.BarDurationMs;
        }
        else if (_currentTime == startTime)
        {
            // The body didn't advance time on its own (e.g. only bare
            // pattern plays, which layer on the same bar instead of
            // advancing) — default to a single bar.
            _currentTime = startTime + (int)_songConfiguration.BarDurationMs;
        }
        // Otherwise, _currentTime already reflects exactly what the body
        // consumed (e.g. nested loop plays advance it themselves) — leave
        // it as is instead of adding a duration on top of that.
    }

    public int InterpretPattern(PatternNode pattern)
    {
        var parameters = pattern.Parameters;

        var bpm = _songConfiguration.BPM;
        var beatsPerBar = _songConfiguration.beatsPerBar;

        var sample = parameters.ContainsKey("sample") ? parameters["sample"].AsString() : null;
        Console.WriteLine($">>>>>>>>>>Interpreting Pattern: {pattern.Name} - Sample: {sample} - BPM: {bpm} - BeatsPerBar: {beatsPerBar}");
        // se sample começar com @, então é uma referência a um pattern, e não um sample direto
        if (sample != null && sample.StartsWith("@"))
        {
            var referencedPatternName = sample.Substring(1);
            if (_references.ContainsKey(referencedPatternName))
            {
                var referencedPattern = _references[referencedPatternName];
                sample = referencedPattern.AsString();
            }
            else
            {
                throw new InvalidOperationException($"Referenced pattern '{referencedPatternName}' not found.");
            }
        }
        var grid = parameters.ContainsKey("grid") ? parameters["grid"].AsInt() : 16;
        var free = parameters.ContainsKey("free") ? parameters["free"].AsBoolean() : false;
        var beatMs = _songConfiguration.BeatDurationMs;
        var barMs = _songConfiguration.BarDurationMs;
        var stepMs = barMs / grid;

        if (free)
        {
            return 0; // In the furure
        }

        Console.WriteLine($"Pattern: {pattern.Name} - Sample: {sample} - Grid: {grid} - Steps: {string.Join(", ", pattern.Steps)} - BPM: {bpm}");

        foreach (var statement in pattern.Steps.OrderBy(x => x))
        {
            var startTime = _currentTime + (int)(stepMs * (statement - 1));
            var soundEvent = new SoundEvent(sample ?? "unknown", startTime, 100);
            _interpretResult.Events.Add(soundEvent);
            Console.WriteLine($"Step: {statement} - Sample: {sample} - StartTime: {startTime}ms");
        }

        return (int)_songConfiguration.BarDurationMs;
    }
}
