namespace SpinScript.Wasm.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using SpinScript.Parser.Ast;

public sealed class SpinValueJsonConverter : JsonConverter<SpinValue>
{
    public override SpinValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("Reading SpinValue from JSON is not supported.");

    public override void Write(Utf8JsonWriter writer, SpinValue value, JsonSerializerOptions options)
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
            default:
                throw new NotSupportedException($"Unknown SpinValue type: {value.GetType().Name}");
        }

        writer.WriteEndObject();
    }
}
