namespace SpinScript.Compiler;

using SpinScript.Analysis;

/// <summary>
/// Thrown by <see cref="Compiler.Compile"/> when the source can't become a
/// timeline: it didn't parse, the semantic analyzer found errors, or (as a
/// backstop) the compiler itself hit something it can't compile.
/// <see cref="Diagnostics"/> holds everything that was found, warnings
/// included; <see cref="Line"/>/<see cref="Column"/> point at the first error.
/// </summary>
public class CompilerException : Exception
{
    /// <summary>Every diagnostic found, warnings included.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Only the diagnostics that stopped compilation.</summary>
    public IReadOnlyList<Diagnostic> Errors { get; }

    public int? Line => Errors.Count > 0 ? Errors[0].Line : null;
    public int? Column => Errors.Count > 0 ? Errors[0].Column : null;

    public CompilerException(IReadOnlyList<Diagnostic> diagnostics)
        : base(BuildMessage(diagnostics))
    {
        Diagnostics = diagnostics;
        Errors = diagnostics.Where(diagnostic => diagnostic.IsError).ToList();
    }

    public CompilerException(string message, int line, int column)
        : this([Diagnostic.Error("compile-error", message, line, column)])
    {
    }

    private static string BuildMessage(IReadOnlyList<Diagnostic> diagnostics)
    {
        var errors = diagnostics.Where(diagnostic => diagnostic.IsError).ToList();
        if (errors.Count == 1)
        {
            return errors[0].Describe();
        }

        var details = string.Join(Environment.NewLine, errors.Select(error => $"- {error.Describe()}"));
        return $"Compilation failed with {errors.Count} error(s):{Environment.NewLine}{details}";
    }
}
