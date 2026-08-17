namespace SpinScript.Lexer;

public enum TokenType
{
    BOOLEAN, // true, false
    FRACTION, // 1/4, 2/4, 1/2
    NOTE,
    STRING_LITERAL, // 'value' or "value"
    SEMICOLON,
    EQUALS,
    NUMBER,
    LPAREN,
    RPAREN,
    COMMA,
    LBRACE,
    RBRACE,
    LOOP,
    PLAY,
    EOF,
    PATTERN,
    REFERENCE,
    IDENT,
    SONG,
}
