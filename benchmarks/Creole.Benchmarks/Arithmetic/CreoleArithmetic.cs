using System.Globalization;

namespace Creole.Benchmarks;

public sealed class CreoleArithmetic : CharGrammar
{
    private static readonly Parser<char, Unit> _space = Whitespace.SkipMany();

    private static readonly Parser<char, char> _minus = Token(Char('-'));

    private static readonly Parser<char, double> _number = Token(Digit.SkipAtLeastOnce().Slice())
        .Select(static digits => (double)long.Parse(digits.Span, CultureInfo.InvariantCulture));

    private static readonly Parser<char, double> _expression = Expression.Build(
        _number.Or(Rec(() => _expression).Between(Token(Char('(')), Token(Char(')')))),
        op =>
        [
            [op.Prefix(_minus, static (_, x) => -x)],
            [op.InfixL(Token(Char('*')), static (l, _, r) => l * r), op.InfixL(Token(Char('/')), static (l, _, r) => l / r)],
            [op.InfixL(Token(Char('+')), static (l, _, r) => l + r), op.InfixL(_minus, static (l, _, r) => l - r)],
        ]
    );

    public static readonly Parser<char, double> Document = _space.Then(_expression).Before(End);

    private static Parser<char, T> Token<T>(Parser<char, T> parser) =>
        parser.Before(_space);

    public static double Evaluate(string text) => Document.Parse(text) switch
    {
        Parsed<double> parsed => parsed.Value,
        Recovered<double> recovered => throw new FormatException(recovered.Diagnostics.Render()),
        Failed failed => throw new FormatException(failed.Diagnostics.Render()),
    };
}
