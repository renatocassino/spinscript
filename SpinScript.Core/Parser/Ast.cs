namespace SpinScript.Parser.Ast;

public abstract record AstNode;

public record ProgramNode(List<AstNode> Statements) : AstNode;

public record Note(string note, int startTime, int duration, Dictionary<string, SpinValue> Parameters);

public record StepOrMelody
{
    public record StepsValue(List<int> steps) : StepOrMelody;
    public record MelodyValue(List<Note> melody) : StepOrMelody;
}

public record SpinValue
{
    public record StringValue(string Value) : SpinValue;
    public record NumberValue(double Value) : SpinValue;
    public record BooleanValue(bool Value) : SpinValue;

    public override string ToString()
    {
        return this switch
        {
            StringValue s => $"\"{s.Value}\"",
            NumberValue n => n.Value.ToString(),
            BooleanValue b => b.Value.ToString(),
            _ => throw new InvalidOperationException("Unknown SpinValue type")
        };
    }

    public double AsNumber() => this is NumberValue n
        ? n.Value
        : throw new InvalidOperationException($"Expected NumberValue, got {GetType().Name}");


    public int AsInt() => this is NumberValue n
        ? (int)n.Value
        : throw new InvalidOperationException($"Expected NumberValue, got {GetType().Name}");

    public string AsString() => this is StringValue s
        ? s.Value
        : throw new InvalidOperationException($"Expected StringValue, got {GetType().Name}");

    public bool AsBoolean() => this is BooleanValue b
        ? b.Value
        : throw new InvalidOperationException($"Expected BooleanValue, got {GetType().Name}");
}

public record AssignmentNode(string Name, SpinValue Value, int Line, int Column) : AstNode;

public record BeatNode(string Name, Dictionary<string, SpinValue> Parameters, List<int> Steps, int Line, int Column) : AstNode;

public record LoopNode(string Name, Dictionary<string, SpinValue> Parameters, List<AstNode> Statements, int Line, int Column) : AstNode;

public record PlayNode(string PatternName, Dictionary<string, SpinValue> Parameters, int Line, int Column) : AstNode;

public record SongNode(List<AstNode> Statements, int Line, int Column) : AstNode;
