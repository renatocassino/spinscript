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
        InterpretStatements();
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

                for (var i = 0; i < repeatCount; i++)
                {
                    Console.WriteLine($"Play: {play.PatternName} (repeat {i + 1}/{repeatCount})");
                    if (isLoop)
                    {
                        Console.WriteLine($"Play Loop: {play.PatternName}");
                        InterpretStatement(_loops[play.PatternName]);
                    }
                    else if (isPattern)
                    {
                        Console.WriteLine($"Play Pattern: {play.PatternName}");
                        InterpretStatement(_patterns[play.PatternName]);
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
        Console.WriteLine($"Interpreting loop: {loop.Name}");
        foreach (var statement in loop.Statements)
        {
            InterpretStatement(statement);
        }
    }

    public void InterpretPattern(PatternNode pattern)
    {
        Console.WriteLine($"Interpreting pattern: {pattern.Name} - WE MUST ADD NOTES HERE!!!!!");
        foreach (var statement in pattern.Steps)
        {
            Console.WriteLine($"Step: {statement}");
        }
    }
}
