namespace SpinScript.Json;

using System.Text;
using System.Text.Json;
using SpinScript.Compiler;
using SpinScript.Lexer;
using SpinScript.Parser;
using SpinScript.Parser.Ast;

/// <summary>
/// JSON front door used by the WebAssembly build (and anything else that
/// wants the pipeline as plain strings). Never throws: every failure comes
/// back as an error object.
///
/// Everything is written by hand with <see cref="Utf8JsonWriter"/> instead of
/// <c>JsonSerializer.Serialize(obj)</c> on purpose: the reflection-based
/// serializer breaks once the WASM build is trimmed for publishing (the
/// trimmer strips the metadata it relies on), while a hand-written writer has
/// nothing to strip.
///
/// Output shapes:
/// <code>
/// Parse   → { "type": "program", "statements": [ ... ] }
/// Compile → { "events": [ { "type": "sound", "sample", "time", "velocity" }
///                       | { "type": "melody", "sample", "time", "duration", "note", "rate" } ],
///             "duration": totalMs }
/// error   → { "error": "first message", "line": 0, "column": 4,
///             "errors": [ { "message", "line", "column" }, ... ] }
/// </code>
/// Lines and columns are 0-indexed; the messages already spell them out
/// 1-indexed for humans.
/// </summary>
public static class SpinScriptJson
{
    public static string Parse(string source)
    {
        try
        {
            var result = new Parser(source).Parse();
            if (result.HasErrors)
            {
                return Errors(result.Errors.Select(e => (e.Message, (int?)e.Line, (int?)e.Column)));
            }

            return Write(writer => WriteNode(writer, result.Ast));
        }
        catch (LexerException ex)
        {
            return Errors([(ex.Message, ex.Line, ex.Column)]);
        }
        catch (Exception ex)
        {
            return Errors([($"Internal error: {ex.Message}", null, null)]);
        }
    }

    public static string Compile(string source)
    {
        try
        {
            var result = new Compiler(source).Compile();
            return Write(writer => WriteCompileResult(writer, result));
        }
        catch (LexerException ex)
        {
            return Errors([(ex.Message, ex.Line, ex.Column)]);
        }
        catch (CompilerException ex) when (ex.Errors.Count > 0)
        {
            return Errors(ex.Errors.Select(e => (e.Message, (int?)e.Line, (int?)e.Column)));
        }
        catch (CompilerException ex)
        {
            return Errors([(ex.Message, ex.Line, ex.Column)]);
        }
        catch (Exception ex)
        {
            return Errors([($"Internal error: {ex.Message}", null, null)]);
        }
    }

    private static string Write(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            write(writer);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string Errors(IEnumerable<(string Message, int? Line, int? Column)> errors)
    {
        var list = errors.ToList();
        return Write(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("error", list[0].Message);
            WriteNullableNumber(writer, "line", list[0].Line);
            WriteNullableNumber(writer, "column", list[0].Column);

            writer.WriteStartArray("errors");
            foreach (var (message, line, column) in list)
            {
                writer.WriteStartObject();
                writer.WriteString("message", message);
                WriteNullableNumber(writer, "line", line);
                WriteNullableNumber(writer, "column", column);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        });
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, string name, int? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(name, value.Value);
        }
        else
        {
            writer.WriteNull(name);
        }
    }

    private static void WriteCompileResult(Utf8JsonWriter writer, CompileResult result)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("events");

        var end = 0.0;
        foreach (var e in result.Events)
        {
            writer.WriteStartObject();
            switch (e)
            {
                case SoundEvent sound:
                    writer.WriteString("type", "sound");
                    writer.WriteString("sample", sound.Sample);
                    writer.WriteNumber("time", sound.Time);
                    writer.WriteNumber("velocity", sound.Velocity);
                    end = Math.Max(end, sound.Time);
                    break;
                case MelodyEvent melody:
                    writer.WriteString("type", "melody");
                    writer.WriteString("sample", melody.Sample);
                    writer.WriteNumber("time", melody.Time);
                    writer.WriteNumber("duration", melody.Duration);
                    writer.WriteString("note", melody.Note);
                    writer.WriteNumber("rate", melody.Rate);
                    end = Math.Max(end, melody.Time + melody.Duration);
                    break;
                default:
                    throw new NotSupportedException($"Unknown Event type: {e.GetType().Name}");
            }
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteNumber("duration", end);
        writer.WriteEndObject();
    }

    private static void WriteNode(Utf8JsonWriter writer, AstNode node)
    {
        writer.WriteStartObject();

        switch (node)
        {
            case ProgramNode program:
                writer.WriteString("type", "program");
                WriteStatements(writer, program.Statements);
                break;
            case AssignmentNode assignment:
                writer.WriteString("type", "assignment");
                writer.WriteString("name", assignment.Name);
                writer.WritePropertyName("value");
                WriteValue(writer, assignment.Value);
                WriteLocation(writer, assignment.Line, assignment.Column);
                break;
            case BeatNode beat:
                writer.WriteString("type", "beat");
                writer.WriteString("name", beat.Name);
                WriteParameters(writer, beat.Parameters);
                writer.WriteStartArray("steps");
                foreach (var step in beat.Steps)
                {
                    writer.WriteNumberValue(step);
                }
                writer.WriteEndArray();
                WriteLocation(writer, beat.Line, beat.Column);
                break;
            case MelodyNode melody:
                writer.WriteString("type", "melody");
                writer.WriteString("name", melody.Name);
                WriteParameters(writer, melody.Parameters);
                writer.WriteStartArray("notes");
                foreach (var note in melody.Notes)
                {
                    writer.WriteStartObject();
                    writer.WriteString("note", note.NoteName);
                    writer.WriteString("start", note.FractionStart);
                    writer.WriteString("duration", note.FractionDuration);
                    WriteParameters(writer, note.Parameters);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                WriteLocation(writer, melody.Line, melody.Column);
                break;
            case LoopNode loop:
                writer.WriteString("type", "loop");
                writer.WriteString("name", loop.Name);
                WriteParameters(writer, loop.Parameters);
                WriteStatements(writer, loop.Statements);
                WriteLocation(writer, loop.Line, loop.Column);
                break;
            case PlayNode play:
                writer.WriteString("type", "play");
                writer.WriteString("patternName", play.PatternName);
                WriteParameters(writer, play.Parameters);
                WriteLocation(writer, play.Line, play.Column);
                break;
            case SongNode song:
                writer.WriteString("type", "song");
                WriteStatements(writer, song.Statements);
                WriteLocation(writer, song.Line, song.Column);
                break;
            default:
                throw new NotSupportedException($"Unknown AstNode type: {node.GetType().Name}");
        }

        writer.WriteEndObject();
    }

    private static void WriteStatements(Utf8JsonWriter writer, List<AstNode> statements)
    {
        writer.WriteStartArray("statements");
        foreach (var statement in statements)
        {
            WriteNode(writer, statement);
        }
        writer.WriteEndArray();
    }

    private static void WriteParameters(Utf8JsonWriter writer, Dictionary<string, SpinValue> parameters)
    {
        writer.WriteStartObject("parameters");
        foreach (var (key, value) in parameters)
        {
            writer.WritePropertyName(key);
            WriteValue(writer, value);
        }
        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, SpinValue value)
    {
        writer.WriteStartObject();
        switch (value)
        {
            case SpinValue.StringValue s:
                writer.WriteString("type", "string");
                writer.WriteString("value", s.Value);
                break;
            case SpinValue.NumberValue n:
                writer.WriteString("type", "number");
                writer.WriteNumber("value", n.Value);
                break;
            case SpinValue.BooleanValue b:
                writer.WriteString("type", "boolean");
                writer.WriteBoolean("value", b.Value);
                break;
            case SpinValue.ReferenceValue r:
                writer.WriteString("type", "reference");
                writer.WriteString("value", r.Name);
                break;
            default:
                throw new NotSupportedException($"Unknown SpinValue type: {value.GetType().Name}");
        }
        writer.WriteEndObject();
    }

    private static void WriteLocation(Utf8JsonWriter writer, int line, int column)
    {
        writer.WriteNumber("line", line);
        writer.WriteNumber("column", column);
    }
}
