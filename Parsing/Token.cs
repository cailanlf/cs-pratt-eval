namespace cs_pratt.Parsing;

record class Token()
{
    public required TokenType Type { get; init; }
    public required string? Content {get; init;}
    public required int Start {get; init;}
    public required int End {get; init;}
}

enum TokenType
{
    LParen,
    RParen,
    LBrace,
    RBrace,
    LBracket,
    RBracket,
    Plus,
    Minus,
    Asterisk,
    Caret,
    Slash,
    Bang,
    Comma,
    Number,
}