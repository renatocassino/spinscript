namespace SpinScript.Analysis;

using SpinScript.Lexer;
using SpinScript.Parser;

public enum DiagnosticSeverity
{
    Error,
    Warning,
}

/// <summary>
/// One problem found in a SpinScript source, from any stage (lexer, parser
/// or semantic analysis). <see cref="Line"/>/<see cref="Column"/> are
/// 0-indexed, like everywhere else in Core; <see cref="ToString"/> reports
/// them 1-indexed. <see cref="Code"/> is a stable kebab-case identifier
/// (e.g. <c>unknown-reference</c>) meant for tests and tooling, so they
/// don't have to match on the human-readable <see cref="Message"/>.
/// </summary>
public record Diagnostic(DiagnosticSeverity Severity, string Code, string Message, int Line, int Column)
{
    public bool IsError => Severity == DiagnosticSeverity.Error;

    public static Diagnostic Error(string code, string message, int line, int column) =>
        new(DiagnosticSeverity.Error, code, message, line, column);

    public static Diagnostic Warning(string code, string message, int line, int column) =>
        new(DiagnosticSeverity.Warning, code, message, line, column);

    public static Diagnostic FromParserException(ParserException exception) =>
        Error("syntax-error", exception.Reason, exception.Line, exception.Column);

    public static Diagnostic FromLexerException(LexerException exception) =>
        Error("lexer-error", exception.Reason, exception.Line, exception.Column);

    /// <summary>The message followed by its 1-indexed position.</summary>
    public string Describe() => $"{Message} (line {Line + 1}, column {Column + 1})";

    public override string ToString() => $"{(IsError ? "error" : "warning")}: {Describe()}";
}
