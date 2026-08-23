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
        Assert.Equal(129, assignment.Value.AsNumber());

        var assignment2 = Assert.IsType<AssignmentNode>(program.Statements[1]);
        Assert.Equal("steps", assignment2.Name);
        Assert.Equal(3, assignment2.Value.AsNumber());

        var assignment3 = Assert.IsType<AssignmentNode>(program.Statements[2]);
        Assert.Equal("volume", assignment3.Name);
        Assert.Equal(120, assignment3.Value.AsNumber());
    }

    [Fact]
    public void ParseLoopRecursive()
    {
        var program = new Parser("""
loop @intro {
    loop @firstPhase {
        loop @instrumental {
            @introMidi = "/intro.mid";

            play @firstPhase (bpm=120, volume=80);
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
        Assert.Equal("/intro.mid", playStatement.Value.AsString());

        var playStatement2 = Assert.IsType<PlayNode>(instrumentalLoop.Statements[1]);
        Assert.Equal("firstPhase", playStatement2.PatternName);
        Assert.Equal(120, playStatement2.Parameters["bpm"].AsInt());
        Assert.Equal(80, playStatement2.Parameters["volume"].AsInt());
    }

    [Fact]
    public void ParsePlay()
    {
        var program = new Parser("""
play @song1 (bpm=120, volume=80); 
""").Parse();

        var play = Assert.IsType<PlayNode>(program.Statements[0]);
        Assert.Equal("song1", play.PatternName);

        var parameter = play.Parameters;
        Assert.Equal(120, parameter["bpm"].AsInt());
        Assert.Equal(80, parameter["volume"].AsInt());
    }

    [Theory]
    [InlineData("loop @iterations {}", "iterations", 0)]
    [InlineData("loop @iterations { @bpm = 120; }", "iterations", 1)]
    [InlineData("loop @iterations { beat @kick (grid=16) { 9 }; }", "iterations", 1)]
    [InlineData("loop @iterations { @bpm = 120; beat @kick (grid=16) { 9 }; }", "iterations", 2)]
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
        var program = new Parser("beat @kick (grid=16) { 9 };\nbeat @hats (grid=16) { 3, 7, 11, 15 };").Parse();

        var beat = Assert.IsType<BeatNode>(program.Statements[0]);

        Assert.Equal("kick", beat.Name);
        Assert.Equal(16, beat.Parameters["grid"].AsInt());
        Assert.Equal([9], beat.Steps);

        var beat2 = Assert.IsType<BeatNode>(program.Statements[1]);

        Assert.Equal("hats", beat2.Name);
        Assert.Equal(16, beat2.Parameters["grid"].AsInt());
        Assert.Equal([3, 7, 11, 15], beat2.Steps);
    }

    [Fact]
    public void ParsePatternReturnPatternGridWithSameContract()
    {
        var program = new Parser("beat @kick (grid=16) { x...|.x..|..x.|x..x };\nbeat @hats (grid=16) { 0, 5, 10, 12, 15 };").Parse();

        var beat = Assert.IsType<BeatNode>(program.Statements[0]);

        Assert.Equal("kick", beat.Name);
        Assert.Equal(16, beat.Parameters["grid"].AsInt());
        Assert.Equal([0, 5, 10, 12, 15], beat.Steps);

        var beat2 = Assert.IsType<BeatNode>(program.Statements[1]);

        Assert.Equal("hats", beat2.Name);
        Assert.Equal(16, beat2.Parameters["grid"].AsInt());
        Assert.Equal([0, 5, 10, 12, 15], beat2.Steps);
    } 

    [Fact]
    public void ParseMelodyWithoutTrailingCommaReturnsAllNotes()
    {
        var program = new Parser("melody @lead { E4 1/4 0, G4 1/4 1/4 };").Parse();

        var melody = Assert.IsType<MelodyNode>(program.Statements[0]);

        Assert.Equal("lead", melody.Name);
        Assert.Equal(2, melody.Notes.Count);
        Assert.Equal("E4", melody.Notes[0].NoteName);
        Assert.Equal("G4", melody.Notes[1].NoteName);
    }

    [Fact]
    public void ParseMelodyWithTrailingCommaReturnsAllNotes()
    {
        var program = new Parser("melody @lead { E4 1/4 0, G4 1/4 1/4, };").Parse();

        var melody = Assert.IsType<MelodyNode>(program.Statements[0]);

        Assert.Equal("lead", melody.Name);
        Assert.Equal(2, melody.Notes.Count);
        Assert.Equal("E4", melody.Notes[0].NoteName);
        Assert.Equal("G4", melody.Notes[1].NoteName);
    }
}
