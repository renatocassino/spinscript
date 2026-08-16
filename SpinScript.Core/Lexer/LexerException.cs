namespace SpinScript.Lexer;

public class LexerException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public LexerException(string message, int line, int column)
        : base($"{message} (line {line + 1}, column {column + 1})")
    {
        Line = line;
        Column = column;
    }
}
