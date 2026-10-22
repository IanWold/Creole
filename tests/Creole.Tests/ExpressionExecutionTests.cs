using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed class Grouping : CharGrammar
{
    public static readonly Parser<char, string> Atom =
        LetterOrDigit.Select(static c => c.ToString())
        .Or(Rec(() => Expr).Between(Char('('), Char(')')));

    public static readonly Parser<char, string> Expr = Expression.Build(Atom, op =>
    [
        [op.Postfix(Char('!'), (x, _) => $"({x}!)")],
        [op.InfixR(Char('^'), (l, _, r) => $"({l}^{r})")],
        [op.Prefix(Char('-'), (_, x) => $"(-{x})")],
        [op.InfixL(Char('*'), (l, _, r) => $"({l}*{r})"), op.InfixL(Char('/'), (l, _, r) => $"({l}/{r})")],
        [op.InfixL(Char('+'), (l, _, r) => $"({l}+{r})"), op.InfixL(Char('-'), (l, _, r) => $"({l}-{r})")],
        [op.InfixN(Char('<'), (l, _, r) => $"({l}<{r})")],
    ]);
}

file sealed class Calc : CharGrammar
{
    public static readonly Parser<char, double> Atom =
        Digit.Select(static c => (double)(c - '0'))
        .Or(Rec(() => Expr).Between(Char('('), Char(')')));

    public static readonly Parser<char, double> Expr = Expression.Build(Atom, op =>
    [
        [op.Postfix(Char('!'), (x, _) => Factorial(x))],
        [op.InfixR(Char('^'), (l, _, r) => Math.Pow(l, r))],
        [op.Prefix(Char('-'), (_, x) => -x)],
        [op.InfixL(Char('*'), (l, _, r) => l * r), op.InfixL(Char('/'), (l, _, r) => l / r)],
        [op.InfixL(Char('+'), (l, _, r) => l + r), op.InfixL(Char('-'), (l, _, r) => l - r)],
        [op.InfixN(Char('<'), (l, _, r) => l < r ? 1 : 0)],
    ]);

    private static double Factorial(double x) => x <= 1 ? 1 : x * Factorial(x - 1);
}

public class ExpressionExecutionTests
{
    private static readonly Outcome _failConsumed = new Outcome.FailConsumed();
    private static readonly Outcome _failEmpty = new Outcome.FailEmpty();
    private static readonly Parser<char, string> _letter = Letter.Select(static c => c.ToString());

    public static TheoryData<string, string, int> Groupings => new()
    {
        { "a+b+c", "((a+b)+c)", 5 },
        { "a-b-c", "((a-b)-c)", 5 },
        { "a^b^c", "(a^(b^c))", 5 },
        { "a+b*c", "(a+(b*c))", 5 },
        { "a*b+c", "((a*b)+c)", 5 },
        { "a--b", "(a-(-b))", 4 },
        { "-a-b", "((-a)-b)", 4 },
        { "-a*b", "((-a)*b)", 4 },
        { "a*-b", "(a*(-b))", 4 },
        { "2^-x", "(2^(-x))", 4 },
        { "-x^2", "(-(x^2))", 4 },
        { "2^-x+1", "((2^(-x))+1)", 6 },
        { "--x", "(-(-x))", 3 },
        { "x!!", "((x!)!)", 3 },
        { "-x!", "(-(x!))", 3 },
        { "a<b", "(a<b)", 3 },
        { "a<b+c", "(a<(b+c))", 5 },
        { "(a<b)<c", "((a<b)<c)", 7 },
        { "(a+b)*c", "((a+b)*c)", 7 },
        { "a", "a", 1 },
        { "a+b)", "(a+b)", 3 },
        { "a b", "a", 1 },
    };

    public static TheoryData<string, double> Values => new()
    {
        { "2^3^2", 512 },
        { "-2^2", -4 },
        { "2^-1", 0.5 },
        { "3!", 6 },
        { "-3!", -6 },
        { "--3", 3 },
        { "9-2-3", 4 },
        { "2*3+4", 10 },
        { "(1+2)*3", 9 },
        { "1<2", 1 },
        { "2<1", 0 },
    };

    private static Outcome.Ok Ok(string value, int consumed) => new(value, consumed);

    private static (bool Ok, long Position, bool Cut) Run<T>(Parser<char, T> parser, string input)
    {
        var cursor = new Cursor<char>(input);
        var (ok, _) = Execution.Run(parser, ref cursor);

        return (ok, cursor.Position, cursor.Cut);
    }

    [Fact]
    public void AChainedNonAssociativeOperatorFailsAfterATighterOperator() =>
        Assert.Equal(_failConsumed, Execution.Outcome(Grouping.Expr, "a<b+c<d"));

    [Fact]
    public void AChainedNonAssociativeOperatorFailsCommittedAfterThatOperator() =>
        Assert.Equal((false, 4, false), Run(Grouping.Expr, "a<b<c"));

    [Fact]
    public void AMissingOperandAfterAnOperatorIsACommittedFailure() =>
        Assert.Equal((false, 2, false), Run(Grouping.Expr, "a+"));

    [Fact]
    public void AnAtomThatPassesACutAndFailsWithoutConsumingDropsTheCut()
    {
        var expression = Expression.Build(new CutThenNode<string>(_letter), op => [[op.Prefix(Char('-'), (_, x) => $"(-{x})")]]);

        Assert.Equal((false, 1, false), Run(expression, "-1"));
    }

    [Fact]
    public void AnEmptyInfixOperatorWhoseOperandFailsWithoutConsumingEndsTheExpression()
    {
        var juxtaposition = Expression.Build(_letter, op => [[op.InfixL(Return(' '), (l, _, r) => $"({l} {r})")]]);

        Assert.Equal(Ok("((a b) c)", 3), Execution.Outcome(juxtaposition, "abc"));
        Assert.Equal(Ok("a", 1), Execution.Outcome(juxtaposition, "a."));
    }

    [Theory]
    [InlineData("")]
    [InlineData("+")]
    public void AnExpressionWithoutAnOperandFailsWithoutConsuming(string input) =>
        Assert.Equal(_failEmpty, Execution.Outcome(Grouping.Expr, input));

    [Theory]
    [InlineData("-")]
    [InlineData("a^")]
    [InlineData("a+-")]
    [InlineData("(a+b")]
    public void AnIncompleteExpressionFailsCommitted(string input) =>
        Assert.Equal(_failConsumed, Execution.Outcome(Grouping.Expr, input));

    [Fact]
    public void AnInfixOperatorAndOperandThatConsumeNothingThrow()
    {
        var expression = Expression.Build(_letter.Or(Return("e")), op => [[op.InfixL(Return(' '), (l, _, r) => $"({l} {r})")]]);

        Assert.Equal(new Outcome.Throws(), Execution.Outcome(expression, ""));
    }

    [Fact]
    public void AnOperatorAttemptThatPassesACutAndFailsWithoutConsumingDropsTheCut()
    {
        var expression = Expression.Build(_letter, op => [[op.InfixL(new CutThenNode<char>(Char('+')), (l, _, r) => $"({l}+{r})")]]);

        Assert.Equal((true, 1, false), Run(expression, "a"));
        Assert.Equal((true, 3, true), Run(expression, "a+b"));
    }

    [Fact]
    public void AnOperatorThatFailsAfterConsumingFailsTheExpression()
    {
        var expression = Expression.Build(_letter, op => [[op.InfixL(Seq(Char('*'), Char('*')), (l, _, r) => $"({l}**{r})")]]);

        Assert.Equal(Ok("(a**b)", 4), Execution.Outcome(expression, "a**b"));
        Assert.Equal(_failConsumed, Execution.Outcome(expression, "a*b"));
    }

    [Fact]
    public void AnOperatorThatLooksAheadForAnOperandStopsBeforeAMissingOne()
    {
        var expression = Expression.Build(_letter, op => [[op.InfixL(Atomic(Char('-').Before(Lookahead(_letter))), (l, _, r) => $"({l}-{r})")]]);

        Assert.Equal(Ok("a", 1), Execution.Outcome(expression, "a-"));
        Assert.Equal(Ok("(a-b)", 3), Execution.Outcome(expression, "a-b"));
    }

    [Fact]
    public void APrefixAttemptThatPassesACutAndFailsWithoutConsumingDropsTheCut()
    {
        var expression = Expression.Build(_letter, op => [[op.Prefix(new CutThenNode<char>(Char('-')), (_, x) => $"(-{x})")]]);

        Assert.Equal((true, 1, false), Run(expression, "a"));
        Assert.Equal((true, 2, true), Run(expression, "-a"));
    }

    [Fact]
    public void APrefixAtTheSameLevelAsAnInfixOperatorAppliesFirst()
    {
        var expression = Expression.Build(_letter, op => [[op.Prefix(Char('-'), (_, x) => $"(-{x})"), op.InfixL(Char('*'), (l, _, r) => $"({l}*{r})")]]);

        Assert.Equal(Ok("((-a)*b)", 4), Execution.Outcome(expression, "-a*b"));
        Assert.Equal(Ok("(a*(-b))", 4), Execution.Outcome(expression, "a*-b"));
    }

    [Fact]
    public void APrefixAtTheSameLevelAsAPostfixAppliesFirst()
    {
        var expression = Expression.Build(_letter, op => [[op.Prefix(Char('-'), (_, x) => $"(-{x})"), op.Postfix(Char('!'), (x, _) => $"({x}!)")]]);

        Assert.Equal(Ok("((-x)!)", 3), Execution.Outcome(expression, "-x!"));
        Assert.Equal(Ok("(((-(-x))!)!)", 5), Execution.Outcome(expression, "--x!!"));
    }

    [Fact]
    public void APrefixOrPostfixOperatorThatConsumesNothingThrows()
    {
        var prefix = Expression.Build(_letter, op => [[op.Prefix(Return('?'), (_, x) => x)]]);
        var postfix = Expression.Build(_letter, op => [[op.Postfix(Return('?'), (x, _) => x)]]);

        Assert.Equal(new Outcome.Throws(), Execution.Outcome(prefix, "a"));
        Assert.Equal(new Outcome.Throws(), Execution.Outcome(postfix, "a"));
    }

    [Theory]
    [MemberData(nameof(Groupings))]
    public void OperatorsGroupByLevelAndAssociativity(string input, string grouped, int consumed) =>
        Assert.Equal(Ok(grouped, consumed), Execution.Outcome(Grouping.Expr, input));

    [Theory]
    [MemberData(nameof(Values))]
    public void TheWhitepapersCalculatorEvaluates(string input, double value) =>
        Assert.Equal(Ok(value.ToString(System.Globalization.CultureInfo.InvariantCulture), input.Length), Execution.Outcome(Calc.Expr, input, v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)));
}
