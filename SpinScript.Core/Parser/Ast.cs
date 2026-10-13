namespace SpinScript.Parser.Ast;

using System.Globalization;

public abstract record AstNode;

public record ProgramNode(List<AstNode> Statements) : AstNode;

public record Note(string NoteName, string FractionStart, string FractionDuration, Dictionary<string, SpinValue> Parameters, int Line = 0, int Column = 0);

public record SpinValue
{
    public record StringValue(string Value) : SpinValue;
    public record NumberValue(double Value) : SpinValue;
    public record BooleanValue(bool Value) : SpinValue;
    // `sample=@kick` — points at a variable declared elsewhere; the compiler
    // resolves it, the parser only records the name (without the '@').
    public record ReferenceValue(string Name) : SpinValue;

    // sealed: otherwise each nested record synthesizes its own
    // "StringValue { Value = ... }" ToString and this one is never used.
    public sealed override string ToString()
    {
        return this switch
        {
            StringValue s => $"\"{s.Value}\"",
            NumberValue n => n.Value.ToString(CultureInfo.InvariantCulture),
            BooleanValue b => b.Value ? "true" : "false",
            ReferenceValue r => $"@{r.Name}",
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
public record MelodyNode(string Name, Dictionary<string, SpinValue> Parameters, List<Note> Notes, int Line, int Column) : AstNode;

public record LoopNode(string Name, Dictionary<string, SpinValue> Parameters, List<AstNode> Statements, int Line, int Column) : AstNode;

public record PlayNode(string PatternName, Dictionary<string, SpinValue> Parameters, int Line, int Column) : AstNode;

public record SongNode(Dictionary<string, SpinValue> Parameters, List<AstNode> Statements, int Line, int Column) : AstNode;
