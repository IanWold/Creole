using Creole;

if (Calc.Expr is null || Sequences.Pair is null)
{
    return 1;
}

Console.WriteLine("Creole AOT smoke: OK");
return 0;

internal class Calc : CharGrammar
{
    private static readonly Parser<char, double> _atom = Return(1.0);
    private static readonly Parser<char, char> _minus = Return('-');

    public static readonly Parser<char, double> Expr = Expression.Build(_atom, op =>
    [
        [op.Postfix(Return(1), (x, n) => x + n)],
        [op.InfixR(Return('^'), (l, _, r) => Math.Pow(l, r))],
        [op.Prefix(_minus, (_, x) => -x)],
        [op.InfixL(Return('*'), (l, _, r) => l * r)],
        [op.InfixL(_minus, (l, _, r) => l - r)],
        [op.InfixN(Return(0L), (l, _, r) => l < r ? 1 : 0)],
    ]);
}

internal class Sequences : CharGrammar
{
    private static readonly Parser<char, string> _comma = Return(",");
    private static readonly Parser<char, int> _one = Return(1);

    public static readonly Parser<char, string> Pair =
        Seq(_one.Before(_comma), _one.Then(_comma), _one.Between(_comma, _comma), _one.As(2L), _one.Ignore())
            .Select(static (a, b, c, d, _) => $"{a}{b}{c}{d}")
            .Select(static text => text + "!");
}
