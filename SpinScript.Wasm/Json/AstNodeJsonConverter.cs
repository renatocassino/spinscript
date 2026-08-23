namespace SpinScript.Wasm.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using SpinScript.Parser.Ast;

public sealed class AstNodeJsonConverter : JsonConverter<AstNode>
{
    public override AstNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("Reading AstNode from JSON is not supported.");

    public override void Write(Utf8JsonWriter writer, AstNode value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case ProgramNode program:
                writer.WriteString("type", "program");
                WriteStatements(writer, "statements", program.Statements, options);
                break;
            case AssignmentNode assignment:
                writer.WriteString("type", "assignment");
                writer.WriteString("name", assignment.Name);
                writer.WritePropertyName("value");
                JsonSerializer.Serialize(writer, assignment.Value, options);
                WriteLocation(writer, assignment.Line, assignment.Column);
                break;
            case BeatNode pattern:
                writer.WriteString("type", "pattern");
                writer.WriteString("name", pattern.Name);
                WriteParameters(writer, "parameters", pattern.Parameters, options);
                writer.WritePropertyName("steps");
                JsonSerializer.Serialize(writer, pattern.Steps, options);
                WriteLocation(writer, pattern.Line, pattern.Column);
                break;
            case MelodyNode melody:
                writer.WriteString("type", "melody");
                writer.WriteString("name", melody.Name);
                WriteParameters(writer, "parameters", melody.Parameters, options);
                writer.WritePropertyName("notes");
                JsonSerializer.Serialize(writer, melody.Notes, options);
                WriteLocation(writer, melody.Line, melody.Column);
                break;
            case LoopNode loop:
                writer.WriteString("type", "loop");
                writer.WriteString("name", loop.Name);
                WriteParameters(writer, "parameters", loop.Parameters, options);
                WriteStatements(writer, "statements", loop.Statements, options);
                WriteLocation(writer, loop.Line, loop.Column);
                break;
            case PlayNode play:
                writer.WriteString("type", "play");
                writer.WriteString("patternName", play.PatternName);
                WriteParameters(writer, "parameters", play.Parameters, options);
                WriteLocation(writer, play.Line, play.Column);
                break;
            case SongNode song:
                writer.WriteString("type", "song");
                WriteStatements(writer, "statements", song.Statements, options);
                WriteLocation(writer, song.Line, song.Column);
                break;
            default:
                throw new NotSupportedException($"Unknown AstNode type: {value.GetType().Name}");
        }

        writer.WriteEndObject();
    }

    private static void WriteLocation(Utf8JsonWriter writer, int line, int column)
    {
        writer.WriteNumber("line", line);
        writer.WriteNumber("column", column);
    }

    private static void WriteStatements(Utf8JsonWriter writer, string propertyName, List<AstNode> statements, JsonSerializerOptions options)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();

        foreach (var statement in statements)
        {
            JsonSerializer.Serialize(writer, statement, options);
        }

        writer.WriteEndArray();
    }

    private static void WriteParameters(Utf8JsonWriter writer, string propertyName, Dictionary<string, SpinValue> parameters, JsonSerializerOptions options)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartObject();

        foreach (var (key, value) in parameters)
        {
            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, value, options);
        }

        writer.WriteEndObject();
    }
}
