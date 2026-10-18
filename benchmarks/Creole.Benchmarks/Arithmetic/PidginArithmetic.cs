using Pidgin;
using Pidgin.Expression;
using static Pidgin.Parser;

namespace Creole.Benchmarks.Arithmetic;

public static class PidginArithmetic
{
    public static readonly Parser<char, double> OperatorTable = BuildOperatorTable();

    public static readonly Parser<char, double> Folds = BuildFolds();

    public static double Evaluate(Parser<char, double> parser, string text) =>
        parser.ParseOrThrow(text);

    private static Parser<char, T> Tok<T>(Parser<char, T> parser) =>
        parser.Before(SkipWhitespaces);

    private static Parser<char, double> BuildOperatorTable()
    {
        static Parser<char, Func<double, double, double>> Binary(char op, Func<double, double, double> f) =>
            Tok(Char(op)).ThenReturn(f);

        var number = Tok(UnsignedLong(10)).Select(static n => (double)n);
        Parser<char, double> expression = null!;
        var group = Rec(() => expression).Between(Tok(Char('(')), Tok(Char(')')));

        expression = ExpressionParser.Build(
            number.Or(group),
            [
                Operator.PrefixChainable(Tok(Char('-')).ThenReturn<Func<double, double>>(static x => -x)),
                Operator.InfixL(Binary('*', static (a, b) => a * b)).And(Operator.InfixL(Binary('/', static (a, b) => a / b))),
                Operator.InfixL(Binary('+', static (a, b) => a + b)).And(Operator.InfixL(Binary('-', static (a, b) => a - b))),
            ]);

        return SkipWhitespaces.Then(expression).Before(Parser<char>.End);
    }

    private static Parser<char, double> BuildFolds()
    {
        var number = Tok(UnsignedLong(10)).Select(static n => (double)n);
        Parser<char, double> expression = null!;
        Parser<char, double> factor = null!;
        var group = Rec(() => expression).Between(Tok(Char('(')), Tok(Char(')')));
        factor = Tok(Char('-')).Then(Rec(() => factor)).Select(static x => -x).Or(number).Or(group);

        var term = Map(
            static (first, rest) => rest.Aggregate(first, static (acc, r) => r.Op == '*' ? acc * r.Value : acc / r.Value),
            factor,
            Map(static (op, value) => (Op: op, Value: value), Tok(OneOf('*', '/')), factor).Many()
        );

        expression = Map(
            static (first, rest) => rest.Aggregate(first, static (acc, r) => r.Op == '+' ? acc + r.Value : acc - r.Value),
            term,
            Map(static (op, value) => (Op: op, Value: value), Tok(OneOf('+', '-')), term).Many()
        );

        return SkipWhitespaces.Then(expression).Before(Parser<char>.End);
    }
}
