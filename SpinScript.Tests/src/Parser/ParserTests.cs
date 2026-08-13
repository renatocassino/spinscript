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

    [Fact]
    public void ParseLoopRecursive()
    {
        var program = new Parser("""
loop @intro {
    loop @firstPhase {
        loop @instrumental {
            @introMidi = "/intro.mid";
        }
    }
}
""").Parse();

        var loop = Assert.IsType<LoopNode>(program.Statements[0]);
        Assert.Equal("intro", loop.Name);
        Assert.Single(loop.Statements);

        var firstPhaseLoop = Assert.IsType<LoopNode>(loop.Statements[0]);
        Assert.Equal("firstPhase", firstPhaseLoop.Name);
        Assert.Single(firstPhaseLoop.Statements);

        var instrumentalLoop = Assert.IsType<LoopNode>(firstPhaseLoop.Statements[0]);
        Assert.Equal("instrumental", instrumentalLoop.Name);

        var playStatement = Assert.IsType<AssignmentNode>(instrumentalLoop.Statements[0]);
        Assert.Equal("introMidi", playStatement.Name);
        Assert.Equal("/intro.mid", playStatement.Value);
    }

    [Theory]
    [InlineData("loop @iterations {}", "iterations", 0)]
    [InlineData("loop @iterations { @bpm = 120; }", "iterations", 1)]
    [InlineData("loop @iterations { pattern @kick (grid=16) { 9 }; }", "iterations", 1)]
    [InlineData("loop @iterations { @bpm = 120; pattern @kick (grid=16) { 9 }; }", "iterations", 2)]
    public void ParseLoopReturnsLoopNodeWithExpectedStatements(string input, string expectedName, int expectedStatementCount)
    {
        var program = new Parser(input).Parse();

        var loop = Assert.IsType<LoopNode>(program.Statements[0]);
        Assert.Equal(expectedName, loop.Name);
        Assert.Equal(expectedStatementCount, loop.Statements.Count);
    }

    [Theory]
    [InlineData("loop @iterations { 9; }")]
    [InlineData("loop @iterations {")]
    [InlineData("loop { }")]
    public void ParseLoopWithInvalidBodyThrows(string input)
    {
        Assert.Throws<ParserException>(() => new Parser(input).Parse());
    }

   [Fact]
    public void ParsePatternReturnNode()
    {
        var program = new Parser("pattern @kick (grid=16) { 9 };\npattern @hats (grid=16) { 3, 7, 11, 15 };").Parse();

        var pattern = Assert.IsType<PatternNode>(program.Statements[0]);

        Assert.Equal("kick", pattern.Name);
        Assert.Equal("16", pattern.Parameters["grid"]);
        Assert.Equal(["9"], pattern.Steps);

        var pattern2 = Assert.IsType<PatternNode>(program.Statements[1]);

        Assert.Equal("hats", pattern2.Name);
        Assert.Equal("16", pattern2.Parameters["grid"]);
        Assert.Equal(["3", "7", "11", "15"], pattern2.Steps);
    }
}
