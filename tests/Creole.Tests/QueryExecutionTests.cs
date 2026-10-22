using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed class BlobGrammar : Grammar<byte>
{
    public static readonly Parser<byte, int> UInt16 = Seq(Any, Any).Select(static (hi, lo) => hi << 8 | lo);

    public static readonly Parser<byte, byte[]> Blob =
        from length in UInt16
        from bytes in Any.Repeat(length)
        select bytes.ToArray();
}

public class QueryExecutionTests
{
    private static readonly Outcome _failConsumed = new Outcome.FailConsumed();
    private static readonly Outcome _failEmpty = new Outcome.FailEmpty();

    private static readonly Parser<char, char> _letterAfterDigit = Digit.Bind(d => d == '1' ? Char('a') : Char('b'));

    private static Outcome.Ok Ok(string value, int consumed) => new(value, consumed);

    [Fact]
    public void ABindClauseRunsTheParserItChooses()
    {
        var pair =
            from d in Digit
            from c in d == '1' ? Char('a') : Char('b')
            select $"{d}{c}";

        Assert.Equal(Ok("1a", 2), Execution.Outcome(pair, "1a"));
        Assert.Equal(Ok("2b", 2), Execution.Outcome(pair, "2b"));
        Assert.Equal(_failConsumed, Execution.Outcome(pair, "1b"));
    }

    [Fact]
    public void ARejectionAfterConsumingFailsARepetition() =>
        Assert.Equal(_failConsumed, Execution.Outcome(Digit.Where(c => c != '0').Many(), "120", items => string.Concat(items)));

    [Fact]
    public void ARejectionAfterConsumingStopsAChoice()
    {
        var nonZeroOrZero = Digit.Where(c => c != '0').Or(Char('0'));

        Assert.Equal(Ok("5", 1), Execution.Outcome(nonZeroOrZero, "5"));
        Assert.Equal(_failConsumed, Execution.Outcome(nonZeroOrZero, "0"));
    }

    [Fact]
    public void ARejectionLeavesTheCursorWhereItsParserStopped()
    {
        var cursor = new Cursor<char>("1234;");

        var (ok, _) = Execution.Run(Digit.AtLeastOnce().Text().Where(digits => digits.Length < 3), ref cursor);

        Assert.False(ok);
        Assert.Equal(4, cursor.Position);
    }

    [Fact]
    public void AtomicLetsAChoiceTryTheNextAlternativeAfterARejection()
    {
        var nonZeroOrZero = Atomic(Digit.Where(c => c != '0')).Or(Char('0'));

        Assert.Equal(Ok("0", 1), Execution.Outcome(nonZeroOrZero, "0"));
    }

    [Fact]
    public void AWhereClauseFiltersAQuery()
    {
        var nonZero =
            from d in Digit
            where d != '0'
            select d;

        Assert.Equal(Ok("5", 1), Execution.Outcome(nonZero, "5"));
        Assert.Equal(_failConsumed, Execution.Outcome(nonZero, "0"));
    }

    [Fact]
    public void BindFailsAsItsSourceDoes()
    {
        Assert.Equal(_failEmpty, Execution.Outcome(_letterAfterDigit, "x"));
        Assert.Equal(_failConsumed, Execution.Outcome(Seq(Char('a'), Digit).Select((_, d) => d).Bind(_ => Char('b')), "ax"));
    }

    [Fact]
    public void BindFailsCommittedWhenTheChosenParserFailsAfterTheSourceConsumed() =>
        Assert.Equal(_failConsumed, Execution.Outcome(_letterAfterDigit, "1b"));

    [Fact]
    public void BindFailsWithoutConsumingWhenNeitherParserConsumes() =>
        Assert.Equal(_failEmpty, Execution.Outcome(Return(1).Bind(_ => Char('a')), "b"));

    [Fact]
    public void BindLowersAParserItsContinuationReturnsAgainOnce()
    {
        var letter = new CountingNode<char>(Char('a'));
        var parser = Digit.Bind(_ => letter);

        Assert.Equal(Ok("a", 2), Execution.Outcome(parser, "1a"));
        Assert.Equal(Ok("a", 2), Execution.Outcome(parser, "2a"));
        Assert.Equal(1, letter.LowerCount);
    }

    [Fact]
    public void BindRunsTheParserItsContinuationChooses()
    {
        Assert.Equal(Ok("a", 2), Execution.Outcome(_letterAfterDigit, "1a"));
        Assert.Equal(Ok("b", 2), Execution.Outcome(_letterAfterDigit, "2b"));
    }

    [Fact]
    public void BindThrowsWhenItsContinuationReturnsNull()
    {
        static void Run(Parser<char, char> parser)
        {
            var cursor = new Cursor<char>("1");
            Execution.Run(parser, ref cursor);
        }

        var exception = Assert.Throws<GrammarException>(() => Run(Digit.Bind<char>(_ => null!)));

        Assert.Equal("'_ => null!' returned null instead of a parser.", exception.Message);
    }

    [Fact]
    public void BlobReadsALengthThenThatManyBytes()
    {
        static string Render(byte[] bytes) => string.Join(",", bytes);

        Assert.Equal(Ok("10,20,30", 5), Execution.Outcome(BlobGrammar.Blob, new byte[] { 0, 3, 10, 20, 30, 40 }, Render));
        Assert.Equal(Ok("", 2), Execution.Outcome(BlobGrammar.Blob, new byte[] { 0, 0, 10 }, Render));
        Assert.Equal(_failConsumed, Execution.Outcome(BlobGrammar.Blob, new byte[] { 0, 3, 10 }, Render));
    }

    [Fact]
    public void WhereFailsAsItsSourceDoes()
    {
        Assert.Equal(_failEmpty, Execution.Outcome(Char('a').Where(_ => true), "b"));
        Assert.Equal(_failConsumed, Execution.Outcome(Seq(Char('a'), Char('b')).Where(_ => true), "ac"));
    }

    [Fact]
    public void WhereFailsCommittedWhenItRejectsAfterConsuming() =>
        Assert.Equal(_failConsumed, Execution.Outcome(Digit.Where(c => c != '0'), "0"));

    [Fact]
    public void WhereFailsWithoutConsumingWhenItRejectsAValueThatConsumedNothing() =>
        Assert.Equal(_failEmpty, Execution.Outcome(Return(0).Where(n => n > 0), "x"));

    [Fact]
    public void WhereSucceedsWhenThePredicateAcceptsTheResult() =>
        Assert.Equal(Ok("5", 1), Execution.Outcome(Digit.Where(c => c != '0'), "5"));

    [Fact]
    public void WhereWithAMessageRunsTheSameWay()
    {
        var nonZero = Digit.Where(c => c != '0', "zero is not allowed");

        Assert.Equal(Ok("5", 1), Execution.Outcome(nonZero, "5"));
        Assert.Equal(_failConsumed, Execution.Outcome(nonZero, "0"));
    }
}
