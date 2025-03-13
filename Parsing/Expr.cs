using System.Numerics;

record class Expr
{
}

record class UnaryOpExpr : Expr
{
    public required Expr Operand { get; init; }
    public required Operator Op { get; init; }

    public enum Operator
    {
        Negative,
        Positive,
        Factorial
    }
}

record class BinaryOpExpr : Expr
{
    public required Expr Left { get; init; }
    public required Expr Right { get; init; }
    public required Operator Op { get; init; }

    public enum Operator
    {
        Add,
        Subtract,
        Divide,
        Multiply,
        Exponent,
        Assign
    }
}

record class ParenExpr : Expr
{
    public required Expr Expr { get; init; }
}

record class NumberExpr : Expr
{
    public required BigInteger Value;
}

record class IfElseExpr : Expr
{
    public required Expr Condition { get; init; }
    public required Expr Then { get; init; }
    public required Expr Else { get; init; }
}

record class IdentifierExpr : Expr
{
    public required string Identifier { get; init; }
}