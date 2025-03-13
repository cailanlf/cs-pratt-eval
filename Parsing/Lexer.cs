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

        while (Pos < Source.Length)
        {
            switch (Source.AsSpan(Pos))
            {
                case ['e', 'l', 's', 'e', ..]: EmitAdvance(TokenType.KwElse, 4); break;
                case ['t', 'h', 'e', 'n', ..]: EmitAdvance(TokenType.KwThen, 4); break;
                case ['e', 'n', 'd', ..]: EmitAdvance(TokenType.KwEnd, 3); break;
                case ['i', 'f', ..]: EmitAdvance(TokenType.KwIf, 2); break;
                case [':', '=', ..]: EmitAdvance(TokenType.Walrus, 2); break;
                case ['=', ..]: EmitSingle(TokenType.Equals, "("); break;
                case ['(', ..]: EmitSingle(TokenType.LParen, "("); break;
                case [')', ..]: EmitSingle(TokenType.RParen, ")"); break;
                case ['{', ..]: EmitSingle(TokenType.LBracket, "{"); break;
                case ['}', ..]: EmitSingle(TokenType.RBracket, "}"); break;
                case ['[', ..]: EmitSingle(TokenType.LBrace, content: "["); break;
                case [']', ..]: EmitSingle(TokenType.RBrace, "]"); break;
                case ['^', ..]: EmitSingle(TokenType.Caret, "^"); break;
                case ['+', ..]: EmitSingle(TokenType.Plus, "+"); break;
                case ['-', ..]: EmitSingle(TokenType.Minus, "-"); break;
                case ['/', ..]: EmitSingle(TokenType.Slash, "/"); break;
                case ['*', ..]: EmitSingle(TokenType.Asterisk, "*"); break;
                case ['!', ..]: EmitSingle(TokenType.Bang, "!"); break;
                case ['\n', ..]:
                case ['\t', ..]:
                case [' ', ..]:
                    _ = Consume();
                    break;
                case [>= '0' and <= '9', ..]: Emit(LexNumber()); break;
                case [>= 'a' and <= 'z' or >= 'A' and <= 'Z', ..]: 
                    Emit(LexIdentifier());
                    break;

                default: throw new NotImplementedException($"(lexer: base): unrecognized character '{Peek()}'");
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

        while (IsDigit(Peek()))
        {
            Consume();
        }

        return new Token
        {
            Type = TokenType.Number,
            Start = start,
            End = Pos,
            Content = Source[start..Pos],
        };
    }

    private Token LexIdentifier()
    {
        int start = Pos;

        while (char.IsLetterOrDigit(Peek()))
        {
            Consume();
        }

        return new Token
        {
            Type = TokenType.Identifier,
            Start = start,
            End = Pos,
            Content = Source[start..Pos],
        };
    }

    bool IsDigit(char c) => c is >= '0' and <= '9';

    private void Emit(Token t)
    {
        this._tokens.Add(t);
    }

    private void EmitAdvance(TokenType type, int advance)
    {
        var content = Consume(advance).ToString();
        Token t = new Token
        {
            Type = type,
            Start = Pos - advance,
            End = Pos,
            Content = content,
        };
        Emit(t);
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

    private ReadOnlySpan<char> PeekN(int n)
    {
        if (n + Pos < Source.Length)
        {
            return Source.AsSpan(Pos, n);
        } else
        {
            return Source.AsSpan(Pos, Source.Length - Pos);
        }
    }

    private char Consume()
    {
        char peeked = Peek();
        if (peeked == '\0') throw new IndexOutOfRangeException("(lexer: consume): attempted to consume character past end of source");

        Pos += 1;
        return peeked;
    }

    private ReadOnlySpan<char> Consume(int n)
    {
        var peeked = PeekN(n);
        if (peeked.Length < n) throw new IndexOutOfRangeException("(lexer: consume): attempted to consume character past end of source");

        Pos += n;
        return peeked;
    }
}