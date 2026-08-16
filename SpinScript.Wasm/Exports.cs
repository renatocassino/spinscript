namespace SpinScript.Wasm;

using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using SpinScript.Interpreter;
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
            var program = new Parser(source).Parse();
            return JsonSerializer.Serialize(program, JsonOptions);
        }
        catch (LexerException ex)
        {
            return SerializeError(ex.Message, ex.Line, ex.Column);
        }
        catch (ParserException ex)
        {
            return SerializeError(ex.Message, ex.Line, ex.Column);
        }
    }

    [JSExport]
    public static string Interpret(string source)
    {
        try
        {
            var interpreter = new SpinScript.Interpreter.Interpreter(source);
            interpreter.Interpret();
            return JsonSerializer.Serialize(interpreter.interpretResult, JsonOptions);
        }
        catch (LexerException ex)
        {
            return SerializeError(ex.Message, ex.Line, ex.Column);
        }
        catch (ParserException ex)
        {
            return SerializeError(ex.Message, ex.Line, ex.Column);
        }
        catch (InvalidOperationException ex)
        {
            return SerializeError(ex.Message);
        }
    }

    private static string SerializeError(string message, int? line = null, int? column = null) =>
        JsonSerializer.Serialize(new { error = message, line, column }, JsonOptions);
}
