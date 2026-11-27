namespace Creole.Tests;

/// <summary>
/// An upstream of <paramref name="count"/> items, one a step, that goes back for the channel it reads, as a <c>Layer.Modal</c> does.
/// </summary>
/// <remarks>
/// The item at position <c>i</c> is <c>10 * i</c> plus the number of times it has gone back, so an item produced again shows.
/// </remarks>
file sealed class Steered(int count) : IUpstream<int>
{
    private readonly int _count = count;
    private int _next;
    private int _times;

    public SourceSpan InputEnd => new(SourceId.Unnamed, _count, _count);

    public SourceSpan InputStart => new(SourceId.Unnamed, 0, 0);

    public ModeLink Reads { get; } = new ModeLink<int>(new ModeChannel<int>(0));

    public bool TryGoBack(StreamSlot slot, ModeLink channel)
    {
        if (!ReferenceEquals(channel, Reads))
        {
            return false;
        }

        _next = (int)slot.StepStart;
        _times++;

        return true;
    }

    public bool TryStep(StreamBuffer<int> buffer)
    {
        if (_next == _count)
        {
            return false;
        }

        buffer.Add(_next * 10 + _times, new StreamSlot(new SourceSpan(SourceId.Unnamed, _next, _next + 1), _next, null, 0));
        _next++;

        return true;
    }
}

/// <summary>
/// <see cref="StreamBuffer{T}.Discard"/>, which drops the items from a position on when the layer above changes the mode its upstream reads, so the upstream produces them again.
/// </summary>
public class StreamBufferDiscardTests
{
    private static List<int> Read(StreamBuffer<int> buffer, int from, int to)
    {
        var items = new List<int>();

        for (var position = from; position < to; position++)
        {
            Assert.True(buffer.TryGetSegment(position, horizon: 0, out _, out _));
            items.Add(buffer.ItemAt(position));
        }

        return items;
    }

    [Fact]
    public void ABufferThatEndedReadsOnAfterADiscard()
    {
        var upstream = new Steered(8);
        var buffer = new StreamBuffer<int>(upstream, segmentSize: 4);

        Read(buffer, 0, 8);
        Assert.False(buffer.TryGetSegment(8, horizon: 0, out _, out _));

        Assert.True(buffer.Discard(6, upstream.Reads));
        Assert.Equal([0, 10, 20, 30, 40, 50, 61, 71], Read(buffer, 0, 8));
    }

    [Fact]
    public void ADiscardForAChannelTheUpstreamDoesNotReadKeepsEveryItem()
    {
        var upstream = new Steered(8);
        var buffer = new StreamBuffer<int>(upstream, segmentSize: 4);

        Read(buffer, 0, 6);

        Assert.False(buffer.Discard(3, new ModeLink<int>(new ModeChannel<int>(0))));
        Assert.Equal(6, buffer.Filled);
        Assert.Equal([0, 10, 20, 30, 40, 50, 60, 70], Read(buffer, 0, 8));
    }

    [Fact]
    public void ADiscardWhereNothingIsBufferedDoesNothing()
    {
        var upstream = new Steered(8);
        var buffer = new StreamBuffer<int>(upstream, segmentSize: 4);

        Read(buffer, 0, 6);

        Assert.False(buffer.Discard(6, upstream.Reads));
        Assert.Equal([0, 10, 20, 30, 40, 50, 60, 70], Read(buffer, 0, 8));
    }

    [Fact]
    public void AfterADiscardTheEndOfTheItemsIsTheEndOfTheLastItemKept()
    {
        var upstream = new Steered(8);
        var buffer = new StreamBuffer<int>(upstream, segmentSize: 4);

        Read(buffer, 0, 6);
        buffer.Discard(3, upstream.Reads);

        Assert.Equal(new SourceSpan(SourceId.Unnamed, 3, 3), buffer.SpanOf(3, 3));
    }

    [Fact]
    public void DiscardingFromAPositionProducesTheItemsAgainFromThereAndKeepsThoseBefore()
    {
        var upstream = new Steered(8);
        var buffer = new StreamBuffer<int>(upstream, segmentSize: 4);

        Read(buffer, 0, 6);

        Assert.True(buffer.Discard(3, upstream.Reads));
        Assert.Equal(3, buffer.Filled);
        Assert.Equal([0, 10, 20, 31, 41, 51, 61, 71], Read(buffer, 0, 8));
    }

    [Fact]
    public void DiscardingFromTheStartOfASegmentGivesTheSegmentBack()
    {
        var upstream = new Steered(8);
        var pool = new OverwritingPool<int>(-1);
        var buffer = new StreamBuffer<int>(upstream, segmentSize: 4, pool);

        Read(buffer, 0, 6);

        Assert.True(buffer.Discard(4, upstream.Reads));
        Assert.Equal(1, pool.Returns);
        Assert.Equal([0, 10, 20, 30, 41, 51, 61, 71], Read(buffer, 0, 8));
    }
}
