using Xunit;

using SpinScript.Lexer;

public class TokenizerTests {
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
}
