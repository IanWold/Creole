using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file enum LexMode
{
    Operand,
    Operator,
    Block,
}

file enum K
{
    Number,
    LBrace,
    RBrace,
}

file readonly record struct Tok(K Kind) : IKinded<K>;

/// <summary>
/// The code sample in docs/concepts/mode-channels.md § Changing the mode. Keep the two in step.
/// </summary>
file class Script : TokenGrammar<Tok, K>
{
    public static readonly ModeChannel<LexMode> Modes = new(LexMode.Operand);

    public static readonly Parser<Tok, Tok> Operand =
        Kind(K.Number).Between(SetMode(Modes, LexMode.Operand), SetMode(Modes, LexMode.Operator));

    public static readonly Parser<Tok, Unit> Block =
        Kind(K.LBrace).Then(PushMode(Modes, LexMode.Block)).Then(Operand.SkipMany()).Before(Kind(K.RBrace)).Before(PopMode(Modes));
}

/// <summary>
/// A parser that consumes nothing and reports the mode log of the layer running it: how many entries it holds, and how many it has room for.
/// </summary>
file class LogSize(Action<int, int> seen) : Parser<char, Unit>
{
    private readonly Action<int, int> _seen = seen;

    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Return;

    internal override Plan<char, Unit> Lower(PlanBuilder builder) => new LogSizePlan(_seen);
}

/// <summary>
/// The plan of <see cref="LogSize"/>.
/// </summary>
file class LogSizePlan(Action<int, int> seen) : Plan<char, Unit>
{
    private readonly Action<int, int> _seen = seen;

    public override bool TryParse(ref Cursor<char> cursor, ref Unit result)
    {
        _seen(cursor.Modes?.Count ?? 0, cursor.Modes?.Capacity ?? 0);
        return true;
    }
}

/// <summary>
/// Mode changes in a parser run on its own, which keeps a stack for each channel that nothing reads (§ 3. Mode channels, ADR-0041 points 2 and 4).
/// </summary>
public class ModeChangeTests
{
    private enum Mode
    {
        Code,
        Text,
        Block,
    }

    private const string PoppedLast = "The parser layer pops the last mode of a ModeChannel<Mode>. Each PopMode needs a PushMode on the same channel before it.";

    private static readonly ModeChannel<Mode> _modes = new(Mode.Code);
    private static readonly Parser<char, Unit> _pushOrPop = OneOf(Char('p').Then(PushMode(_modes, Mode.Text)), Char('q').Then(PopMode(_modes))).SkipMany();

    private static long Allocated(Parser<char, Unit> parser)
    {
        for (var i = 0; i < 1_000; i++)
        {
            Assert.IsType<Parsed<Unit>>(parser.Parse("").Value);
        }

        return Allocation.Fewest(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                parser.Parse("");
            }
        });
    }

    private static int PeakLogSize(int length)
    {
        var peak = 0;
        var parser = SetMode(_modes, Mode.Text).Then(Char('a')).Then(SetMode(_modes, Mode.Code)).Then(new LogSize((count, _) => peak = Math.Max(peak, count))).SkipMany();

        Assert.IsType<Parsed<Unit>>(parser.Parse(new string('a', length)).Value);

        return peak;
    }

    private static Parser<char, Unit> Changes(int count) =>
        Enumerable.Range(0, count)
            .Select(index => SetMode(
                _modes,
                index % 2 == 0
                    ? Mode.Text
                    : Mode.Code
            ))
            .Aggregate(static (first, second) => first.Then(second));

    [Fact]
    public void ALogOverInputInMemoryHoldsNoMoreEntriesOverALongerInput()
    {
        var shorter = PeakLogSize(1_000);

        Assert.InRange(shorter, 1, 1024);
        Assert.Equal(shorter, PeakLogSize(100_000));
    }

    [Fact]
    public void AParseLeavesTheNextParseOnItsThreadNoLargeLog()
    {
        var pinned = Atomic(SetMode(_modes, Mode.Text).Then(Char('a')).Then(SetMode(_modes, Mode.Code)).SkipMany().Then(End));
        var room = -1;

        Assert.IsType<Parsed<Unit>>(pinned.Parse(new string('a', 10_000)).Value);
        Assert.IsType<Parsed<Unit>>(SetMode(_modes, Mode.Text).Then(new LogSize((_, capacity) => room = capacity)).Parse("").Value);
        Assert.InRange(room, 1, 1024);
    }

    [Fact]
    public void AParseOnItsOwnUndoesItsChangesAfterAParseWhoseLogDroppedEntries()
    {
        var dropping = SetMode(_modes, Mode.Text).Then(Char('a')).Then(SetMode(_modes, Mode.Code)).SkipMany().Then(End);

        Assert.IsType<Parsed<Unit>>(dropping.Parse(new string('a', 10_000)).Value);
        Assert.Equal('a', Assert.IsType<Parsed<char>>(SetMode(_modes, Mode.Text).Then(Char('z')).Or(Char('a')).Parse("a").Value).Value);
    }

    [Fact]
    public void AParserThatWritesAChannelParsesOnItsOwnOverTokens() =>
        Assert.IsType<Parsed<Unit>>(Script.Block.Parse((Tok[])[new(K.LBrace), new(K.Number), new(K.Number), new(K.RBrace)]).Value);

    [Fact]
    public void APushedModeIsPoppedOnce()
    {
        Assert.IsType<Parsed<Unit>>(_pushOrPop.Parse("ppqq").Value);
        Assert.Equal(PoppedLast, Assert.Throws<GrammarException>(() => _pushOrPop.Parse("pqq")).Message);
    }

    [Fact]
    public void ChangingTheModeOverAStackOfOneModeAllocatesNothing() =>
        Assert.Equal(Allocated(Changes(1)), Allocated(Changes(20)));

    [Fact]
    public void EachChannelHasItsOwnStack()
    {
        var other = new ModeChannel<Mode>(Mode.Code);
        var depths = new ModeChannel<int>(0);

        Assert.Equal(PoppedLast, Assert.Throws<GrammarException>(() => PushMode(_modes, Mode.Text).Then(PopMode(other)).Parse("")).Message);
        Assert.Equal(
            "The parser layer pops the last mode of a ModeChannel<Int32>. Each PopMode needs a PushMode on the same channel before it.",
            Assert.Throws<GrammarException>(() => PushMode(_modes, Mode.Text).Then(PopMode(depths)).Parse("")).Message
        );
    }

    [Fact]
    public void EachParseStartsWithItsChannelsAtTheirInitialModes()
    {
        Assert.IsType<Parsed<Unit>>(_pushOrPop.Parse("ppp").Value);
        Assert.Equal(PoppedLast, Assert.Throws<GrammarException>(() => _pushOrPop.Parse("q")).Message);
    }

    [Fact]
    public void PoppingTheLastModeThrows() =>
        Assert.Equal(PoppedLast, Assert.Throws<GrammarException>(() => PopMode(_modes).Parse("")).Message);

    [Fact]
    public void PoppingTheLastModeOfTheDocsSampleNamesItsChannel() =>
        Assert.Equal(
            "The parser layer pops the last mode of a ModeChannel<LexMode>. Each PopMode needs a PushMode on the same channel before it.",
            Assert.Throws<GrammarException>(() => PopMode(Script.Modes).Parse("")).Message
        );

    [Fact]
    public void SetModeReplacesTheTopAndKeepsTheDepth()
    {
        Assert.Equal(PoppedLast, Assert.Throws<GrammarException>(() => SetMode(_modes, Mode.Text).Then(PopMode(_modes)).Parse("")).Message);
        Assert.IsType<Parsed<Unit>>(PushMode(_modes, Mode.Text).Then(SetMode(_modes, Mode.Block)).Then(PopMode(_modes)).Parse("").Value);
    }
}
