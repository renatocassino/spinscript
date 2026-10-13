namespace SpinScript.Lexer;

public class LexerException : Exception
{
    public int Line { get; }
    public int Column { get; }

    /// <summary>The error message without the "(line X, column Y)" suffix.</summary>
    public string Reason { get; }

    public LexerException(string message, int line, int column)
        : base($"{message} (line {line + 1}, column {column + 1})")
    {
        Line = line;
        Column = column;
        Reason = message;
    }
}
