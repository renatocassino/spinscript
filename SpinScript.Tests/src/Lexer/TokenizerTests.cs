using SpinScript.Lexer;
using Xunit;

public class TokenizerTests
{
    [Fact]
    public void TokenizeSimpleAssignmentReturnsCorrectTokens()
    {
        var tokens = new Lexer("@bpm = 129;").Tokenize();

        Assert.Equal(TokenType.REFERENCE, tokens[0].Type);
        Assert.Equal("bpm", tokens[0].Value);

        Assert.Equal(TokenType.EQUALS, tokens[1].Type);
        Assert.Equal("=", tokens[1].Value);

        Assert.Equal(TokenType.NUMBER, tokens[2].Type);
        Assert.Equal("129", tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);
        Assert.Equal(";", tokens[3].Value);

        Assert.Equal(TokenType.EOF, tokens[4].Type);
        Assert.Equal("", tokens[4].Value);
    }

    [Fact]
    public void TokenizeSimpleFraction()
    {
        var tokens = new Lexer("1/4; 1/5; 1; 3.2;").Tokenize();

        Assert.Equal(TokenType.FRACTION, tokens[0].Type);
        Assert.Equal("1/4", tokens[0].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[1].Type);

        Assert.Equal(TokenType.FRACTION, tokens[2].Type);
        Assert.Equal("1/5", tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);

        Assert.Equal(TokenType.NUMBER, tokens[4].Type);
        Assert.Equal("1", tokens[4].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[5].Type);

        Assert.Equal(TokenType.NUMBER, tokens[6].Type);
        Assert.Equal("3.2", tokens[6].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[7].Type);
        Assert.Equal(TokenType.EOF, tokens[8].Type);
    }

    [Fact]
    public void TokenizeSimpleVarWithDoubleNumber()
    {
        var tokens = new Lexer("@volume = 0.1;").Tokenize();

        Assert.Equal(TokenType.REFERENCE, tokens[0].Type);
        Assert.Equal("volume", tokens[0].Value);

        Assert.Equal(TokenType.EQUALS, tokens[1].Type);
        Assert.Equal("=", tokens[1].Value);

        Assert.Equal(TokenType.NUMBER, tokens[2].Type);
        Assert.Equal("0.1", tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);
        Assert.Equal(";", tokens[3].Value);

        Assert.Equal(TokenType.EOF, tokens[4].Type);
        Assert.Equal("", tokens[4].Value);
    }

    [Fact]
    public void TokenizeSimpleVarWithSingleQuoteString()
    {
        var tokens = new Lexer("@guitarMidi='/guitar.midi';").Tokenize();

        Assert.Equal(TokenType.REFERENCE, tokens[0].Type);
        Assert.Equal("guitarMidi", tokens[0].Value);

        Assert.Equal(TokenType.EQUALS, tokens[1].Type);
        Assert.Equal("=", tokens[1].Value);

        Assert.Equal(TokenType.STRING_LITERAL, tokens[2].Type);
        Assert.Equal("/guitar.midi", tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);
        Assert.Equal(";", tokens[3].Value);

        Assert.Equal(TokenType.EOF, tokens[4].Type);
        Assert.Equal("", tokens[4].Value);
    }

    [Fact]
    public void TokenizeSimpleVarWithDoubleQuoteString()
    {
        var tokens = new Lexer("@guitarMidi = \"/guitar.midi\";").Tokenize();

        Assert.Equal(TokenType.REFERENCE, tokens[0].Type);
        Assert.Equal("guitarMidi", tokens[0].Value);

        Assert.Equal(TokenType.EQUALS, tokens[1].Type);
        Assert.Equal("=", tokens[1].Value);

        Assert.Equal(TokenType.STRING_LITERAL, tokens[2].Type);
        Assert.Equal("/guitar.midi", tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);
        Assert.Equal(";", tokens[3].Value);

        Assert.Equal(TokenType.EOF, tokens[4].Type);
        Assert.Equal("", tokens[4].Value);
    }

    [Theory]
    [InlineData("A4")]
    [InlineData("B")]
    [InlineData("C2")]
    [InlineData("D1")]
    [InlineData("E")]
    [InlineData("F5")]
    [InlineData("A#6")]
    [InlineData("C#")]
    [InlineData("Eb8")]
    [InlineData("F#9")]
    [InlineData("D#0")]
    [InlineData("Db")]
    [InlineData("Db1")]
    public void CheckIfNoteWhenIsValid(string input)
    {
        var lexer = new Lexer("");
        Assert.True(lexer.CheckIsNote(input));
    }

    [Theory]
    [InlineData("H")]
    [InlineData("Bw")]
    [InlineData("CD")]
    [InlineData("a#")]
    [InlineData("b#")]
    [InlineData("E#-10")]
    [InlineData("E#11")]
    [InlineData("Hb")]
    [InlineData("Zb6")]
    public void CheckIfNoteWhenIsInvalid(string input)
    {
        var lexer = new Lexer("");
        Assert.False(lexer.CheckIsNote(input));
    }

    [Theory]
    [InlineData("@x = \"\";")]
    [InlineData("@x = '';")]
    public void TokenizeEmptyStringReturnsEmptyValue(string input)
    {
        var tokens = new Lexer(input).Tokenize();

        Assert.Equal(TokenType.STRING_LITERAL, tokens[2].Type);
        Assert.Equal("", tokens[2].Value);
    }

    [Fact]
    public void TokenizeNotes()
    {
        var tokens = new Lexer("C4 1/3 1/2; D#7 1/2 1/2").Tokenize();

        Assert.Equal(TokenType.NOTE, tokens[0].Type);
        Assert.Equal("C4", tokens[0].Value);

        Assert.Equal(TokenType.FRACTION, tokens[1].Type);
        Assert.Equal("1/3", tokens[1].Value);

        Assert.Equal(TokenType.FRACTION, tokens[2].Type);
        Assert.Equal("1/2", tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);

        Assert.Equal(TokenType.NOTE, tokens[4].Type);
        Assert.Equal("D#7", tokens[4].Value);

        Assert.Equal(TokenType.FRACTION, tokens[5].Type);
        Assert.Equal("1/2", tokens[5].Value);

        Assert.Equal(TokenType.FRACTION, tokens[6].Type);
        Assert.Equal("1/2", tokens[6].Value);
    }

    [Fact]
    public void TokenizeDoubleQuoteStringAllowsSingleQuoteInside()
    {
        var tokens = new Lexer("@x = \"it's fine\";").Tokenize();

        Assert.Equal(TokenType.STRING_LITERAL, tokens[2].Type);
        Assert.Equal("it's fine", tokens[2].Value);
    }

    [Fact]
    public void TokenizeSingleQuoteStringAllowsDoubleQuoteInside()
    {
        var tokens = new Lexer("@x = 'She said \"hi\"';").Tokenize();

        Assert.Equal(TokenType.STRING_LITERAL, tokens[2].Type);
        Assert.Equal("She said \"hi\"", tokens[2].Value);
    }

    [Fact]
    public void TokenizeStringTracksStartPosition()
    {
        var tokens = new Lexer("@guitarMidi = \"/guitar.midi\";").Tokenize();

        Assert.Equal(0, tokens[2].Line);
        Assert.Equal(14, tokens[2].Column); // aponta pra aspa de abertura
    }

    [Theory]
    [InlineData("@x = \"unterminated")]
    [InlineData("@x = 'unterminated")]
    public void TokenizeUnterminatedStringThrows(string input)
    {
        var ex = Assert.Throws<LexerException>(() => new Lexer(input).Tokenize());

        Assert.Equal(0, ex.Line);
        Assert.Equal(5, ex.Column); // aponta pra aspa de abertura
    }

    [Fact]
    public void TokenizePlaySong()
    {
        var tokens = new Lexer("play @intro; play @chord (repeat=2);").Tokenize();

        Assert.Equal(TokenType.PLAY, tokens[0].Type);
        Assert.Equal("play", tokens[0].Value);

        Assert.Equal(TokenType.REFERENCE, tokens[1].Type);
        Assert.Equal("intro", tokens[1].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[2].Type);
        Assert.Equal(";", tokens[2].Value);

        Assert.Equal(TokenType.PLAY, tokens[3].Type);
        Assert.Equal("play", tokens[3].Value);

        Assert.Equal(TokenType.REFERENCE, tokens[4].Type);
        Assert.Equal("chord", tokens[4].Value);

        Assert.Equal(TokenType.LPAREN, tokens[5].Type);
        Assert.Equal("(", tokens[5].Value);

        Assert.Equal(TokenType.IDENT, tokens[6].Type);
        Assert.Equal("repeat", tokens[6].Value);

        Assert.Equal(TokenType.EQUALS, tokens[7].Type);
        Assert.Equal("=", tokens[7].Value);

        Assert.Equal(TokenType.NUMBER, tokens[8].Type);
        Assert.Equal("2", tokens[8].Value);

        Assert.Equal(TokenType.RPAREN, tokens[9].Type);
        Assert.Equal(")", tokens[9].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[10].Type);
        Assert.Equal(";", tokens[10].Value);

        Assert.Equal(TokenType.EOF, tokens[11].Type);
        Assert.Equal("", tokens[11].Value);
    }

    [Fact]
    public void TokenizeLoopBlock()
    {
        var tokens = new Lexer("loop @groove (bars=1) {\n  play @kick;\n  play @snare;\n  play @hats;\n}").Tokenize();

        Assert.Equal(TokenType.LOOP, tokens[0].Type);
        Assert.Equal("loop", tokens[0].Value);

        Assert.Equal(TokenType.REFERENCE, tokens[1].Type);
        Assert.Equal("groove", tokens[1].Value);

        Assert.Equal(TokenType.LPAREN, tokens[2].Type);
        Assert.Equal("(", tokens[2].Value);

        Assert.Equal(TokenType.IDENT, tokens[3].Type);
        Assert.Equal("bars", tokens[3].Value);

        Assert.Equal(TokenType.EQUALS, tokens[4].Type);
        Assert.Equal("=", tokens[4].Value);

        Assert.Equal(TokenType.NUMBER, tokens[5].Type);
        Assert.Equal("1", tokens[5].Value);

        Assert.Equal(TokenType.RPAREN, tokens[6].Type);
        Assert.Equal(")", tokens[6].Value);

        Assert.Equal(TokenType.LBRACE, tokens[7].Type);
        Assert.Equal("{", tokens[7].Value);

        Assert.Equal(TokenType.PLAY, tokens[8].Type);
        Assert.Equal("play", tokens[8].Value);

        Assert.Equal(TokenType.REFERENCE, tokens[9].Type);
        Assert.Equal("kick", tokens[9].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[10].Type);
        Assert.Equal(";", tokens[10].Value);

        Assert.Equal(TokenType.PLAY, tokens[11].Type);
        Assert.Equal("play", tokens[11].Value);

        Assert.Equal(TokenType.REFERENCE, tokens[12].Type);
        Assert.Equal("snare", tokens[12].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[13].Type);
        Assert.Equal(";", tokens[13].Value);

        Assert.Equal(TokenType.PLAY, tokens[14].Type);
        Assert.Equal("play", tokens[14].Value);

        Assert.Equal(TokenType.REFERENCE, tokens[15].Type);
        Assert.Equal("hats", tokens[15].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[16].Type);
        Assert.Equal(";", tokens[16].Value);

        Assert.Equal(TokenType.RBRACE, tokens[17].Type);
        Assert.Equal("}", tokens[17].Value);

        Assert.Equal(TokenType.EOF, tokens[18].Type);
        Assert.Equal("", tokens[18].Value);
    }

    [Fact]
    public void TokenizeMelody()
    {
        var tokens = new Lexer("melody @piano (volume=2) { C3 1/2 1/4 }").Tokenize();

        Assert.Equal(TokenType.MELODY, tokens[0].Type);
        Assert.Equal("melody", tokens[0].Value);

        Assert.Equal(TokenType.REFERENCE, tokens[1].Type);
        Assert.Equal("piano", tokens[1].Value);

        Assert.Equal(TokenType.LPAREN, tokens[2].Type);
        Assert.Equal("(", tokens[2].Value);

        Assert.Equal(TokenType.IDENT, tokens[3].Type);
        Assert.Equal("volume", tokens[3].Value);

        Assert.Equal(TokenType.EQUALS, tokens[4].Type);
        Assert.Equal("=", tokens[4].Value);

        Assert.Equal(TokenType.NUMBER, tokens[5].Type);
        Assert.Equal("2", tokens[5].Value);

        Assert.Equal(TokenType.RPAREN, tokens[6].Type);
        Assert.Equal(")", tokens[6].Value);

        Assert.Equal(TokenType.LBRACE, tokens[7].Type);

        Assert.Equal(TokenType.NOTE, tokens[8].Type);
        Assert.Equal("C3", tokens[8].Value);

        Assert.Equal(TokenType.FRACTION, tokens[9].Type);
        Assert.Equal("1/2", tokens[9].Value);

        Assert.Equal(TokenType.FRACTION, tokens[10].Type);
        Assert.Equal("1/4", tokens[10].Value);

        Assert.Equal(TokenType.RBRACE, tokens[11].Type);
    }

    [Fact]
    public void TokenizeBeatSintax()
    {
        var tokens = new Lexer("beat @kick (grid=16) { 9 };\nbeat hats (sound=hihat, grid=16) { 3, 7, 11, 15 };").Tokenize();

        Assert.Equal(TokenType.BEAT, tokens[0].Type);
        Assert.Equal("beat", tokens[0].Value);

        Assert.Equal(TokenType.REFERENCE, tokens[1].Type);
        Assert.Equal("kick", tokens[1].Value);

        Assert.Equal(TokenType.LPAREN, tokens[2].Type);
        Assert.Equal("(", tokens[2].Value);

        Assert.Equal(TokenType.IDENT, tokens[3].Type);
        Assert.Equal("grid", tokens[3].Value);

        Assert.Equal(TokenType.EQUALS, tokens[4].Type);
        Assert.Equal("=", tokens[4].Value);

        Assert.Equal(TokenType.NUMBER, tokens[5].Type);
        Assert.Equal("16", tokens[5].Value);

        Assert.Equal(TokenType.RPAREN, tokens[6].Type);
        Assert.Equal(")", tokens[6].Value);

        Assert.Equal(TokenType.LBRACE, tokens[7].Type);
        Assert.Equal("{", tokens[7].Value);

        Assert.Equal(TokenType.NUMBER, tokens[8].Type);
        Assert.Equal("9", tokens[8].Value);

        Assert.Equal(TokenType.RBRACE, tokens[9].Type);
        Assert.Equal("}", tokens[9].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[10].Type);
        Assert.Equal(";", tokens[10].Value);

        Assert.Equal(TokenType.BEAT, tokens[11].Type);
        Assert.Equal("beat", tokens[11].Value);

        Assert.Equal(TokenType.IDENT, tokens[12].Type);
        Assert.Equal("hats", tokens[12].Value);

        Assert.Equal(TokenType.LPAREN, tokens[13].Type);
        Assert.Equal("(", tokens[13].Value);

        Assert.Equal(TokenType.IDENT, tokens[14].Type);
        Assert.Equal("sound", tokens[14].Value);

        Assert.Equal(TokenType.EQUALS, tokens[15].Type);
        Assert.Equal("=", tokens[15].Value);

        Assert.Equal(TokenType.IDENT, tokens[16].Type);
        Assert.Equal("hihat", tokens[16].Value);

        Assert.Equal(TokenType.COMMA, tokens[17].Type);
        Assert.Equal(",", tokens[17].Value);

        Assert.Equal(TokenType.IDENT, tokens[18].Type);
        Assert.Equal("grid", tokens[18].Value);

        Assert.Equal(TokenType.EQUALS, tokens[19].Type);
        Assert.Equal("=", tokens[19].Value);

        Assert.Equal(TokenType.NUMBER, tokens[20].Type);
        Assert.Equal("16", tokens[20].Value);

        Assert.Equal(TokenType.RPAREN, tokens[21].Type);
        Assert.Equal(")", tokens[21].Value);

        Assert.Equal(TokenType.LBRACE, tokens[22].Type);
        Assert.Equal("{", tokens[22].Value);

        Assert.Equal(TokenType.NUMBER, tokens[23].Type);
        Assert.Equal("3", tokens[23].Value);

        Assert.Equal(TokenType.COMMA, tokens[24].Type);
        Assert.Equal(",", tokens[24].Value);

        Assert.Equal(TokenType.NUMBER, tokens[25].Type);
        Assert.Equal("7", tokens[25].Value);

        Assert.Equal(TokenType.COMMA, tokens[26].Type);
        Assert.Equal(",", tokens[26].Value);

        Assert.Equal(TokenType.NUMBER, tokens[27].Type);
        Assert.Equal("11", tokens[27].Value);

        Assert.Equal(TokenType.COMMA, tokens[28].Type);
        Assert.Equal(",", tokens[28].Value);

        Assert.Equal(TokenType.NUMBER, tokens[29].Type);
        Assert.Equal("15", tokens[29].Value);

        Assert.Equal(TokenType.RBRACE, tokens[30].Type);
        Assert.Equal("}", tokens[30].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[31].Type);
        Assert.Equal(";", tokens[31].Value);

        Assert.Equal(TokenType.EOF, tokens[32].Type);
        Assert.Equal("", tokens[32].Value);
    }

    [Fact]
    public void TokenizeLineWithComments()
    {
        var tokens = new Lexer("@steps = 30; // This is the steps to code").Tokenize();

        Assert.Equal(TokenType.REFERENCE, tokens[0].Type);
        Assert.Equal("steps", tokens[0].Value);

        Assert.Equal(TokenType.EQUALS, tokens[1].Type);
        Assert.Equal("=", tokens[1].Value);

        Assert.Equal(TokenType.NUMBER, tokens[2].Type);
        Assert.Equal("30", tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);
        Assert.Equal(";", tokens[3].Value);

        Assert.Equal(TokenType.EOF, tokens[4].Type);
        Assert.Equal("", tokens[4].Value);
    }

    [Fact]
    public void TokenizeLinesWithInlineComments()
    {
        var tokens = new Lexer("@steps = 30;\n// This is the steps to code\n// Now I'll set another another var\n@bpm = 80;\n\n").Tokenize();

        Assert.Equal(TokenType.REFERENCE, tokens[0].Type);
        Assert.Equal("steps", tokens[0].Value);

        Assert.Equal(TokenType.EQUALS, tokens[1].Type);
        Assert.Equal("=", tokens[1].Value);

        Assert.Equal(TokenType.NUMBER, tokens[2].Type);
        Assert.Equal("30", tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);
        Assert.Equal(";", tokens[3].Value);

        Assert.Equal(TokenType.REFERENCE, tokens[4].Type);
        Assert.Equal("bpm", tokens[4].Value);

        Assert.Equal(TokenType.EQUALS, tokens[5].Type);
        Assert.Equal("=", tokens[5].Value);

        Assert.Equal(TokenType.NUMBER, tokens[6].Type);
        Assert.Equal("80", tokens[6].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[7].Type);
        Assert.Equal(";", tokens[7].Value);

        Assert.Equal(TokenType.EOF, tokens[8].Type);
        Assert.Equal("", tokens[8].Value);
    }

    [Fact]
    public void TokenizeMultipleLineComments()
    {
        var input = """
            @bpm = 79;
            /**
                This is a multiline comment, really cool
            */
            @steps = 5;                                                                                                                                    
            """;

        var tokens = new Lexer(input).Tokenize();

        Assert.Equal(TokenType.REFERENCE, tokens[0].Type);
        Assert.Equal("bpm", tokens[0].Value);

        Assert.Equal(TokenType.EQUALS, tokens[1].Type);
        Assert.Equal("=", tokens[1].Value);

        Assert.Equal(TokenType.NUMBER, tokens[2].Type);
        Assert.Equal("79", tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);
        Assert.Equal(";", tokens[3].Value);

        Assert.Equal(TokenType.REFERENCE, tokens[4].Type);
        Assert.Equal("steps", tokens[4].Value);

        Assert.Equal(TokenType.EQUALS, tokens[5].Type);
        Assert.Equal("=", tokens[5].Value);

        Assert.Equal(TokenType.NUMBER, tokens[6].Type);
        Assert.Equal("5", tokens[6].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[7].Type);
        Assert.Equal(";", tokens[7].Value);

        Assert.Equal(TokenType.EOF, tokens[8].Type);
        Assert.Equal("", tokens[8].Value);
    }

    [Fact]
    public void TokenizeTracksLineAndColumnAcrossMultipleLines()
    {
        var input = "@bpm = 120;\nloop {\n  play;\n}";
        var tokens = new Lexer(input).Tokenize();

        Assert.Equal(0, tokens[0].Line);   // bpm (REFERENCE, aponta pro '@')
        Assert.Equal(0, tokens[0].Column);

        Assert.Equal(0, tokens[2].Line);   // 120 (NUMBER)
        Assert.Equal(7, tokens[2].Column);

        Assert.Equal(1, tokens[4].Line);   // loop (LOOP), segunda linha
        Assert.Equal(0, tokens[4].Column);

        Assert.Equal(2, tokens[6].Line);   // play (PLAY), terceira linha, indentado
        Assert.Equal(2, tokens[6].Column);

        Assert.Equal(3, tokens[8].Line);   // '}' na última linha
        Assert.Equal(0, tokens[8].Column);
    }

    [Fact]
    public void TokenizeTracksLineAndColumnAfterMultilineComment()
    {
        var input = "@bpm = 79;\n/**\n    multiline comment\n*/\n@steps = 5;";
        var tokens = new Lexer(input).Tokenize();

        // Depois do comentário de 4 linhas (linhas 1 a 3), @steps deve estar na linha 4.
        Assert.Equal(TokenType.REFERENCE, tokens[4].Type);
        Assert.Equal("steps", tokens[4].Value);
        Assert.Equal(4, tokens[4].Line);
        Assert.Equal(0, tokens[4].Column);
    }

    [Fact]
    public void TokenizeUnexpectedCharacterThrowsWithLineAndColumn()
    {
        var ex = Assert.Throws<LexerException>(() => new Lexer("loop\n#").Tokenize());

        Assert.Equal(1, ex.Line);
        Assert.Equal(0, ex.Column);
    }

    [Fact]
    public void TokenizeInvalidCommentSliceThrowsWithLineAndColumn()
    {
        var ex = Assert.Throws<LexerException>(() => new Lexer("loop\n/x").Tokenize());

        Assert.Equal(1, ex.Line);
        Assert.Equal(1, ex.Column);
    }

    [Fact]
    public void TokenizeCommentReachingEndOfInputThrowsWithLineAndColumn()
    {
        var ex = Assert.Throws<LexerException>(() => new Lexer("/").Tokenize());

        Assert.Equal(0, ex.Line);
        Assert.Equal(1, ex.Column);
    }

    [Fact]
    public void TokenizeUnterminatedMultilineCommentThrowsWithLineAndColumn()
    {
        var ex = Assert.Throws<LexerException>(() => new Lexer("@bpm = 120;\n/* nunca fecha").Tokenize());

        Assert.Equal(1, ex.Line);   // aponta pro '/' que abriu o comentário
        Assert.Equal(0, ex.Column);
    }

    [Fact]
    public void TokenizeNumberWithDoubleDotThrowsWithLineAndColumn()
    {
        var ex = Assert.Throws<LexerException>(() => new Lexer("12.3.4;").Tokenize());

        Assert.Equal(0, ex.Line);
        Assert.Equal(0, ex.Column);
    }

    [Fact]
    public void TokenizeReferenceInvalidStartThrowsWithLineAndColumn()
    {
        var ex = Assert.Throws<LexerException>(() => new Lexer("@2fast = 1;").Tokenize());

        Assert.Equal(0, ex.Line);
        Assert.Equal(1, ex.Column);
    }

    [Theory]
    [InlineData("@bpm = 129;\n")]
    [InlineData("    @bpm    =    129    ;     \n\n")]
    [InlineData("\n@bpm\n=\n129\n;\n")]
    [InlineData("\t@bpm \t= \t129;\t\n")]
    public void TokenizeSimpleAssignmentWithDifferentChars(string input)
    {
        var tokens = new Lexer(input).Tokenize();

        Assert.Equal(TokenType.REFERENCE, tokens[0].Type);
        Assert.Equal("bpm", tokens[0].Value);

        Assert.Equal(TokenType.EQUALS, tokens[1].Type);
        Assert.Equal("=", tokens[1].Value);

        Assert.Equal(TokenType.NUMBER, tokens[2].Type);
        Assert.Equal("129", tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);
        Assert.Equal(";", tokens[3].Value);

        Assert.Equal(TokenType.EOF, tokens[4].Type);
        Assert.Equal("", tokens[4].Value);
    }


    [Theory]
    [InlineData("@bpm = 80;\n", "bpm", "80")]
    [InlineData("@forceLoop = 1;", "forceLoop", "1")]
    [InlineData("@valid_variable = 12;", "valid_variable", "12")]
    public void TokenizeSetVariables(string input, string varName, string varValue)
    {
        var tokens = new Lexer(input).Tokenize();

        Assert.Equal(TokenType.REFERENCE, tokens[0].Type);
        Assert.Equal(varName, tokens[0].Value);

        Assert.Equal(TokenType.EQUALS, tokens[1].Type);
        Assert.Equal("=", tokens[1].Value);

        Assert.Equal(TokenType.NUMBER, tokens[2].Type);
        Assert.Equal(varValue, tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);
        Assert.Equal(";", tokens[3].Value);

        Assert.Equal(TokenType.EOF, tokens[4].Type);
        Assert.Equal("", tokens[4].Value);
    }

    [Theory]
    [InlineData("@_tmp = 1;")]        // começa com underscore
    [InlineData("@2fast = 1;")]       // começa com dígito
    [InlineData("@$var = 1;")]        // começa com símbolo
    [InlineData("@ = 1;")]            // referência vazia (@ seguido de espaço)
    [InlineData("@;")]                // @ seguido direto de símbolo
    public void TokenizeInvalidInputThrows(string input)
    {
        Assert.Throws<LexerException>(() => new Lexer(input).Tokenize());
    }

    [Fact]
    public void TokenizeReservedWords()
    {
        var tokens = new Lexer("loop repeat 2 { play }").Tokenize();
        Assert.Equal(TokenType.LOOP, tokens[0].Type);
    }

    [Theory]
    [InlineData("loop", TokenType.LOOP)]
    [InlineData("play", TokenType.PLAY)]
    [InlineData("song", TokenType.SONG)]
    public void TokenizeKeywordsReturnsCorrespondingTokenType(string keyword, TokenType expected)
    {
        var tokens = new Lexer(keyword).Tokenize();

        Assert.Equal(expected, tokens[0].Type);
        Assert.Equal(keyword, tokens[0].Value);
        Assert.Equal(TokenType.EOF, tokens[1].Type);
    }

    [Theory]
    [InlineData("bpm")]
    [InlineData("track1")]
    [InlineData("my_track")]
    [InlineData("my_track_2")]
    [InlineData("a1_2b")]
    public void TokenizeIdentifiersLetterFollowedByLettersDigitsUnderscore_ReturnsIdent(string word)
    {
        var tokens = new Lexer(word).Tokenize();

        Assert.Equal(TokenType.IDENT, tokens[0].Type);
        Assert.Equal(word, tokens[0].Value);
        Assert.Equal(TokenType.EOF, tokens[1].Type);
    }

    [Fact]
    public void TokenizeIdentifierStopsAtNonWordCharacter()
    {
        var tokens = new Lexer("track1;").Tokenize();

        Assert.Equal(TokenType.IDENT, tokens[0].Type);
        Assert.Equal("track1", tokens[0].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[1].Type);
    }

    [Theory]
    [InlineData("(", TokenType.LPAREN)]
    [InlineData(")", TokenType.RPAREN)]
    [InlineData("{", TokenType.LBRACE)]
    [InlineData("}", TokenType.RBRACE)]
    [InlineData("=", TokenType.EQUALS)]
    [InlineData(";", TokenType.SEMICOLON)]
    public void TokenizeSymbolsReturnsCorrespondingTokenType(string symbol, TokenType expected)
    {
        var tokens = new Lexer(symbol).Tokenize();

        Assert.Equal(expected, tokens[0].Type);
        Assert.Equal(symbol, tokens[0].Value);
        Assert.Equal(TokenType.EOF, tokens[1].Type);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("7", "7")]
    [InlineData("129", "129")]
    [InlineData("00042", "42")]
    public void TokenizeNumbersReturnsNumberToken(string input, string expectedValue)
    {
        var tokens = new Lexer(input).Tokenize();

        Assert.Equal(TokenType.NUMBER, tokens[0].Type);
        Assert.Equal(expectedValue, tokens[0].Value);
    }

    [Fact]
    public void TokenizeEmptyInputReturnsOnlyEof()
    {
        var tokens = new Lexer("").Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.EOF, tokens[0].Type);
        Assert.Equal("", tokens[0].Value);
    }

    [Fact]
    public void TokenizeWhitespaceOnlyInputReturnsOnlyEof()
    {
        var tokens = new Lexer("   \t\n\n  ").Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.EOF, tokens[0].Type);
    }

    [Fact]
    public void TokenizeUnexpectedCharacterThrows()
    {
        Assert.Throws<LexerException>(() => new Lexer("#").Tokenize());
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void TokenizeBooleanKeywordsReturnsBooleanToken(string word)
    {
        var tokens = new Lexer(word).Tokenize();

        Assert.Equal(TokenType.BOOLEAN, tokens[0].Type);
        Assert.Equal(word, tokens[0].Value);
        Assert.Equal(TokenType.EOF, tokens[1].Type);
    }

    [Fact]
    public void TokenizeBooleanAssignment()
    {
        var tokens = new Lexer("@muted = true;").Tokenize();

        Assert.Equal(TokenType.REFERENCE, tokens[0].Type);
        Assert.Equal("muted", tokens[0].Value);

        Assert.Equal(TokenType.EQUALS, tokens[1].Type);
        Assert.Equal("=", tokens[1].Value);

        Assert.Equal(TokenType.BOOLEAN, tokens[2].Type);
        Assert.Equal("true", tokens[2].Value);

        Assert.Equal(TokenType.SEMICOLON, tokens[3].Type);
        Assert.Equal(";", tokens[3].Value);

        Assert.Equal(TokenType.EOF, tokens[4].Type);
        Assert.Equal("", tokens[4].Value);
    }

    [Theory]
    [InlineData("truee")]
    [InlineData("falsey")]
    [InlineData("TRUE")]
    [InlineData("False")]
    public void TokenizeWordsSimilarToBooleanReturnsIdent(string word)
    {
        var tokens = new Lexer(word).Tokenize();

        Assert.Equal(TokenType.IDENT, tokens[0].Type);
        Assert.Equal(word, tokens[0].Value);
        Assert.Equal(TokenType.EOF, tokens[1].Type);
    }

    [Fact]
    public void TokenizeBooleanInsideBeatParameter()
    {
        var tokens = new Lexer("beat @kick (muted=true) { 9 };").Tokenize();

        Assert.Equal(TokenType.BEAT, tokens[0].Type);
        Assert.Equal(TokenType.REFERENCE, tokens[1].Type);
        Assert.Equal(TokenType.LPAREN, tokens[2].Type);
        Assert.Equal(TokenType.IDENT, tokens[3].Type);
        Assert.Equal("muted", tokens[3].Value);
        Assert.Equal(TokenType.EQUALS, tokens[4].Type);

        Assert.Equal(TokenType.BOOLEAN, tokens[5].Type);
        Assert.Equal("true", tokens[5].Value);

        Assert.Equal(TokenType.RPAREN, tokens[6].Type);
    }

    [Fact]
    public void TokenizeParamWithoutValueReturnsIdentWithoutEqualsOrBoolean()
    {
        var tokens = new Lexer("(bpm=129, free)").Tokenize();

        Assert.Equal(TokenType.LPAREN, tokens[0].Type);

        Assert.Equal(TokenType.IDENT, tokens[1].Type);
        Assert.Equal("bpm", tokens[1].Value);

        Assert.Equal(TokenType.EQUALS, tokens[2].Type);

        Assert.Equal(TokenType.NUMBER, tokens[3].Type);
        Assert.Equal("129", tokens[3].Value);

        Assert.Equal(TokenType.COMMA, tokens[4].Type);

        // "free" não tem "=valor" depois, então o lexer só o enxerga como um
        // IDENT solto; não existe token BOOLEAN aqui — quem decide que um
        // parâmetro sem valor assume "true" é o parser, não o lexer.
        Assert.Equal(TokenType.IDENT, tokens[5].Type);
        Assert.Equal("free", tokens[5].Value);

        Assert.Equal(TokenType.RPAREN, tokens[6].Type);

        Assert.Equal(TokenType.EOF, tokens[7].Type);
    }

    [Fact]
    public void TokenizeFullSongBlockReturnsExpectedTokenSequence()
    {
        var tokens = new Lexer("song { loop repeat @times { play @track; } }").Tokenize();

        var expectedTypes = new[]
        {
            TokenType.SONG,
            TokenType.LBRACE,
            TokenType.LOOP,
            TokenType.IDENT,
            TokenType.REFERENCE,
            TokenType.LBRACE,
            TokenType.PLAY,
            TokenType.REFERENCE,
            TokenType.SEMICOLON,
            TokenType.RBRACE,
            TokenType.RBRACE,
            TokenType.EOF,
        };

        Assert.Equal(expectedTypes, tokens.Select(t => t.Type));
    }
}
