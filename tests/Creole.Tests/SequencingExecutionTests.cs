using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

public class SequencingExecutionTests
{
    private static readonly Outcome _failConsumed = new Outcome.FailConsumed();
    private static readonly Outcome _failEmpty = new Outcome.FailEmpty();

    private static Outcome.Ok Ok(string value, int consumed) => new(value, consumed);

    [Fact]
    public void AsAndIgnoreReplaceTheResult()
    {
        Assert.Equal(Ok("7", 1), Execution.Outcome(Char('a').As(7), "a"));
        Assert.Equal(Ok("()", 1), Execution.Outcome(Char('a').Ignore(), "a", _ => "()"));
    }

    [Fact]
    public void BeforeThenAndBetweenKeepOneResult()
    {
        Assert.Equal(Ok("a", 2), Execution.Outcome(Char('a').Before(Char(';')), "a;"));
        Assert.Equal(Ok(";", 2), Execution.Outcome(Char('a').Then(Char(';')), "a;"));
        Assert.Equal(Ok("a", 3), Execution.Outcome(Char('a').Between(Char('('), Char(')')), "(a)"));
    }

    [Fact]
    public void ContextAndNamedDoNotChangeTheOutcome()
    {
        var ab = Seq(Char('a'), Char('b')).Select((a, b) => $"{a}{b}");

        Assert.Equal(Ok("ab", 2), Execution.Outcome(ab.Named("ab").Context("in ab"), "ab"));
        Assert.Equal(_failEmpty, Execution.Outcome(ab.Named("ab").Context("in ab"), "x"));
        Assert.Equal(_failConsumed, Execution.Outcome(ab.Named("ab").Context("in ab"), "ac"));
    }

    [Fact]
    public void OptionalFailsWhenItsItemFailsAfterConsuming() =>
        Assert.Equal(_failConsumed, Execution.Outcome(Seq(Char('a'), Char('b')).Optional(), "ac"));

    [Fact]
    public void OptionalKeepsACutFromAnItemThatSucceeded()
    {
        var cursor = new Cursor<char>("a");

        var (ok, _) = Execution.Run(new CutThenNode<char>(Char('a')).Optional(), ref cursor);

        Assert.True(ok);
        Assert.True(cursor.Cut);
    }

    [Fact]
    public void OptionalProducesNothingWithoutConsumingWhenItsItemFailsWithoutConsuming() =>
        Assert.Equal(Ok("Nothing", 0), Execution.Outcome(Char('a').Optional(), "b"));

    [Fact]
    public void OptionalProducesTheItemsValue() =>
        Assert.Equal(Ok("Just(a)", 1), Execution.Outcome(Char('a').Optional(), "ab"));

    [Fact]
    public void OptionalRestoresTheCutWhenItsItemFailsWithoutConsuming()
    {
        var cursor = new Cursor<char>("b");

        var (ok, _) = Execution.Run(new CutThenNode<char>(Char('a')).Optional(), ref cursor);

        Assert.True(ok);
        Assert.False(cursor.Cut);
    }

    [Fact]
    public void SelectAppliesItsFunction() =>
        Assert.Equal(Ok("A", 1), Execution.Outcome(Char('a').Select(char.ToUpperInvariant), "a"));

    [Fact]
    public void SequenceFailsAfterConsumingWhenALaterParserFails() =>
        Assert.Equal(_failConsumed, Execution.Outcome(Seq(Char('a'), Char('b'), Char('c')), "abx"));

    [Fact]
    public void SequenceFailsWithoutConsumingWhenItsFirstParserDoes() =>
        Assert.Equal(_failEmpty, Execution.Outcome(Seq(Char('a'), Char('b')), "b"));

    [Fact]
    public void SequenceOfEightCombinesEveryResult()
    {
        var parser = Seq(Char('a'), Char('b'), Char('c'), Char('d'), Char('e'), Char('f'), Char('g'), Char('h'))
            .Select(static (a, b, c, d, e, f, g, h) => string.Concat(a, b, c, d, e, f, g, h));

        Assert.Equal(Ok("abcdefgh", 8), Execution.Outcome(parser, "abcdefghi"));
    }

    [Fact]
    public void SequenceOfEveryArityRunsInOrder()
    {
        var a = Char('a');

        Assert.Equal(Ok("(a, a)", 2), Execution.Outcome(Seq(a, a), "aaaaaaaa"));
        Assert.Equal(Ok("(a, a, a)", 3), Execution.Outcome(Seq(a, a, a), "aaaaaaaa"));
        Assert.Equal(Ok("(a, a, a, a)", 4), Execution.Outcome(Seq(a, a, a, a), "aaaaaaaa"));
        Assert.Equal(Ok("(a, a, a, a, a)", 5), Execution.Outcome(Seq(a, a, a, a, a), "aaaaaaaa"));
        Assert.Equal(Ok("(a, a, a, a, a, a)", 6), Execution.Outcome(Seq(a, a, a, a, a, a), "aaaaaaaa"));
        Assert.Equal(Ok("(a, a, a, a, a, a, a)", 7), Execution.Outcome(Seq(a, a, a, a, a, a, a), "aaaaaaaa"));
        Assert.Equal(Ok("(a, a, a, a, a, a, a, a)", 8), Execution.Outcome(Seq(a, a, a, a, a, a, a, a), "aaaaaaaa"));
    }

    [Fact]
    public void SliceAcrossSegmentsHoldsTheConsumedCharacters()
    {
        var cursor = new Cursor<char>(new SegmentedSource<char>("xabcdx".ToCharArray(), 2));
        cursor.Advance(1);

        var (ok, slice) = Execution.Run(String("abcd").Slice(), ref cursor);

        Assert.True(ok);
        Assert.Equal("abcd", slice.ToString());
    }

    [Fact]
    public void SliceOverASourceKeepsItsTextWhenTheSourceMemoryIsReused()
    {
        var segments = "xabcdx".ToCharArray();
        var cursor = new Cursor<char>(new SegmentedSource<char>(segments, 6));
        cursor.Advance(1);

        var (ok, slice) = Execution.Run(String("abcd").Slice(), ref cursor);
        segments.AsSpan().Fill('z');

        Assert.True(ok);
        Assert.Equal("abcd", slice.ToString());
    }

    [Fact]
    public void SliceFromASpanCopiesTheConsumedCharacters() =>
        Assert.Equal(Ok("abc", 4), Execution.Outcome(Seq(Char('x'), String("abc").Slice()).Select((_, s) => s), "xabcx", s => s.ToString()));

    [Fact]
    public void SliceInsideOneSegmentIsAViewOfTheInput()
    {
        const string input = "xabcx";
        var cursor = Cursor<char>.FromMemory(input.AsMemory());
        cursor.Advance(1);

        var (ok, slice) = Execution.Run(String("abc").Slice(), ref cursor);

        Assert.True(ok);
        Assert.True(System.Runtime.InteropServices.MemoryMarshal.TryGetString(slice, out var text, out var start, out var length));
        Assert.Same(input, text);
        Assert.Equal((1, 3), (start, length));
    }

    [Fact]
    public void TextProducesTheConsumedCharactersAsAString() =>
        Assert.Equal(Ok("12", 2), Execution.Outcome(Seq(Digit, Digit).Text(), "12;"));

    [Fact]
    public void TextFailsAsItsParserDoes() =>
        Assert.Equal(_failConsumed, Execution.Outcome(Seq(Char('a'), Char('b')).Text(), "ac"));
}
