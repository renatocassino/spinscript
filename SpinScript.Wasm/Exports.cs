namespace SpinScript.Wasm;

using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
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
            return SerializeError(error.Message, error.Line, error.Column);
        }
        catch (InvalidOperationException ex)
        {
            return SerializeError(ex.Message);
        }
    }

    private static string SerializeError(string message, int? line = null, int? column = null) =>
        JsonSerializer.Serialize(new { error = message, line, column }, JsonOptions);
}
