namespace SpinScript.Tests.src.Parser;

using SpinScript.Parser;
using SpinScript.Parser.Ast;
using SpinScript.Lexer;
using Xunit;

public class ParserTests
{
    [Fact]
    public void ParserRunnerExample() {
        var p = new Parser("@bpm = 129;");
        p.Parse();
    }

    [Fact]
    public void ParseAssignmentReturnsAssignmentNode()
    {
        var program = new Parser("@bpm = 129; @steps=3; @volume=120;").Parse();

        var assignment = Assert.IsType<AssignmentNode>(program.Statements[0]);

        Assert.Equal("bpm", assignment.Name);
        Assert.Equal("129", assignment.Value);

        var assignment2 = Assert.IsType<AssignmentNode>(program.Statements[1]);
        Assert.Equal("steps", assignment2.Name);
        Assert.Equal("3", assignment2.Value);

        var assignment3 = Assert.IsType<AssignmentNode>(program.Statements[2]);
        Assert.Equal("volume", assignment3.Name);
        Assert.Equal("120", assignment3.Value);
    }
}
