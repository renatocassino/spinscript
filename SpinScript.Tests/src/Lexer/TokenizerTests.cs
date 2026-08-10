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
        Assert.Equal("EOF", tokens[4].Value);
    }
}
