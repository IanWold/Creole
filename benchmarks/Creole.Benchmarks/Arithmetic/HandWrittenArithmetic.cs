using System.Globalization;

namespace Creole.Benchmarks.Arithmetic;

public static class HandWrittenArithmetic
{
    public static double Evaluate(string text)
    {
        var position = 0;
        var result = Expression(text, ref position);

        SkipSpaces(text, ref position);

        return position == text.Length
            ? result
            : throw Error(position);
    }

    private static double Expression(string text, ref int position)
    {
        var left = Term(text, ref position);

        while (true)
        {
            SkipSpaces(text, ref position);

            if (position >= text.Length || text[position] is not ('+' or '-'))
            {
                return left;
            }

            var op = text[position++];
            var right = Term(text, ref position);

            left = op == '+' ? left + right : left - right;
        }
    }

    private static double Term(string text, ref int position)
    {
        var left = Factor(text, ref position);

        while (true)
        {
            SkipSpaces(text, ref position);

            if (position >= text.Length || text[position] is not ('*' or '/'))
            {
                return left;
            }

            var op = text[position++];
            var right = Factor(text, ref position);

            left = op == '*' ? left * right : left / right;
        }
    }

    private static double Factor(string text, ref int position)
    {
        SkipSpaces(text, ref position);
        if (position >= text.Length)
        {
            throw Error(position);
        }

        switch (text[position])
        {
            case '-':
                position++;
                return -Factor(text, ref position);

            case '(':
                position++;

                var inner = Expression(text, ref position);

                SkipSpaces(text, ref position);

                if (position >= text.Length || text[position] != ')')
                {
                    throw Error(position);
                }

                position++;

                return inner;

            default:
                var start = position;

                while (position < text.Length && char.IsAsciiDigit(text[position]))
                {
                    position++;
                }

                return position > start
                    ? long.Parse(text.AsSpan(start, position - start), CultureInfo.InvariantCulture)
                    : throw Error(position);
        }
    }

    private static void SkipSpaces(string text, ref int position)
    {
        while (position < text.Length && text[position] == ' ')
        {
            position++;
        }
    }

    private static FormatException Error(int position) =>
        new($"Unexpected input at offset {position}.");
}
