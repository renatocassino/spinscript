namespace SpinScript.Wasm;

using System.Runtime.InteropServices.JavaScript;
using SpinScript.Json;

/// <summary>
/// The functions JavaScript can call. All of them return a JSON string and never
/// throw — see <see cref="SpinScriptJson"/> for the shapes.
/// </summary>
public static partial class Exports
{
    [JSExport]
    public static string Parse(string source) => SpinScriptJson.Parse(source);

    [JSExport]
    public static string Check(string source) => SpinScriptJson.Check(source);

    [JSExport]
    public static string Compile(string source) => SpinScriptJson.Compile(source);

    [JSExport]
    public static string CompilePart(string source, string name) => SpinScriptJson.CompilePart(source, name);
}
