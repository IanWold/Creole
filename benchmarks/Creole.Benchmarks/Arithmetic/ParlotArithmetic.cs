using Parlot.Fluent;

namespace Creole.Benchmarks;

public static class ParlotArithmetic
{
    public static readonly Parser<double> Interpreted = Build();

    private static Parser<double> Build()
    {
        var number = Terms.Integer().Then(static n => (double)n);
        var expression = Deferred<double>();
        var group = Between(Terms.Char('('), expression, Terms.Char(')'));
        var primary = number.Or(group);
        var unary = Parsers.Unary(primary, (Terms.Char('-'), static x => -x));
        var term = Parsers.LeftAssociative(unary, (Terms.Char('*'), static (a, b) => a * b), (Terms.Char('/'), static (a, b) => a / b));

        expression.Parser = Parsers.LeftAssociative(term, (Terms.Char('+'), static (a, b) => a + b), (Terms.Char('-'), static (a, b) => a - b));

        return expression.Eof();
    }

    public static double Evaluate(Parser<double> parser, string text) =>
        parser.TryParse(text, out var value)
        ? value
        : throw new FormatException("Parlot failed to parse an expression.");
}
