using System.Runtime.Serialization;

namespace cs_pratt.Parsing;

class Lexer
{
    public string Source { get; init; }

    public int Pos { get; private set; }

    List<Token> _tokens;
    public IReadOnlyList<Token> Tokens => _tokens;

    public Lexer(string source)
    {
        this.Source = source;
    }

    public IReadOnlyList<Token> Lex()
    {
        this.Pos = 0;
        this._tokens = [];

        char peeked;
        while ((peeked = Peek()) != '\0')
        {
            switch (peeked)
            {
                case '(': EmitSingle(TokenType.LParen, "("); break;
                case ')': EmitSingle(TokenType.RParen, ")"); break;
                case '{': EmitSingle(TokenType.LBracket, "{"); break;
                case '}': EmitSingle(TokenType.RBracket, "}"); break;
                case '[': EmitSingle(TokenType.LBrace, "["); break;
                case ']': EmitSingle(TokenType.RBrace, "]"); break;
                case '^': EmitSingle(TokenType.Caret, "^"); break;
                case '+': EmitSingle(TokenType.Plus, "+"); break;
                case '-': EmitSingle(TokenType.Minus, "-"); break;
                case '/': EmitSingle(TokenType.Slash, "/"); break;
                case '*': EmitSingle(TokenType.Asterisk, "*"); break;
                case '!': EmitSingle(TokenType.Bang, "!"); break;
                case '\n':
                case '\t':
                case ' ':
                    _ = Consume();
                    break;

                case char n when IsDigit(n): Emit(LexNumber()); break;

                default: throw new NotImplementedException($"(lexer: base): unrecognized character '{peeked}'");
            }
        }

        //Emit(new Token
        //{
        //    Type = TokenType.Eof,
        //    Start = Pos,
        //    End = Pos,
        //    Content = string.Empty,
        //});

        return Tokens;
    }

    private Token LexNumber()
    {
        int start = Pos;
        List<char> tokens = [];

        while (IsDigit(Peek()))
        {
            tokens.Add(Consume());
        }

        return new Token
        {
            Type = TokenType.Number,
            Start = start,
            End = Pos,
            Content = string.Concat(tokens)
        };
    }

    bool IsDigit(char c) => c is >= '0' and <= '9';

    private void Emit(Token t)
    {
        this._tokens.Add(t);
    }

    private void EmitSingle(TokenType type, string content)
    {
        Consume();
        Token t = new()
        {
            Type = type,
            Start = Pos - 1,
            End = Pos,
            Content = content,
        };
        Emit(t);
    }

    private char Peek()
        => Pos < Source.Length
            ? Source[Pos]
            : '\0';

    private char Consume()
    {
        char peeked = Peek();
        if (peeked == '\0') throw new IndexOutOfRangeException("(lexer: consume): attempted to consume character past end of source");

        Pos += 1;
        return peeked;
    }
}