using System.Numerics;
using System.Reflection.Metadata.Ecma335;

namespace cs_pratt.Parsing;

class Parser
{
    public IReadOnlyList<Token> Tokens { get; init; }
    public int Pos { get; private set; }

    public Parser(IReadOnlyList<Token> tokens)
    {
        Tokens = tokens;
        Pos = 0;
    }

    public Expr Parse()
    {
        var expr = ParseExpr();
        if (Peek()?.Type is not null)
        {
            string message = $"(parsing: parse-top-level): expected end of input, got {Peek()?.Type}";

            throw new Exception(message);
        }
        return expr;
    }

    Expr ParseExpr()
    {
        return ParseExprBinding(0);
    }

    Expr ParseExprBinding(int minBP)
    {
        var prefixPrec = Precedence.GetUnaryPrecedencePrefix(Peek());
        Expr left;

        if (prefixPrec != 0 && prefixPrec >= minBP)
        {
            var op = Consume();
            var right = ParseExprBinding(prefixPrec);
            left = new UnaryOpExpr
            {
                Op = op.Type switch
                {
                    TokenType.Plus => UnaryOpExpr.Operator.Positive,
                    TokenType.Minus => UnaryOpExpr.Operator.Negative,
                    _ => throw new Exception($"Invalid prefix operator {op.Type}"),
                },
                Operand = right,
            };
        } else
        {
            left = ParseAtomicExpr();
        }

        while (true) {
            var postfixPrec = Precedence.GetUnaryPrecedencePostfix(Peek());

            if (postfixPrec == 0 || postfixPrec < minBP) { break; }

            var op = Consume();
            var operand = left;
            left = new UnaryOpExpr
            {
                Op = op.Type switch
                {
                    TokenType.Bang => UnaryOpExpr.Operator.Factorial,
                    _ => throw new Exception($"Invalid postfix operator {op.Type}"),
                },
                Operand = operand,
            };
        }

        while (true) {
            var (leftPrec, rightPrec) = Precedence.GetBinaryPrecedence(Peek());
            if (leftPrec == 0 || leftPrec < minBP) { break; }

            var op = Consume();
            var right = ParseExprBinding(rightPrec);
            left = new BinaryOpExpr
            {
                Left = left,
                Op = op.Type switch
                {
                    TokenType.Plus => BinaryOpExpr.Operator.Add,
                    TokenType.Minus => BinaryOpExpr.Operator.Subtract,
                    TokenType.Asterisk => BinaryOpExpr.Operator.Multiply,
                    TokenType.Slash => BinaryOpExpr.Operator.Divide,
                    TokenType.Caret => BinaryOpExpr.Operator.Exponent,
                    TokenType.Walrus => BinaryOpExpr.Operator.Assign,
                    _ => throw new Exception($"Invalid binary operator {op.Type}"),
                },
                Right = right,
            };
        }

        return left;
    }

    Expr ParseAtomicExpr()
    {
        switch (Peek()) {
            case { Type: TokenType.Identifier }:
                var identifier = Consume();
                return new IdentifierExpr
                {
                    Identifier = identifier.Content!
                };

            case { Type: TokenType.Number }:
                return EmitNumber();

            case { Type: TokenType.LParen }:
                var left = Consume();
                var expr = ParseExpr();
                var right = Consume();
                if (right.Type is not TokenType.RParen)
                {
                    throw new Exception($"expected closing parenthesis, got {right.Type}");
                }

                return new ParenExpr {
                    Expr = expr
                };

            case { Type: TokenType.KwIf }:
                var kwIf = Consume();
                var condition = ParseExpr();

                var kwThen = Consume();
                if (kwThen.Type is not TokenType.KwThen)
                {
                    throw new Exception($"expected 'then', got {kwThen.Type}");
                }
                var thenExpr = ParseExpr();

                var kwElse = Consume();
                if (kwElse.Type is not TokenType.KwElse)
                {
                    throw new Exception($"expected 'else', got {kwElse.Type}");
                }
                var elseExpr = ParseExpr();

                var kwEnd = Consume();
                if (kwEnd.Type is not TokenType.KwEnd)
                {
                    throw new Exception($"expected 'end', got {kwEnd.Type}");
                }

                return new IfElseExpr
                {
                    Condition = condition,
                    Then = thenExpr,
                    Else = elseExpr,
                };

            default:
                throw new Exception($"Expected an atomic expression, but got {Peek()?.Type}");
        }
    }

    Expr EmitNumber()
    {
        if (Peek()?.Type is not TokenType.Number)
        {
            throw new Exception($"Expected a number, but got {Peek()?.Type}");
        }

        return new NumberExpr
        {
            Value = BigInteger.Parse(Consume().Content!)
        };
    }

    Token? Peek()
        => Pos < Tokens.Count
            ? Tokens[Pos] 
            : null;

    Token Consume()
    {
        var token = Peek();
        if (token is null) throw new Exception("Consumed past end of input");
        Pos += 1;
        return token;
    }
}

static class Precedence
{
    public static int Level_Assignment =      0;
    public static int Level_Addition =        1;
    public static int Level_Multiplication =  2;
    public static int Level_Negate =          3;
    public static int Level_Exponent =        4;
    public static int Level_Factorial =       5;

    public static (int, int) LeftAssociative(int level)
        => ((level + 1) * 2 - 1, (level + 1) * 2);

    public static (int, int) RightAssociative(int level)
        => ((level + 1) * 2, (level + 1) * 2 - 1);

    public static int Unary(int level)
        => (level + 1) * 2;

    public static int GetUnaryPrecedencePrefix(Token? t) =>
        t?.Type switch
        {
            TokenType.Minus or TokenType.Plus 
                => Unary(Level_Negate),
            _ => 0,
        };

    public static int GetUnaryPrecedencePostfix(Token? t) =>
        t?.Type switch
        {
            TokenType.Bang
                => Unary(Level_Factorial),
            _ => 0,
        };

    public static (int, int) GetBinaryPrecedence(Token? t) =>
        t?.Type switch
        {
            TokenType.Plus or TokenType.Minus
                => LeftAssociative(Level_Addition),
            TokenType.Asterisk or TokenType.Slash
                => LeftAssociative(Level_Multiplication),
            TokenType.Caret
                => RightAssociative(Level_Exponent),
            TokenType.Walrus
                => RightAssociative(Level_Assignment),
            _ => (0, 0),
        };
}
