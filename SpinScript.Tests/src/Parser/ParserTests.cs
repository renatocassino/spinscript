namespace SpinScript.Tests.src.Parser;

using SpinScript.Parser;
using SpinScript.Lexer;
using Xunit;

public class ParserTests
{
    [Fact]
    public void ParserRunnerExample() {
        var p = new Parser("@bpm = 129;");
        p.Parse();
    }
}
