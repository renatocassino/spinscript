using SpinScript.Lexer;
using Xunit;

public class TokenizerTests
{
    [Fact]
    public void TokenizeSimpleAssignment_ReturnsCorrectTokens()
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


    [Theory]
    [InlineData("@bpm = 129;\n")]
    [InlineData("    @bpm    =    129    ;     \n\n")]
    [InlineData("\n@bpm\n=\n129\n;\n")]
    [InlineData("\t@bpm \t= \t129;\t\n")]
    public void TokenizeSimpleAssignment_WithDifferentChars(string input)
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
    public void TokenizeInvalidInput_Throws(string input)
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
    [InlineData("repeat", TokenType.REPEAT)]
    public void TokenizeKeywords_ReturnsCorrespondingTokenType(string keyword, TokenType expected)
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
    public void TokenizeIdentifiers_LetterFollowedByLettersDigitsUnderscore_ReturnsIdent(string word)
    {
        var tokens = new Lexer(word).Tokenize();

        Assert.Equal(TokenType.IDENT, tokens[0].Type);
        Assert.Equal(word, tokens[0].Value);
        Assert.Equal(TokenType.EOF, tokens[1].Type);
    }

    [Fact]
    public void TokenizeIdentifier_StopsAtNonWordCharacter()
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
    public void TokenizeSymbols_ReturnsCorrespondingTokenType(string symbol, TokenType expected)
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
    public void TokenizeNumbers_ReturnsNumberToken(string input, string expectedValue)
    {
        var tokens = new Lexer(input).Tokenize();

        Assert.Equal(TokenType.NUMBER, tokens[0].Type);
        Assert.Equal(expectedValue, tokens[0].Value);
    }

    [Fact]
    public void TokenizeEmptyInput_ReturnsOnlyEof()
    {
        var tokens = new Lexer("").Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.EOF, tokens[0].Type);
        Assert.Equal("", tokens[0].Value);
    }

    [Fact]
    public void TokenizeWhitespaceOnlyInput_ReturnsOnlyEof()
    {
        var tokens = new Lexer("   \t\n\n  ").Tokenize();

        Assert.Single(tokens);
        Assert.Equal(TokenType.EOF, tokens[0].Type);
    }

    [Fact]
    public void TokenizeUnexpectedCharacter_Throws()
    {
        Assert.Throws<LexerException>(() => new Lexer("#").Tokenize());
    }

    [Fact]
    public void TokenizeFullSongBlock_ReturnsExpectedTokenSequence()
    {
        var tokens = new Lexer("song { loop repeat @times { play @track; } }").Tokenize();

        var expectedTypes = new[]
        {
            TokenType.SONG,
            TokenType.LBRACE,
            TokenType.LOOP,
            TokenType.REPEAT,
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
