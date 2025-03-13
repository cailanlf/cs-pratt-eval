using cs_pratt.Parsing;
using System.Numerics;

var tree = false;
var printTokens = false;
Context ctx = new Context();

while (true)
{
    Console.Write("> ");
    string input = Console.ReadLine() ?? "";

    try
    {
        if (input.StartsWith('.'))
        {
            var split = input[1..].Split(" ");
            switch (split)
            {
                case ["tokens", ..]:
                    printTokens ^= true;
                    Console.WriteLine($"{(printTokens ? "enabled" : "disabled")} token printing");
                    break;
                case ["tree", ..]:
                    tree ^= true;
                    Console.WriteLine($"{(tree ? "enabled" : "disabled")} tree pretty printing");
                    break;
                case ["exit", ..]:
                    Environment.Exit(0);
                    break;
                case ["help", ..]:
                    Console.WriteLine(
                        """
                    .tree: show or hide the pretty-printed parse tree
                    .exit: exits the repl
                    .help: list available commands
                    """);
                    break;
                default:
                    throw new Exception($"unrecognized command {input}");
            }
            continue;
        }

        Lexer l = new Lexer(input);
        var tokens = l.Lex();

        if (printTokens)
        {
            foreach (var token in tokens)
            {
                Console.WriteLine(token);
            }
            Console.WriteLine("\n");
        }

        Parser p = new Parser(tokens);
        var root = p.Parse();

        if (tree)
        {
            PrettyPrint(root, "", true);
            Console.WriteLine("\n");
        }

        Console.WriteLine(input);
        Console.WriteLine($"= {Evaluate(root, ctx)}\n");
    } 
    catch (Exception e)
    {
        Console.WriteLine($"error: {e.Message}\n");
    }
}

static void PrettyPrint(Expr root, string indent, bool last)
{
    Console.Write($"{indent}");
    Console.Write(last ? "└── " : "├── ");
    indent = indent + (last ? "    " : "|   ");

    switch (root)
    {
        case NumberExpr ne when root is NumberExpr:
            Console.WriteLine(ne.Value);
            break;

        case UnaryOpExpr uoe when root is UnaryOpExpr:
            {
                var op = uoe.Op switch
                {
                    UnaryOpExpr.Operator.Negative => "-",
                    UnaryOpExpr.Operator.Positive => "+",
                    UnaryOpExpr.Operator.Factorial => "!",
                    _ => throw new NotImplementedException($"(pretty-print: unary): not implemented for unary operation {uoe.Op}"),
                };
                Console.WriteLine($"Unary {op}");
                PrettyPrint(uoe.Operand, indent, true);
            }
            break;

        case BinaryOpExpr boe when root is BinaryOpExpr:
            {
                var op = boe.Op switch
                {
                    BinaryOpExpr.Operator.Subtract => "-",
                    BinaryOpExpr.Operator.Add => "+",
                    BinaryOpExpr.Operator.Multiply => "*",
                    BinaryOpExpr.Operator.Divide => "/",
                    BinaryOpExpr.Operator.Exponent => "^",
                    BinaryOpExpr.Operator.Assign => ":=",
                    _ => throw new NotImplementedException($"(pretty-print: binary): not implemented for unary operation {boe.Op}"),
                };
                Console.WriteLine($"Binary {op}");
                PrettyPrint(boe.Left, indent, false);
                PrettyPrint(boe.Right, indent, true);
            }
            break;

        case ParenExpr pe when root is ParenExpr:
            Console.WriteLine("Parens");
            PrettyPrint(pe.Expr, indent, true);
            break;

        case IfElseExpr iee when root is IfElseExpr:
            Console.WriteLine("IfElse");
            PrettyPrint(iee.Condition, indent, false);
            PrettyPrint(iee.Then, indent, false);
            PrettyPrint(iee.Else, indent, true);
            break;

        case IdentifierExpr identifierExpr when root is IdentifierExpr:
            Console.WriteLine(identifierExpr.Identifier);
            break;

        default:
            throw new NotImplementedException($"(pretty-print: root): not implemented for {root.GetType()}");
    }
}
static BigInteger Evaluate(Expr root, Context ctx)
{
    static BigInteger Factorial(BigInteger value)
    {
        if (value == 0 || value == 1)
        {
            return 1;
        }
        else
        {
            BigInteger result = 1;
            for (BigInteger i = 2; i <= value; i++)
            {
                result *= i;
            }
            return result;
        }
    }

    switch (root)
    {
        case NumberExpr ne when root is NumberExpr:
            return ne.Value;

        case UnaryOpExpr uoe when root is UnaryOpExpr:
            var value = Evaluate(uoe.Operand, ctx);

            return uoe.Op switch
            {
                UnaryOpExpr.Operator.Factorial => Factorial(value),
                UnaryOpExpr.Operator.Negative => -value,
                UnaryOpExpr.Operator.Positive => +value,
                _ => throw new NotImplementedException($"(eval: unary): not implemented for {root.GetType()}"),
            };

        case BinaryOpExpr boe when root is BinaryOpExpr:
            var right = Evaluate(boe.Right, ctx);

            if (boe.Op is BinaryOpExpr.Operator.Assign)
            {
                if (boe.Left is not IdentifierExpr identifierExpr)
                {
                    throw new Exception($"(eval: binary): left side of assignment must be an identifier");
                }
                ctx.SetVariable(identifierExpr.Identifier, right);
                return right;
            }

            var left = Evaluate(boe.Left, ctx);

            return boe.Op switch
            {
                BinaryOpExpr.Operator.Add => left + right,
                BinaryOpExpr.Operator.Subtract => left - right,
                BinaryOpExpr.Operator.Multiply => left * right,
                BinaryOpExpr.Operator.Divide => left / right,
                BinaryOpExpr.Operator.Exponent => right < int.MaxValue 
                    ? BigInteger.Pow(left, (int)right)
                    : throw new Exception($"(eval: binary): exponent too large ({left} ^ {right})"),
                _ => throw new NotImplementedException($"(eval: binary): not implemented for {root.GetType()}"),
            };

        case ParenExpr pe when root is ParenExpr:
            return Evaluate(pe.Expr, ctx);

        case IfElseExpr iee when root is IfElseExpr:
            var condition = Evaluate(iee.Condition, ctx);
            return condition != 0 ? Evaluate(iee.Then, ctx) : Evaluate(iee.Else, ctx);

        case IdentifierExpr identifierExpr when root is IdentifierExpr:
            return ctx.GetVariable(identifierExpr.Identifier);

        default:
            throw new NotImplementedException($"(eval: root): not implemented for {root.GetType()}");
    }
}

class Context
{
    private Dictionary<string, BigInteger> _variables = new();
    public void SetVariable(string name, BigInteger value)
    {
        _variables[name] = value;
    }
    public BigInteger GetVariable(string name)
    {
        if (_variables.TryGetValue(name, out var value))
        {
            return value;
        }
        else
        {
            throw new Exception($"variable {name} not found");
        }
    }
}