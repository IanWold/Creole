using Creole;

if (Calc.Expr is null || Sequences.Pair is null || Sequences.Captured is null || Queries.Independent is null || Queries.Dependent is null)
{
    return 1;
}

Maybe<int> just = 3;
if (just switch { int value => value, Nothing => 0 } != 3 || default(Maybe<int>) != new Nothing())
{
    return 1;
}

if (Tokens.Describe<Tok>(K.Comma) != "','" || Tokens.Describe<Plain>(K.Comma) != "Comma" || Tokens.Detail(new Tok(K.Word, "x")) != "x" || Tokens.Detail(new Plain(K.Word)) is not null)
{
    return 1;
}

var parsed = Lists.Depth.Parse("[[x]]") switch
{
    Parsed<int> result => result.Value,
    Recovered<int> => -1,
    Failed => -1,
};

if (parsed != 2
    || !Lists.Depth.Parse("[[y]]".AsMemory(), source: "smoke.txt").Diagnostics.Render().Contains(" --> smoke.txt:1:3", StringComparison.Ordinal)
    || Lists.Depth.Parse("[[x]]", options: new ParseOptions { MaxDepth = 1 }).Value is not Failed
)
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

    public static readonly Parser<char, string> Captured =
        Seq(_one.Optional(), _one.Slice(), _one.Text()).Select(static (sign, slice, text) => $"{sign}{slice}{text}");
}

internal class Queries : CharGrammar
{
    private static readonly Parser<char, int> _one = Return(1);
    private static readonly Parser<char, string> _word = Return("x");

    public static readonly Parser<char, string> Independent =
        from a in _one
        from b in _word
        where b.Length == a
        let c = b + a
        select c;

    public static readonly Parser<char, int> Dependent =
        from a in _one
        from b in _word
        from c in _word.Repeat(a)
        select c.Count + b.Length;
}

internal class Lists : CharGrammar
{
    public static readonly Parser<char, int> Depth =
        Char('[').Then(Rec(() => Depth)).Before(Char(']')).Select(static depth => depth + 1).Or(Char('x').As(0));
}

internal enum K
{
    Comma,
    Word,
}

internal readonly record struct Tok(K Kind, string Text) : IKinded<K>
{
    static string IKinded<K>.Describe(K kind) => kind == K.Comma ? "','" : "word";

    string? IKinded<K>.Detail => Kind == K.Word ? Text : null;
}

internal readonly record struct Plain(K Kind) : IKinded<K>;

internal static class Tokens
{
    public static string? Detail<TToken>(TToken token) where TToken : IKinded<K> => token.Detail;

    public static string Describe<TToken>(K kind) where TToken : IKinded<K> => TToken.Describe(kind);
}
