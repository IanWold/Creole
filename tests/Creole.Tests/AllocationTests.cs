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

    public static readonly Parser<char, int> Comparison = Expression.Build(Digit.Select(static c => c - '0'), op =>
    [
        [op.InfixN(Char('<'), static (l, _, r) => l < r ? 1 : 0)],
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
    private static readonly Dictionary<string, Func<bool, long>> _cases = new()
    {
        ["primitives in a sequence"] = recording => Allocated(Seq(Digit, Char('+'), Any, String("let"), End), "1+xlet", recording),
        ["a chain of Select"] = recording => Allocated(Digit.Select(static c => c - '0').Select(static d => d * 2), "7", recording),
        ["Optional"] = recording => Allocated(Char('-').Optional().Then(Digit), "-1", recording),
        ["skipped runs, scanned and item by item"] = recording => Allocated(Whitespace.SkipMany().Then(Satisfy(char.IsLetter).SkipAtLeastOnce()), "   abc", recording),
        ["a dispatched choice"] = recording => Allocated(OneOf(Char('a').As(1), Char('b').As(2), Char('c').As(3), Char('d').As(4), Return(5)), "b", recording),
        ["a choice in order"] = recording => Allocated(Char('a').Or(Return('r')), "", recording),
        ["Atomic, Lookahead, Not and Cut"] = recording => Allocated(Seq(Lookahead(Char('a')), Atomic(Seq(Char('a'), Cut, Char('b'))), Not(Char('c'))), "ab", recording),
        ["Longest"] = recording => Allocated(Longest(String("<"), String("<=")), "<=", recording),
        ["a compiled Longest with values from the match"] = recording => Allocated(Longest(Range('a', 'z').Select(static c => (int)c), String("<=").As(1), Digit.SkipAtLeastOnce().Slice().Select(static digits => digits.Length)), "123", recording),
        ["a compiled Longest whose winner runs again"] = recording => Allocated(Longest(Seq(Digit, Digit).Select(static (a, b) => a + b), Char('x').As(0)), "12", recording),
        ["Named and Context"] = recording => Allocated(Digit.Named("digit").Context("in a number"), "5", recording),
        ["a slice of in-memory input"] = recording => Allocated(Digit.SkipAtLeastOnce().Slice(), "123", recording),
        ["Where"] = recording => Allocated(Digit.Where(static c => c != '0'), "5", recording),
        ["Switch"] = recording => Allocated(Any.Switch(('a', Return(1)), ('b', Return(2)), @default: Return(0)), "b", recording),
        ["Rec and Expression.Build"] = recording => Allocated(Arithmetic.Expr, "-(1+2)*3", recording),
        ["a dispatched choice over struct tokens"] = recording => Allocated(Arithmetic.Tokens, [new(Symbol.Number), new(Symbol.Plus), new(Symbol.Number)], recording),
    };

    private static readonly Dictionary<string, Func<long>> _failures = new()
    {
        ["a character"] = () => Allocated(Char('a'), "b", recording: true, succeeds: false),
        ["a dispatched choice with labels and contexts"] = () => Allocated(OneOf(Char('a').Named("A"), Char('b'), Char('c').Context("in c"), Char('d')).Then(Char('!')), "x", recording: true, succeeds: false),
        ["contexts over many constructs"] = () => Allocated(Char('{').Then(Char('a').SkipMany()).Then(Char('}')).Context("in object").SkipMany().Then(End), string.Concat(Enumerable.Repeat("{a}", 50)) + "{ax", recording: true, succeeds: false),
        ["a rejected filter"] = () => Allocated(Digit.SkipAtLeastOnce().Slice().Where(static digits => digits.Length < 3, "too long"), "123", recording: true, succeeds: false),
        ["a chained operator"] = () => Allocated(Arithmetic.Comparison, "1<2<3", recording: true, succeeds: false),
        ["Lookahead and Not"] = () => Allocated(Seq(Lookahead(Char('a')), Not(Char('a'))), "a", recording: true, succeeds: false),
        ["a compiled Longest that matches and one that does not"] = () => Allocated(Longest(String("<"), String("<=")).Then(Longest(String("<"), String("<="))), "<x", recording: true, succeeds: false),
        ["Fail under Named"] = () => Allocated(Fail<int>("nope").Named("thing"), "x", recording: true, succeeds: false),
        ["a kind over struct tokens"] = () => Allocated(TokenGrammar<SymbolToken, Symbol>.Kind(Symbol.Plus), [new SymbolToken(Symbol.Number)], recording: true, succeeds: false),
    };

    private static Parsed<int>? _sink;

    public static TheoryData<string> Cases => [.. _cases.Keys];

    public static TheoryData<string> Failures => [.. _failures.Keys];

    private static long Allocated<T>(Parser<char, T> parser, string input, bool recording, bool succeeds = true)
    {
        var plan = PlanCache.Get(parser);
        var memory = input.AsMemory();
        var log = recording
            ? new FailureLog<char>()
            : null;

        return Allocated(() =>
        {
            log?.Clear();

            var cursor = Cursor<char>.FromMemory(memory) with { Failures = log };
            T value = default!;

            return (plan.TryParse(ref cursor, ref value) && cursor.IsAtEnd) == succeeds;
        });
    }

    private static long Allocated<TIn, T>(Parser<TIn, T> parser, TIn[] input, bool recording, bool succeeds = true)
    {
        var plan = PlanCache.Get(parser);
        var log = recording
            ? new FailureLog<TIn>()
            : null;

        return Allocated(() =>
        {
            log?.Clear();

            var cursor = new Cursor<TIn>(input) { Failures = log };
            T value = default!;

            return (plan.TryParse(ref cursor, ref value) && cursor.IsAtEnd) == succeeds;
        });
    }

    private static long Allocated(Func<bool> parse)
    {
        for (var i = 0; i < 1_000; i++)
        {
            Assert.True(parse());
        }

        return Allocation.Fewest(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                parse();
            }
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AFailedParseAllocatesTheSameWhateverTheLengthOfItsInput(bool inMemory)
    {
        var letters = Letter.SkipMany().Before(End);

        long AllocatedFor(int length)
        {
            var text = new string('a', length) + "1";
            var memory = text.ToCharArray().AsMemory();

            return Allocated(() =>
                (
                    inMemory
                    ? letters.Parse(memory)
                    : letters.Parse(text)
                )
                .Value is Failed
            );
        }

        Assert.Equal(AllocatedFor(1_000), AllocatedFor(100_000));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void AParseOfValueTypesAllocatesNothing(string name) =>
        Assert.Equal(0, _cases[name](false));

    [Fact]
    public void ASuccessfulParseAllocatesOnlyItsResult() =>
        Assert.Equal(
            Allocated(() => (_sink = new Parsed<int>(-9)) is not null),
            Allocated(() => Arithmetic.Expr.Parse("-(1+2)*3").Value is Parsed<int> { Value: -9 })
        );

    [Theory]
    [MemberData(nameof(Cases))]
    public void RecordingFailuresInAParseThatSucceedsAllocatesNothing(string name) =>
        Assert.Equal(0, _cases[name](true));

    [Theory]
    [MemberData(nameof(Failures))]
    public void RecordingFailuresInAParseThatFailsAllocatesNothing(string name) =>
        Assert.Equal(0, _failures[name]());
}
