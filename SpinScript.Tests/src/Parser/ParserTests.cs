namespace SpinScript.Tests.src.Parser;

using System.Linq;
using SpinScript.Parser;
using SpinScript.Parser.Ast;
using SpinScript.Lexer;
using Xunit;

public class ParserTests
{
    [Fact]
    public void ParserRunnerExample() {
        var p = new Parser("@bpm = 129;");
        var program = p.Parse();

        Assert.False(program.HasErrors);
    }

    [Fact]
    public void ParseAssignmentReturnsAssignmentNode()
    {
        var program = new Parser("@bpm = 129; @steps=3; @volume=120;").Parse();

        Assert.False(program.HasErrors);

        var ast = program.Ast;
        var assignment = Assert.IsType<AssignmentNode>(ast.Statements[0]);

        Assert.Equal("bpm", assignment.Name);
        Assert.Equal(129, assignment.Value.AsNumber());

        var assignment2 = Assert.IsType<AssignmentNode>(ast.Statements[1]);
        Assert.Equal("steps", assignment2.Name);
        Assert.Equal(3, assignment2.Value.AsNumber());

        var assignment3 = Assert.IsType<AssignmentNode>(ast.Statements[2]);
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

        Assert.False(program.HasErrors);

        var loop = Assert.IsType<LoopNode>(program.Ast.Statements[0]);
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

        Assert.False(program.HasErrors);

        var play = Assert.IsType<PlayNode>(program.Ast.Statements[0]);
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

        Assert.False(program.HasErrors);

        var loop = Assert.IsType<LoopNode>(program.Ast.Statements[0]);
        Assert.Equal(expectedName, loop.Name);
        Assert.Equal(expectedStatementCount, loop.Statements.Count);
    }

    [Theory]
    [InlineData("loop @iterations { 9; }")]
    [InlineData("loop @iterations {")]
    [InlineData("loop { }")]
    public void ParseLoopWithInvalidBodyThrows(string input)
    {
        var program = new Parser(input).Parse();
        Assert.True(program.HasErrors);
    }

   [Fact]
    public void ParsePatternReturnNode()
    {
        var program = new Parser("beat @kick (grid=16) { 9 };\nbeat @hats (grid=16) { 3, 7, 11, 15 };").Parse();

        Assert.False(program.HasErrors);

        var beat = Assert.IsType<BeatNode>(program.Ast.Statements[0]);

        Assert.Equal("kick", beat.Name);
        Assert.Equal(16, beat.Parameters["grid"].AsInt());
        Assert.Equal([9], beat.Steps);

        var beat2 = Assert.IsType<BeatNode>(program.Ast.Statements[1]);

        Assert.Equal("hats", beat2.Name);
        Assert.Equal(16, beat2.Parameters["grid"].AsInt());
        Assert.Equal([3, 7, 11, 15], beat2.Steps);
    }

    [Fact]
    public void ParsePatternReturnPatternGridWithSameContract()
    {
        var program = new Parser("beat @kick (grid=16) { x...|.x..|..x.|x..x };\nbeat @hats (grid=16) { 1, 6, 11, 13, 16 };").Parse();

        Assert.False(program.HasErrors);

        var beat = Assert.IsType<BeatNode>(program.Ast.Statements[0]);

        Assert.Equal("kick", beat.Name);
        Assert.Equal(16, beat.Parameters["grid"].AsInt());
        Assert.Equal([1, 6, 11, 13, 16], beat.Steps);

        var beat2 = Assert.IsType<BeatNode>(program.Ast.Statements[1]);

        Assert.Equal("hats", beat2.Name);
        Assert.Equal(16, beat2.Parameters["grid"].AsInt());
        Assert.Equal([1, 6, 11, 13, 16], beat2.Steps);
    } 

    [Fact]
    public void ParseMelodyWithoutTrailingCommaReturnsAllNotes()
    {
        var program = new Parser("melody @lead { E4 1/4 0, G4 1/4 1/4 };").Parse();

        Assert.False(program.HasErrors);

        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);

        Assert.Equal("lead", melody.Name);
        Assert.Equal(2, melody.Notes.Count);
        Assert.Equal("E4", melody.Notes[0].NoteName);
        Assert.Equal("G4", melody.Notes[1].NoteName);
    }

    [Fact]
    public void ParseMelodyWithTrailingCommaReturnsAllNotes()
    {
        var program = new Parser("melody @lead { E4 1/4 0, G4 1/4 1/4, };").Parse();

        Assert.False(program.HasErrors);

        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);

        Assert.Equal("lead", melody.Name);
        Assert.Equal(2, melody.Notes.Count);
        Assert.Equal("E4", melody.Notes[0].NoteName);
        Assert.Equal("G4", melody.Notes[1].NoteName);
    }

    // --- Posição (linha/coluna) dos nós da AST ---

    [Fact]
    public void ParseAssignmentNodeTracksLineAndColumnAcrossStatements()
    {
        var program = new Parser("@bpm = 129;\n@steps = 3;").Parse();

        Assert.False(program.HasErrors);

        var first = Assert.IsType<AssignmentNode>(program.Ast.Statements[0]);
        Assert.Equal(0, first.Line);
        Assert.Equal(0, first.Column);

        var second = Assert.IsType<AssignmentNode>(program.Ast.Statements[1]);
        Assert.Equal(1, second.Line);
        Assert.Equal(0, second.Column);
    }

    [Fact]
    public void ParsePlayNodeTracksLineAndColumn()
    {
        var program = new Parser("beat @kick (grid=16) { 9 };\nplay @kick;").Parse();

        Assert.False(program.HasErrors);

        var play = Assert.IsType<PlayNode>(program.Ast.Statements[1]);
        Assert.Equal(1, play.Line);
        Assert.Equal(5, play.Column); // aponta pra referência '@kick', não pro 'play'
    }

    [Fact]
    public void ParseBeatNodeTracksLineAndColumn()
    {
        var program = new Parser("\n  beat @kick (grid=16) { 9 };").Parse();

        Assert.False(program.HasErrors);

        var beat = Assert.IsType<BeatNode>(program.Ast.Statements[0]);
        Assert.Equal(1, beat.Line);
        Assert.Equal(7, beat.Column); // aponta pra referência '@kick'
    }

    [Fact]
    public void ParseMelodyNodeTracksLineAndColumn()
    {
        var program = new Parser("\nmelody @lead { E4 1/4 0 };").Parse();

        Assert.False(program.HasErrors);

        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal(1, melody.Line);
        Assert.Equal(7, melody.Column);
    }

    [Fact]
    public void ParseLoopNodeTracksLineAndColumn()
    {
        var program = new Parser("\n  loop @times { }").Parse();

        Assert.False(program.HasErrors);

        var loop = Assert.IsType<LoopNode>(program.Ast.Statements[0]);
        Assert.Equal(1, loop.Line);
        Assert.Equal(2, loop.Column); // aponta pro keyword 'loop'
    }

    [Fact]
    public void ParseSongNodeTracksLineAndColumn()
    {
        var program = new Parser("\n  song { }").Parse();

        Assert.False(program.HasErrors);

        var song = Assert.IsType<SongNode>(program.Ast.Statements[0]);
        Assert.Equal(1, song.Line);
        Assert.Equal(2, song.Column); // aponta pro keyword 'song'
    }

    // --- Posição (linha/coluna) de erros do parser ---

    [Fact]
    public void ParseReferenceWithInvalidValueReportsPositionOfOffendingToken()
    {
        // Regressão: o erro apontava pra posição de '@bpm' (a referência),
        // não pra posição do token que de fato causou o problema (@invalid).
        var parser = new Parser("@bpm = @invalid;");
        var ex = Assert.Throws<ParserException>(() => parser.ParseReference());

        Assert.Equal(0, ex.Line);
        Assert.Equal(7, ex.Column); // posição de '@invalid', não de '@bpm'
    }

    [Fact]
    public void ParseReferenceWithInvalidValueOnSecondLineReportsCorrectLine()
    {
        var parser = new Parser("@a = 1;\n@bpm = @invalid;");
        parser.ParseReference(); // consome a primeira linha, sem erro

        var ex = Assert.Throws<ParserException>(() => parser.ParseReference());

        Assert.Equal(1, ex.Line);
        Assert.Equal(7, ex.Column);
    }

    [Fact]
    public void ParseParamWithInvalidValueReportsPositionOfOffendingToken()
    {
        // Regressão: o erro apontava pra posição do nome do parâmetro ('grid'),
        // não pra posição do token inválido depois do '='.
        var program = new Parser("beat @kick (grid=(1)) { 9 };").Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);

        Assert.Equal(0, error.Line);
        Assert.Equal(17, error.Column); // posição do '(' inválido, não de 'grid'
    }

    [Fact]
    public void ParseParamBareFlagFollowedByInvalidTokenReportsPositionOfOffendingToken()
    {
        // Regressão: mesmo padrão de bug, em outro ponto de ParseParam.
        var program = new Parser("beat @kick (free 5) { 9 };").Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);

        Assert.Equal(0, error.Line);
        Assert.Equal(17, error.Column); // posição do '5', não de 'free'
    }

    [Fact]
    public void ParseParamDuplicateReportsPositionOfDuplicateToken()
    {
        // Aqui o token que "errou" de fato é o duplicado, então apontar pra
        // ele é o comportamento correto (não é o bug acima).
        var program = new Parser("beat @kick (grid=16, grid=8) { 9 };").Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);

        Assert.Equal(0, error.Line);
        Assert.Equal(21, error.Column); // posição do segundo 'grid'
    }

    [Fact]
    public void ParseConsumeMismatchReportsPositionOfActualToken()
    {
        var parser = new Parser("@bpm 129;"); // falta o '='
        var ex = Assert.Throws<ParserException>(() => parser.ParseReference());

        Assert.Equal(0, ex.Line);
        Assert.Equal(5, ex.Column); // posição do '129', o token que apareceu no lugar de '='
    }

    [Fact]
    public void ParseTopLevelUnexpectedTokenReportsItsOwnPosition()
    {
        var program = new Parser("42;").Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);

        Assert.Equal(0, error.Line);
        Assert.Equal(0, error.Column);
    }

    [Fact]
    public void ParseTopLevelUnexpectedTokenOnSecondLineReportsCorrectLine()
    {
        var program = new Parser("@a = 1;\n42;").Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);

        Assert.Equal(1, error.Line);
        Assert.Equal(0, error.Column);
    }

    [Fact]
    public void ParseLoopUnexpectedEofInsideBodyReportsEofPosition()
    {
        var parser = new Parser("loop @times {\n  @bpm = 1;");
        var ex = Assert.Throws<ParserException>(() => parser.ParseLoop());

        Assert.Equal(1, ex.Line);
        Assert.Equal(11, ex.Column); // posição do EOF, uma coluna após o ';'
    }

    [Fact]
    public void ParseSongUnexpectedEofInsideBodyReportsEofPosition()
    {
        var parser = new Parser("song {\n  play @kick;");
        var ex = Assert.Throws<ParserException>(() => parser.ParseSong());

        Assert.Equal(1, ex.Line);
        Assert.Equal(13, ex.Column);
    }

    [Fact]
    public void ParseMultipleErrorsEachReportItsOwnLineAndColumnAfterRecovery()
    {
        var program = new Parser("42;\n@a = 1;\n99;\n@b = 2;").Parse();

        Assert.True(program.HasErrors);
        Assert.Equal(2, program.Errors.Count);

        Assert.Equal(0, program.Errors[0].Line);
        Assert.Equal(0, program.Errors[0].Column);

        Assert.Equal(2, program.Errors[1].Line);
        Assert.Equal(0, program.Errors[1].Column);

        // A recuperação de erro não deve corromper o parsing das
        // instruções válidas entre os erros.
        var a = Assert.IsType<AssignmentNode>(program.Ast.Statements[0]);
        Assert.Equal("a", a.Name);

        var b = Assert.IsType<AssignmentNode>(program.Ast.Statements[1]);
        Assert.Equal("b", b.Name);
    }

    // --- Sequência de notas em melody: casos válidos e vírgulas quebradas ---

    private const string ValidMelodySequence = """
        melody @minhaAutoria (sample=@piano) {
            G5 0/4 1/4, F5 1/4 1/4, G5 2/4 1/4, E5 3/4 1/4,
            G5 4/4 1/4, D5 5/4 1/4, G5 6/4 1/4 }
        """;

    [Fact]
    public void ParseMelodyWithValidNoteSequenceParsesAllNotesInOrder()
    {
        var program = new Parser(ValidMelodySequence).Parse();

        Assert.False(program.HasErrors);

        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal("minhaAutoria", melody.Name);
        Assert.Equal("@piano", melody.Parameters["sample"].AsString());
        Assert.Equal(7, melody.Notes.Count);

        Assert.Equal(["G5", "F5", "G5", "E5", "G5", "D5", "G5"], melody.Notes.Select(n => n.NoteName));

        Assert.Equal("0/4", melody.Notes[0].FractionStart);
        Assert.Equal("1/4", melody.Notes[0].FractionDuration);
        Assert.Equal("6/4", melody.Notes[6].FractionStart);
        Assert.Equal("1/4", melody.Notes[6].FractionDuration);
    }

    [Fact]
    public void ParseMelodyWithDoubleCommaReportsErrorAtExactPositionAndKeepsPriorNotes()
    {
        // Regressão: uma vírgula duplicada no meio da lista de notas tem que
        // apontar exatamente pra vírgula extra, não pra outra linha.
        var input = """
            melody @minhaAutoria (sample=@piano) {
                G5 0/4 1/4, F5 1/4 1/4, G5 2/4 1/4, E5 3/4 1/4,
                G5 4/4 1/4, D5 5/4 1/4,, G5 6/4 1/4 };
            """;

        var program = new Parser(input).Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);
        Assert.Equal(2, error.Line);
        Assert.Equal(27, error.Column); // a segunda vírgula, não a primeira

        // As notas válidas, antes e depois do erro, continuam na AST.
        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal(["G5", "F5", "G5", "E5", "G5", "D5", "G5"], melody.Notes.Select(n => n.NoteName));
    }

    [Fact]
    public void ParseMelodyWithMissingCommaBetweenNotesReportsUnexpectedNoteToken()
    {
        // Vírgula esquecida (em vez de duplicada): o parser não trata isso
        // como fim da lista silenciosamente — reporta a nota inesperada na
        // posição correta.
        var input = """
            melody @minhaAutoria (sample=@piano) {
                G5 0/4 1/4, F5 1/4 1/4, G5 2/4 1/4, E5 3/4 1/4,
                G5 4/4 1/4 D5 5/4 1/4, G5 6/4 1/4 };
            """;

        var program = new Parser(input).Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);
        Assert.Equal(2, error.Line);
        Assert.Equal(15, error.Column); // aponta pro 'D5' que apareceu sem vírgula antes
    }

    [Fact]
    public void ParseMelodyWithTrailingDoubleCommaBeforeClosingBraceReportsError()
    {
        var input = "melody @m {\n    G5 0/4 1/4, F5 1/4 1/4,, }";

        var program = new Parser(input).Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);
        Assert.Equal(1, error.Line);
        Assert.Equal(27, error.Column);

        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal(2, melody.Notes.Count);
    }

    [Fact]
    public void ParseMelodyWithDoubleCommaNestedInLoopReportsOnlyTheRealErrorAndKeepsSiblingStatements()
    {
        // Regressão principal: antes dessa correção, uma vírgula quebrada
        // dentro de uma melody aninhada num loop fazia a recuperação de erro
        // "vazar" pro fora do loop — o 'play' que vinha depois da melody, no
        // mesmo bloco, era promovido incorretamente pro nível superior, e o
        // '}' do loop sobrava como um SEGUNDO erro, numa linha completamente
        // diferente da vírgula quebrada de fato.
        var input = """
            loop @composicao (bars=2) {
                melody @minhaAutoria (sample=@piano) {
                    G5 0/4 1/4, F5 1/4 1/4,, G5 2/4 1/4, E5 3/4 1/4,
                    G5 4/4 1/4, D5 5/4 1/4, G5 6/4 1/4 };
                play @minhaAutoria;
            }
            """;

        var program = new Parser(input).Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors); // só o erro real, sem "fantasma" de '}'
        Assert.Equal(2, error.Line);
        Assert.Equal(31, error.Column);

        var loop = Assert.IsType<LoopNode>(program.Ast.Statements[0]);
        Assert.Equal(2, loop.Statements.Count);

        var melody = Assert.IsType<MelodyNode>(loop.Statements[0]);
        // notas antes e depois do erro foram preservadas
        Assert.Equal(["G5", "F5", "G5", "E5", "G5", "D5", "G5"], melody.Notes.Select(n => n.NoteName));

        // O 'play' continua aninhado dentro do loop, não vazou pro topo.
        var play = Assert.IsType<PlayNode>(loop.Statements[1]);
        Assert.Equal("minhaAutoria", play.PatternName);
    }

    [Fact]
    public void ParseBeatWithDoubleCommaInStepsNestedInLoopReportsOnlyTheRealErrorAndKeepsSiblingStatements()
    {
        // Mesma classe de bug do teste acima, só que na lista de steps de
        // um beat em vez da lista de notas de uma melody.
        var input = """
            loop @composicao (bars=2) {
                beat @kick (grid=16) { 1, 5,, 9, 13 };
                play @kick;
            }
            """;

        var program = new Parser(input).Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);
        Assert.Equal(1, error.Line);
        Assert.Equal(32, error.Column);

        var loop = Assert.IsType<LoopNode>(program.Ast.Statements[0]);
        Assert.Equal(2, loop.Statements.Count);

        var beat = Assert.IsType<BeatNode>(loop.Statements[0]);
        Assert.Equal([1, 5], beat.Steps);

        var play = Assert.IsType<PlayNode>(loop.Statements[1]);
        Assert.Equal("kick", play.PatternName);
    }

    // --- Sintaxe de início relativo de nota ('+offset') ---

    [Fact]
    public void ParseMelodyWithRelativeStartResolvesToAbsoluteFractionAfterPreviousNoteEnds()
    {
        // Exemplo do pedido original: G4 termina em 1/2 + 1/4 = 3/4,
        // então F4 (+1/8) deve começar em 3/4 + 1/8 = 7/8.
        var program = new Parser("melody @m { G4 1/2 1/4, F4 +1/8 1/4 };").Parse();

        Assert.False(program.HasErrors);

        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal("1/2", melody.Notes[0].FractionStart);
        Assert.Equal("7/8", melody.Notes[1].FractionStart);
        Assert.Equal("1/4", melody.Notes[1].FractionDuration); // duração não é afetada pelo '+'
    }

    [Fact]
    public void ParseMelodyWithChainedRelativeStartsAccumulatesCorrectly()
    {
        var program = new Parser(
            "melody @m { G4 0 1/4, F4 +0 1/4, E4 +0 1/4, D4 +1/8 1/4 };").Parse();

        Assert.False(program.HasErrors);

        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal(["0", "1/4", "1/2", "7/8"], melody.Notes.Select(n => n.FractionStart));
    }

    [Fact]
    public void ParseMelodyRelativeStartAutomaticallyCascadesWhenAnEarlierDurationChanges()
    {
        // Esse é o problema original que o '+' resolve: mudar a duração de
        // uma nota anterior não deveria exigir recalcular manualmente o
        // início de todas as notas seguintes.
        var withOriginalDuration = new Parser(
            "melody @m { G4 0 1/4, F4 +0 1/4, E4 +0 1/4 };").Parse();
        var originalStarts = Assert.IsType<MelodyNode>(withOriginalDuration.Ast.Statements[0])
            .Notes.Select(n => n.FractionStart);
        Assert.Equal(["0", "1/4", "1/2"], originalStarts);

        // Só a duração da primeira nota mudou (1/4 -> 1/2); as notas
        // seguintes usam a mesma sintaxe '+0' de antes, sem tocar em nada.
        var withChangedDuration = new Parser(
            "melody @m { G4 0 1/2, F4 +0 1/4, E4 +0 1/4 };").Parse();
        var shiftedStarts = Assert.IsType<MelodyNode>(withChangedDuration.Ast.Statements[0])
            .Notes.Select(n => n.FractionStart);
        Assert.Equal(["0", "1/2", "3/4"], shiftedStarts);
    }

    [Fact]
    public void ParseMelodyWithRelativeStartUsingIntegerOffsetAddsWholeBar()
    {
        var program = new Parser("melody @m { G4 0 1/4, F4 +1 1/4 };").Parse();

        Assert.False(program.HasErrors);

        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal("5/4", melody.Notes[1].FractionStart); // 1/4 (fim da anterior) + 1 (bar inteiro)
    }

    [Fact]
    public void ParseMelodyRelativeStartReducesResultToLowestTerms()
    {
        var program = new Parser("melody @m { G4 0 1/2, F4 +1/2 1/4 };").Parse();

        Assert.False(program.HasErrors);

        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal("1", melody.Notes[1].FractionStart); // 1/2 + 1/2 = 2/2, reduzido pra "1"
    }

    [Fact]
    public void ParseMelodyMixingAbsoluteAndRelativeStartsInSameNoteListWorks()
    {
        var program = new Parser(
            "melody @m { G4 0/4 1/4, F4 1/4 1/4, G4 +0 1/4, E4 3/4 1/4 };").Parse();

        Assert.False(program.HasErrors);

        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal(["0/4", "1/4", "1/2", "3/4"], melody.Notes.Select(n => n.FractionStart));
    }

    [Fact]
    public void ParseMelodyWithRelativeStartOnFirstNoteThrowsAtThePlusToken()
    {
        var input = "melody @m { G4 +1/8 1/4 };";

        var program = new Parser(input).Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);
        Assert.Contains("previous note", error.Message);
        Assert.Equal(0, error.Line);
        Assert.Equal(15, error.Column); // posição do '+'
    }

    [Fact]
    public void ParseMelodyWithRelativeStartMissingOffsetValueThrowsAtOffendingToken()
    {
        var input = "melody @m { G4 0 1/4, F4 +, G4 1/4 1/4 };";

        var program = new Parser(input).Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);
        Assert.Equal(0, error.Line);
        Assert.Equal(26, error.Column); // posição da vírgula que veio no lugar do offset

        // a nota seguinte, válida, continua sendo lida
        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal(["G4", "G4"], melody.Notes.Select(n => n.NoteName));
    }

    [Fact]
    public void ParseMelodyWithRelativeStartFollowedByInvalidOffsetTokenThrowsAtOffendingToken()
    {
        var input = "melody @m { G4 0 1/4, F4 + G4 1/4 };";

        var program = new Parser(input).Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);
        Assert.Equal(0, error.Line);
        Assert.Equal(27, error.Column); // posição do 'G4' que veio no lugar do offset
    }

    [Fact]
    public void ParseMelodyRelativeStartStillRecoversFromUnrelatedCommaError()
    {
        // Garante que a sintaxe '+' não quebra a recuperação de erro já
        // existente pra vírgulas duplicadas no meio da lista de notas.
        var input = """
            melody @m {
                G4 0 1/4, F4 +0 1/4,, E4 +0 1/4
            };
            """;

        var program = new Parser(input).Parse();

        Assert.True(program.HasErrors);
        var error = Assert.Single(program.Errors);
        Assert.Equal(1, error.Line);

        // E4 continua relativo ao fim do F4 (a última nota válida)
        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal(["0", "1/4", "1/2"], melody.Notes.Select(n => n.FractionStart));
    }

    [Fact]
    public void ParseExplicitChordExpandsIntoNotesSharingStartAndDuration()
    {
        var program = new Parser("melody @m { [C4 E4 G4] 0 1/2, [A3, C4] +0 1/4 };").Parse();

        Assert.False(program.HasErrors);
        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);

        Assert.Equal(["C4", "E4", "G4", "A3", "C4"], melody.Notes.Select(n => n.NoteName));
        Assert.Equal(["0", "0", "0", "1/2", "1/2"], melody.Notes.Select(n => n.FractionStart));
        Assert.Equal(["1/2", "1/2", "1/2", "1/4", "1/4"], melody.Notes.Select(n => n.FractionDuration));
        Assert.Equal([13, 16, 19], melody.Notes.Take(3).Select(n => n.Column)); // cada nota na sua posição
    }

    [Theory]
    [InlineData("C4:maj", new[] { "C4", "E4", "G4" })]
    [InlineData("A3:m", new[] { "A3", "C4", "E4" })]
    [InlineData("G3:7", new[] { "G3", "B3", "D4", "F4" })]
    [InlineData("Bb3:maj7", new[] { "Bb3", "D4", "F4", "A4" })]
    [InlineData("F#4:dim", new[] { "F#4", "A4", "C5" })]
    [InlineData("D4:sus4", new[] { "D4", "G4", "A4" })]
    public void ParseNamedChordExpandsToItsNotes(string chord, string[] expected)
    {
        var program = new Parser($"melody @m {{ {chord} 0 1 }};").Parse();

        Assert.False(program.HasErrors);
        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal(expected, melody.Notes.Select(n => n.NoteName));
        Assert.All(melody.Notes, n => Assert.Equal(12, n.Column)); // posição do acorde
    }

    [Fact]
    public void ParseRelativeStartAfterChordIsRelativeToTheChordsEnd()
    {
        var program = new Parser("melody @m { C4:maj 1/4 1/2, D4 +0 1/4 };").Parse();

        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal("3/4", melody.Notes[^1].FractionStart);
    }

    [Theory]
    [InlineData("C4:maj8 0 1", "Unknown chord quality 'maj8'")]
    [InlineData("C:m 0 1", "needs an octave")]
    [InlineData("[] 0 1", "at least one note")]
    public void ParseInvalidChordReportsError(string entry, string message)
    {
        var program = new Parser($"melody @m {{ {entry} }};").Parse();

        var error = Assert.Single(program.Errors);
        Assert.Contains(message, error.Message);
        Assert.Equal(12, error.Column);
    }

    [Fact]
    public void ParseMelodyReportsEveryInvalidNoteNotJustTheFirst()
    {
        var program = new Parser("melody @m { C4:x 0 1, E4 1 1, C:m 2 1, G4 3 1 };").Parse();

        Assert.Equal(2, program.Errors.Count);
        var melody = Assert.IsType<MelodyNode>(program.Ast.Statements[0]);
        Assert.Equal(["E4", "G4"], melody.Notes.Select(n => n.NoteName));
    }
}
