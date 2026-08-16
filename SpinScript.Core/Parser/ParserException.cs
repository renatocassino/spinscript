namespace SpinScript.Parser;

public class ParserException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public ParserException(string message, int line, int column) : base($"{message} (line {line + 1}, column {column + 1})")
    {
        Line = line;
        Column = column;
    }
}
