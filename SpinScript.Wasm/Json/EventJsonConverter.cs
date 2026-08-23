namespace SpinScript.Wasm.Json;

using System.Text.Json;
using System.Text.Json.Serialization;
using SpinScript.Interpreter;

public sealed class EventJsonConverter : JsonConverter<Event>
{
    public override Event Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("Reading Event from JSON is not supported.");

    public override void Write(Utf8JsonWriter writer, Event value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case SoundEvent soundEvent:
                writer.WriteString("type", "sound");
                writer.WriteString("sample", soundEvent.Sample);
                writer.WriteNumber("time", soundEvent.Time);
                writer.WriteNumber("velocity", soundEvent.Velocity);
                break;
            case MelodyEvent melodyEvent:
                writer.WriteString("type", "melody");
                writer.WriteString("sample", melodyEvent.Sample);
                writer.WriteNumber("time", melodyEvent.Time);
                writer.WriteNumber("duration", melodyEvent.Duration);
                writer.WriteString("note", melodyEvent.Note);
                writer.WriteNumber("rate", melodyEvent.rate);
                break;
            default:
                throw new NotSupportedException($"Unknown Event type: {value.GetType().Name}");
        }

        writer.WriteEndObject();
    }
}
