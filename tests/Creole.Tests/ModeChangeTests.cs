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
file sealed class Script : TokenGrammar<Tok, K>
{
    public static readonly ModeChannel<LexMode> Modes = new(LexMode.Operand);

    public static readonly Parser<Tok, Tok> Operand =
        Kind(K.Number).Between(SetMode(Modes, LexMode.Operand), SetMode(Modes, LexMode.Operator));

    public static readonly Parser<Tok, Unit> Block =
        Kind(K.LBrace).Then(PushMode(Modes, LexMode.Block)).Then(Operand.SkipMany()).Before(Kind(K.RBrace)).Before(PopMode(Modes));
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
