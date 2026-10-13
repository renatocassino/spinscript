namespace SpinScript.Wasm;

using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using SpinScript.Analysis;
using SpinScript.Compiler;
using SpinScript.Lexer;
using SpinScript.Parser;
using SpinScript.Wasm.Json;

public static partial class Exports
{
    private static readonly JsonSerializerOptions JsonOptions = SpinScriptJson.CreateOptions();

    [JSExport]
    public static string Parse(string source)
    {
        try
        {
            var result = new Parser(source).Parse();
            if (result.HasErrors)
            {
                var error = result.Errors[0];
                return SerializeError(error.Message, error.Line, error.Column);
            }
            return JsonSerializer.Serialize(result.Ast, JsonOptions);
        }
        catch (LexerException ex)
        {
            return SerializeError(ex.Message, ex.Line, ex.Column);
        }
    }

    /// <summary>
    /// Every lexer, parser and semantic diagnostic for <paramref name="source"/>,
    /// as a JSON array of <c>{severity, code, message, line, column}</c>
    /// (0-indexed positions). Empty array when the source is clean.
    /// </summary>
    [JSExport]
    public static string Check(string source) =>
        JsonSerializer.Serialize(SemanticAnalyzer.Check(source).Select(ToJson), JsonOptions);

    [JSExport]
    public static string Compile(string source)
    {
        try
        {
            var compiler = new SpinScript.Compiler.Compiler(source);
            compiler.Compile();
            return JsonSerializer.Serialize(compiler.compileResult, JsonOptions);
        }
        catch (LexerException ex)
        {
            return SerializeError(ex.Message, ex.Line, ex.Column);
        }
        catch (CompilerException ex)
        {
            var error = ex.Errors[0];
            return JsonSerializer.Serialize(new
            {
                error = error.ToString(),
                line = error.Line,
                column = error.Column,
                diagnostics = ex.Diagnostics.Select(ToJson),
            }, JsonOptions);
        }
        catch (InvalidOperationException ex)
        {
            return SerializeError(ex.Message);
        }
    }

    private static object ToJson(Diagnostic diagnostic) => new
    {
        severity = diagnostic.IsError ? "error" : "warning",
        code = diagnostic.Code,
        message = diagnostic.Message,
        line = diagnostic.Line,
        column = diagnostic.Column,
    };

    private static string SerializeError(string message, int? line = null, int? column = null) =>
        JsonSerializer.Serialize(new { error = message, line, column }, JsonOptions);
}
