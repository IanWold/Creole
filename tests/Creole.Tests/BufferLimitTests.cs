using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

/// <summary>
/// Produces <paramref name="length"/> copies of <paramref name="item"/>, up to <paramref name="itemsPerStep"/> each step, and records the most items its buffer held after a step.
/// </summary>
/// <remarks>
/// Item <c>i</c> has the span <c>[i, i + 1)</c>, as a character of a root layer does.
/// </remarks>
file sealed class RepeatingUpstream(char item, long length, int itemsPerStep = 1) : IUpstream<char>
{
    private readonly char _item = item;
    private readonly int _itemsPerStep = itemsPerStep;
    private readonly long _length = length;

    public SourceSpan InputEnd => new(SourceId.Unnamed, _length, _length);

    public SourceSpan InputStart => new(SourceId.Unnamed, 0, 0);

    /// <summary>
    /// The most items the buffer held after any step, released or not.
    /// </summary>
    public long MostHeld { get; private set; }

    /// <summary>
    /// How many items it has produced.
    /// </summary>
    public long Produced { get; private set; }

    public bool TryStep(StreamBuffer<char> buffer)
    {
        if (Produced == _length)
        {
            return false;
        }

        var stepStart = Produced;

        for (var group = 0; group < _itemsPerStep && Produced < _length; group++)
        {
            buffer.Add(_item, new StreamSlot(new SourceSpan(SourceId.Unnamed, Produced, Produced + 1), stepStart, StateBefore: null, group));
            Produced++;
        }

        MostHeld = Math.Max(MostHeld, buffer.Filled - buffer.Start);

        return true;
    }
}

/// <summary>
/// <see cref="ParseOptions.MaxBufferedItems"/>: a reader that would hold more of its buffer than the limit ends the parse with one error at the oldest item it still had to keep.
/// </summary>
public class BufferLimitTests
{
    private const int Limit = 1000;

    private static readonly Parser<char, Unit> _twoThenReadAhead = Char('a').Then(Char('a')).Then(Lookahead(Char('a').SkipMany()));

    private static ParseError Expected(long horizon, int limit = Limit) =>
        new(
            new SourceSpan(SourceId.Unnamed, horizon, horizon + 1),
            "parser",
            new CustomMessage($"buffered input from here exceeds ParseOptions.MaxBufferedItems of {limit}"),
            [],
            []
        );

    private static (bool Ok, ParseError? Error) Run<T>(Parser<char, T> parser, IUpstream<char> upstream, int limit, int segmentSize = 16)
    {
        var buffer = new StreamBuffer<char>(upstream, segmentSize, maxBufferedItems: limit);
        var log = new FailureLog<char>();
        var cursor = new Cursor<char>(buffer) { Failures = log };
        var value = default(T)!;

        try
        {
            return (PlanCache.Get(parser).TryParse(ref cursor, ref value), null);
        }
        catch (BufferLimitExceededException exception)
        {
            return (false, ParseErrors.BufferLimit(exception, log, "parser", buffer.SpanOf));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(16)]
    [InlineData(1024)]
    public void ALookaheadOverHugeInputEndsAtTheLimitWithTheErrorAtItsFirstItem(int segmentSize)
    {
        var upstream = new RepeatingUpstream('a', 10_000_000);

        var (ok, error) = Run(_twoThenReadAhead, upstream, Limit, segmentSize);

        Assert.False(ok);
        Assert.Equal(Expected(2), error);
        Assert.Equal(2 + Limit + 1, upstream.Produced);
        Assert.InRange(upstream.MostHeld, 0, Limit + (2 * segmentSize) + 1);
    }

    [Fact]
    public void AnInputWithinTheLimitParses()
    {
        var (ok, error) = Run(_twoThenReadAhead.Then(Char('a').SkipMany()).Then(End), new RepeatingUpstream('a', 2 + Limit), Limit);

        Assert.True(ok);
        Assert.Null(error);
    }

    [Fact]
    public void NoCombinatorCatchesTheLimit()
    {
        var readAhead = Lookahead(Char('a').SkipMany());
        var neverEnds = Char('a').SkipMany().Then(Char('b')).Select(static _ => default(Unit));

        Parser<char, Unit>[] wrapped =
        [
            Not(readAhead),
            readAhead.Optional().Select(static _ => default(Unit)),
            readAhead.Or(Char('a').SkipMany()),
            Atomic(neverEnds).Or(Char('a').SkipMany()),
        ];

        foreach (var parser in wrapped)
        {
            var (ok, error) = Run(Char('a').Then(Char('a')).Then(parser), new RepeatingUpstream('a', 100_000), Limit);

            Assert.False(ok);
            Assert.Equal(Expected(2), error);
        }
    }

    [Fact]
    public void TheErrorKeepsTheContextInForce()
    {
        var (_, error) = Run(Char('a').Then(Char('a')).Then(Lookahead(Char('a').SkipMany()).Context("in lookahead")), new RepeatingUpstream('a', 100_000), Limit);

        Assert.Equal([new ContextLine("in lookahead", new SourceSpan(SourceId.Unnamed, 2, 3))], error!.Context);
    }

    [Fact]
    public void ALimitOfOneLetsAParseWithNoPinsRunToTheEnd()
    {
        var upstream = new RepeatingUpstream('a', 10_000);

        var (ok, error) = Run(Char('a').SkipMany().Then(End), upstream, limit: 1);

        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(10_000, upstream.Produced);
    }

    [Fact]
    public void TheCountIncludesTheItemsTheLastStepProduced()
    {
        var (ok, error) = Run(Char('a').SkipMany().Then(End), new RepeatingUpstream('a', 10_000, itemsPerStep: 2), limit: 1);

        Assert.False(ok);
        Assert.Equal(Expected(0, limit: 1), error);
    }

    [Fact]
    public void AParseOverInputInMemoryIgnoresTheLimit() =>
        Assert.IsType<Parsed<Unit>>(
            Lookahead(Char('a').SkipMany()).Then(Char('a').SkipMany()).Parse(new string('a', 10_000), options: new ParseOptions { MaxBufferedItems = 1 }).Value
        );
}
