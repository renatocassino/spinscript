namespace SpinScript.Wasm.Json;

using System.Text.Json;

public static class SpinScriptJson
{
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        options.Converters.Add(new AstNodeJsonConverter());
        options.Converters.Add(new SpinValueJsonConverter());
        options.Converters.Add(new EventJsonConverter());

        return options;
    }
}
