namespace SpinScript.Interpreter;

using SpinScript.Parser;
using SpinScript.Parser.Ast;

public class Interpreter
{
    private readonly string _input;
    private readonly Parser _parser;

    private Dictionary<string, SpinValue> _references = new Dictionary<string, SpinValue>();
    private Dictionary<string, BeatNode> _beats = new Dictionary<string, BeatNode>();
    private Dictionary<string, MelodyNode> _melodies = new Dictionary<string, MelodyNode>();
    private Dictionary<string, LoopNode> _loops = new Dictionary<string, LoopNode>();
    private SongNode? song;

    private InterpretResult _interpretResult = new InterpretResult(new List<Event>());

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

        _interpretResult = new InterpretResult(new List<Event>());

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
            case BeatNode beat:
                Console.WriteLine($"Beat: {beat.Name}");
                InterpretBeat(beat);
                break;
            case MelodyNode melody:
                Console.WriteLine($"Melody: {melody.Name}");
                InterpretMelody(melody);
                break;
            case LoopNode loop:
                Console.WriteLine($"Loop: {loop.Name}");
                InterpretLoop(loop);
                break;
            case PlayNode play:
                var isLoop = _loops.ContainsKey(play.PatternName);
                var isBeat = _beats.ContainsKey(play.PatternName);
                var isMelody = _melodies.ContainsKey(play.PatternName);
                var parameters = play.Parameters;

                var repeatCount = 1;
                if (parameters.ContainsKey("repeat"))
                {
                    repeatCount = parameters["repeat"].AsInt();
                }

                AstNode beatOrLoop;
                if (isLoop)
                {
                    beatOrLoop = _loops[play.PatternName];
                } else if (isBeat)
                {
                    beatOrLoop = _beats[play.PatternName];
                } else if (isMelody)
                {
                    beatOrLoop = _melodies[play.PatternName];
                } else
                {
                    throw new InvalidOperationException($"PlayNode references unknown beat or loop: {play.PatternName}");
                }

                for (var i = 0; i < repeatCount; i++)
                {
                    Console.WriteLine($"Play: {play.PatternName} (repeat {i + 1}/{repeatCount})");
                    if (isLoop || isBeat || isMelody)
                    {
                        Console.WriteLine($"Play Loop/Beat/Melody: {play.PatternName}");
                        InterpretStatement(beatOrLoop);
                        if (isBeat)
                        {
                            // A repeated bare pattern behaves like its own
                            // 1-bar loop: each repetition lands one bar
                            // after the previous one instead of every
                            // repeat stacking on the exact same bar.
                            // Loops already advance _currentTime on their
                            // own each time through InterpretLoop, so this
                            // only applies to plain patterns.
                            _currentTime += (int)_songConfiguration.BarDurationMs;
                        }
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

            InterpretStatement(statement);

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
            _currentTime = startTime + explicitBars.Value * (int)_songConfiguration.BarDurationMs;
        }
        else if (furthestTime == startTime)
        {
            // Nothing in the body advanced time on its own — default to a
            // single bar.
            _currentTime = startTime + (int)_songConfiguration.BarDurationMs;
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

    public int InterpretMelody(MelodyNode melody)
    {
        var parameters = melody.Parameters;

        var sample = parameters.ContainsKey("sample") ? parameters["sample"].AsString() : null;
        if (sample != null && sample.StartsWith("@"))
        {
            var referencedBeatName = sample.Substring(1);
            if (_references.ContainsKey(referencedBeatName))
            {
                var referencedPattern = _references[referencedBeatName];
                sample = referencedPattern.AsString();
            }
            else
            {
                throw new InvalidOperationException($"Referenced beat '{referencedBeatName}' not found.");
            }
        }

        var barMs = _songConfiguration.BarDurationMs;
        Console.WriteLine($"[DEBUG] - {barMs} - {melody.Notes}");

        foreach (var note in melody.Notes)
        {
            var noteNotation = note.NoteName;
            var startTimeNotation = fractionToTime(note.FractionStart);
            var durationTimeNotation = startTimeNotation + fractionToTime(note.FractionDuration);
            // Add parameters in the future

            // Calc metrics here
            Console.WriteLine($"Add note {noteNotation} - {startTimeNotation}-{durationTimeNotation}");
            var melodyEvent = new MelodyEvent(sample ?? "unknown", startTimeNotation, startTimeNotation + durationTimeNotation, noteNotation);
            _interpretResult.Events.Add(melodyEvent);
        }

        return 0;
    }

    public double fractionToTime(string fraction)
    {
        if (fraction.Contains("/"))
        {
            var nums = fraction.Split('/');

            if (double.TryParse(nums[0], out double n1) && double.TryParse(nums[1], out double n2))
            {
                return n1 / n2 * _songConfiguration.BarDurationMs;
            }
        }

        if (int.TryParse(fraction, out var f)) {
            return _songConfiguration.BarDurationMs * f;
        }

        throw new InvalidOperationException($"Invalid time notation: '{fraction}'. Expected a fraction like '1/4' or a plain integer.");
    }

    public int InterpretBeat(BeatNode beat)
    {
        var parameters = beat.Parameters;

        var bpm = _songConfiguration.BPM;
        var beatsPerBar = _songConfiguration.beatsPerBar;

        var sample = parameters.ContainsKey("sample") ? parameters["sample"].AsString() : null;
        Console.WriteLine($">>>>>>>>>>Interpreting Pattern: {beat.Name} - Sample: {sample} - BPM: {bpm} - BeatsPerBar: {beatsPerBar}");
        // se sample começar com @, então é uma referência a um pattern, e não um sample direto
        if (sample != null && sample.StartsWith("@"))
        {
            var referencedBeatName = sample.Substring(1);
            if (_references.ContainsKey(referencedBeatName))
            {
                var referencedPattern = _references[referencedBeatName];
                sample = referencedPattern.AsString();
            }
            else
            {
                throw new InvalidOperationException($"Referenced beat '{referencedBeatName}' not found.");
            }
        }
        var grid = parameters.ContainsKey("grid") ? parameters["grid"].AsInt() : 16;
        var beatMs = _songConfiguration.BeatDurationMs;
        var barMs = _songConfiguration.BarDurationMs;
        var stepMs = barMs / grid;

        Console.WriteLine($"Beat: {beat.Name} - Sample: {sample} - Grid: {grid} - Steps: {string.Join(", ", beat.Steps)} - BPM: {bpm}");

        foreach (var statement in beat.Steps.OrderBy(x => x))
        {
            var startTime = _currentTime + (int)(stepMs * (statement - 1));
            var soundEvent = new SoundEvent(sample ?? "unknown", startTime, 100);
            _interpretResult.Events.Add(soundEvent);
            Console.WriteLine($"Step: {statement} - Sample: {sample} - StartTime: {startTime}ms");
        }

        return (int)_songConfiguration.BarDurationMs;
    }
}
