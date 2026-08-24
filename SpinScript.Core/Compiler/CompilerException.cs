namespace SpinScript.Compiler;

using SpinScript.Parser;

public class CompilerException : Exception
{
    public IReadOnlyList<ParserException> Errors { get; }

    public CompilerException(IReadOnlyList<ParserException> errors)
        : base(BuildMessage(errors))
    {
        Errors = errors;
    }

    private static string BuildMessage(IReadOnlyList<ParserException> errors)
    {
        var details = string.Join(Environment.NewLine, errors.Select(error => $"- {error.Message}"));
        return $"Parsing failed with {errors.Count} error(s):{Environment.NewLine}{details}";
    }
}
