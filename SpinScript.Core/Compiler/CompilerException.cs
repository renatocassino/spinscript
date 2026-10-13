namespace SpinScript.Compiler;

using SpinScript.Parser;

/// <summary>
/// Thrown by <see cref="Compiler.Compile"/>. Two flavours:
/// <list type="bullet">
/// <item>the source didn't parse — <see cref="Errors"/> holds every syntax
/// error the parser collected (each with its own line/column);</item>
/// <item>the source parsed but doesn't make sense (unknown reference, a step
/// outside the grid, a loop that plays itself, ...) — <see cref="Errors"/>
/// is empty and <see cref="Line"/>/<see cref="Column"/> point at the
/// offending node.</item>
/// </list>
/// </summary>
public class CompilerException : Exception
{
    public IReadOnlyList<ParserException> Errors { get; }

    public int? Line { get; }
    public int? Column { get; }

    public CompilerException(IReadOnlyList<ParserException> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
        Line = errors.Count > 0 ? errors[0].Line : null;
        Column = errors.Count > 0 ? errors[0].Column : null;
    }

    public CompilerException(string message, int line, int column)
        : base($"{message} (line {line + 1}, column {column + 1})")
    {
        Errors = [];
        Line = line;
        Column = column;
    }

    private static string BuildMessage(IReadOnlyList<ParserException> errors)
    {
        var details = string.Join(Environment.NewLine, errors.Select(error => $"- {error.Message}"));
        return $"Parsing failed with {errors.Count} error(s):{Environment.NewLine}{details}";
    }
}
