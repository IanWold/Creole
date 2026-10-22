using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file enum Symbol
{
    Number,
    Plus,
    Minus,
    Times,
}

file readonly record struct SymbolToken(Symbol Kind) : IKinded<Symbol>;

file sealed class Arithmetic : CharGrammar
{
    public static readonly Parser<char, int> Atom =
        Digit.Select(static c => c - '0').Or(Rec(() => Expr).Between(Char('('), Char(')')));

    public static readonly Parser<char, int> Expr = Expression.Build(Atom, op =>
    [
        [op.Prefix(Char('-'), static (_, x) => -x)],
        [op.InfixL(Char('*'), static (l, _, r) => l * r)],
        [op.InfixL(Char('+'), static (l, _, r) => l + r)],
    ]);

    public static readonly Parser<SymbolToken, Unit> Tokens =
        Grammar<SymbolToken>.OneOf(
            TokenGrammar<SymbolToken, Symbol>.Kind(Symbol.Number).As(1),
            TokenGrammar<SymbolToken, Symbol>.Kind(Symbol.Plus).As(2),
            TokenGrammar<SymbolToken, Symbol>.Kind(Symbol.Minus).As(3),
            TokenGrammar<SymbolToken, Symbol>.Kind(Symbol.Times).As(4))
        .SkipMany();
}

public class AllocationTests
{
    private static readonly Dictionary<string, Func<long>> _cases = new()
    {
        ["primitives in a sequence"] = () => Allocated(Seq(Digit, Char('+'), Any, String("let"), End), "1+xlet"),
        ["a chain of Select"] = () => Allocated(Digit.Select(static c => c - '0').Select(static d => d * 2), "7"),
        ["Optional"] = () => Allocated(Char('-').Optional().Then(Digit), "-1"),
        ["skipped runs, scanned and item by item"] = () => Allocated(Whitespace.SkipMany().Then(Satisfy(char.IsLetter).SkipAtLeastOnce()), "   abc"),
        ["a dispatched choice"] = () => Allocated(OneOf(Char('a').As(1), Char('b').As(2), Char('c').As(3), Char('d').As(4), Return(5)), "b"),
        ["a choice in order"] = () => Allocated(Char('a').Or(Return('r')), ""),
        ["Atomic, Lookahead, Not and Cut"] = () => Allocated(Seq(Lookahead(Char('a')), Atomic(Seq(Char('a'), Cut, Char('b'))), Not(Char('c'))), "ab"),
        ["Longest"] = () => Allocated(Longest(String("<"), String("<=")), "<="),
        ["Named and Context"] = () => Allocated(Digit.Named("digit").Context("in a number"), "5"),
        ["a slice of in-memory input"] = () => Allocated(Digit.SkipAtLeastOnce().Slice(), "123"),
        ["Where"] = () => Allocated(Digit.Where(static c => c != '0'), "5"),
        ["Switch"] = () => Allocated(Any.Switch(('a', Return(1)), ('b', Return(2)), @default: Return(0)), "b"),
        ["Rec and Expression.Build"] = () => Allocated(Arithmetic.Expr, "-(1+2)*3"),
        ["a dispatched choice over struct tokens"] = () => Allocated(Arithmetic.Tokens, [new(Symbol.Number), new(Symbol.Plus), new(Symbol.Number)]),
    };

    public static TheoryData<string> Cases => [.. _cases.Keys];

    private static long Allocated<T>(Parser<char, T> parser, string input)
    {
        var plan = PlanCache.Get(parser);
        var memory = input.AsMemory();

        return Allocated(() =>
        {
            var cursor = Cursor<char>.FromMemory(memory);
            T value = default!;
            return plan.TryParse(ref cursor, ref value) && cursor.IsAtEnd;
        });
    }

    private static long Allocated<TIn, T>(Parser<TIn, T> parser, TIn[] input)
    {
        var plan = PlanCache.Get(parser);

        return Allocated(() =>
        {
            var cursor = new Cursor<TIn>(input);
            T value = default!;
            return plan.TryParse(ref cursor, ref value) && cursor.IsAtEnd;
        });
    }
    private static long Allocated(Func<bool> parse)
    {
        for (var i = 0; i < 1_000; i++)
        {
            Assert.True(parse());
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 100; i++)
        {
            parse();
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void AParseOfValueTypesAllocatesNothing(string name) =>
        Assert.Equal(0, _cases[name]());
}
