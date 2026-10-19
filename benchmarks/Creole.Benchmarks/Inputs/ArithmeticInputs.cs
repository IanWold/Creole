using System.Globalization;
using System.Text;

namespace Creole.Benchmarks;

public static class ArithmeticInputs
{
    public const int Count = 1_000;

    private static readonly Lazy<string[]> _lazyExpressions = new(() => Generate(seed: 4, Count));

    public static IReadOnlyList<string> Expressions =>
        _lazyExpressions.Value;

    private static string[] Generate(int seed, int count)
    {
        var random = new Random(seed);
        var expressions = new string[count];
        var builder = new StringBuilder();

        for (var i = 0; i < count; i++)
        {
            builder.Clear();
            WriteExpression(builder, random, depth: 0);
            expressions[i] = builder.ToString();
        }

        return expressions;
    }

    private static void WriteExpression(StringBuilder builder, Random random, int depth)
    {
        var terms = random.Next(1, 5);

        for (var i = 0; i < terms; i++)
        {
            if (i > 0)
            {
                builder.Append(' ').Append("+-*/"[random.Next(4)]).Append(' ');
            }

            WriteOperand(builder, random, depth);
        }
    }

    private static void WriteOperand(StringBuilder builder, Random random, int depth)
    {
        if (random.Next(100) < 10)
        {
            builder.Append('-');
        }

        if (depth < 4 && random.Next(100) < 25)
        {
            builder.Append('(');
            WriteExpression(builder, random, depth + 1);
            builder.Append(')');
        }
        else
        {
            builder.Append(random.Next(0, 10_000).ToString(CultureInfo.InvariantCulture));
        }
    }
}
