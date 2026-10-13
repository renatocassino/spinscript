namespace SpinScript.Compiler;

using SpinScript.Analysis;

public class CompilerException : Exception
{
    /// <summary>Every diagnostic found, warnings included.</summary>
    public IReadOnlyList<Diagnostic> Diagnostics { get; }

    /// <summary>Only the diagnostics that stopped compilation.</summary>
    public IReadOnlyList<Diagnostic> Errors { get; }

    public CompilerException(IReadOnlyList<Diagnostic> diagnostics)
        : base(BuildMessage(diagnostics))
    {
        Diagnostics = diagnostics;
        Errors = diagnostics.Where(diagnostic => diagnostic.IsError).ToList();
    }

    private static string BuildMessage(IReadOnlyList<Diagnostic> diagnostics)
    {
        var errors = diagnostics.Where(diagnostic => diagnostic.IsError).ToList();
        var details = string.Join(Environment.NewLine, errors.Select(error => $"- {error}"));
        return $"Compilation failed with {errors.Count} error(s):{Environment.NewLine}{details}";
    }
}
