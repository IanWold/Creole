using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Creole;

/// <summary>
/// A layer boundary: the items an upstream layer has produced, held so that the layer above can read and rewind over them without the upstream running again.
/// </summary>
/// <typeparam name="T">The item type, such as <see cref="char"/> or a token type.</typeparam>
/// <remarks>
/// <para>
/// Items are indexed by absolute position and held in segments of a fixed size, rented from a pool.
/// Beside each item the buffer keeps what a <see cref="StreamSlot"/> holds: the item's span, and what is needed to produce it again.
/// </para>
/// <para>
/// A span is stored as two 32-bit numbers: its start as an offset from its segment's base, which is the start of the segment's first span, and its length.
/// A span that does not fit is kept whole in a list beside its segment: one whose offset or length is out of range, one from another source than the segment's first span, or one whose character offsets differ from its root offsets.
/// So every span reads back exactly as it was added, and over a text root nothing is allocated per item.
/// </para>
/// <para>
/// A <see cref="Cursor{T}"/> reads the buffer as its <see cref="ICursorSource{T}"/>.
/// Asking for a position past the filled items runs upstream steps until that position is filled or the input ends, and no further, so no item is produced before the layer above asks for it.
/// Reading a filled position again, after a rewind, never runs the upstream.
/// </para>
/// <para>
/// <see cref="Release"/> returns whole segments below a position to the pool, which may hand their arrays to anything else.
/// Nothing may read a released position afterwards, so the layer above first copies out what its pending errors need.
/// </para>
/// </remarks>
/// <param name="upstream">The layer that produces the items.</param>
/// <param name="segmentSize">How many items each segment holds.</param>
/// <param name="pool">Where segments are rented from and returned to; <see cref="ArrayPool{T}.Shared"/> if null.</param>
internal sealed class StreamBuffer<T>(IUpstream<T> upstream, int segmentSize = 1024, ArrayPool<T>? pool = null) : ICursorSource<T>
{
    /// <summary>
    /// One segment: its items and slots, and what its spans are stored relative to.
    /// </summary>
    /// <param name="Items">The items, rented from the buffer's pool.</param>
    /// <param name="Slots">The slot beside each item.</param>
    /// <param name="SpanBase">The start of the segment's first span, from which its spans' offsets count.</param>
    /// <param name="Source">The source of the segment's first span, which every span stored in its slots shares.</param>
    /// <param name="Wide">The spans kept whole because they do not fit in a <see cref="PackedSpan"/>, or null if there are none.</param>
    private record struct Segment(T[] Items, PackedSlot[] Slots, long SpanBase, SourceId Source, List<SourceSpan>? Wide);

    /// <summary>
    /// A <see cref="StreamSlot"/> as the buffer stores it, with its span packed.
    /// </summary>
    internal readonly record struct PackedSlot(PackedSpan Span, long StepStart, object? StateBefore, int GroupIndex);

    /// <summary>
    /// A span as the buffer stores it: <paramref name="Offset"/> from its segment's base, and <paramref name="Length"/>.
    /// </summary>
    /// <remarks>
    /// A <paramref name="Length"/> of <see cref="WideLength"/> marks a span kept whole, and <paramref name="Offset"/> is then its index in the segment's list of wide spans.
    /// </remarks>
    internal readonly record struct PackedSpan(int Offset, int Length);

    private const int WideLength = -1;

    private bool _ended;
    private SourceSpan _lastSpan;
    private readonly ArrayPool<T> _pool = pool ?? ArrayPool<T>.Shared;
    private long _released;
    private readonly List<Segment> _segments = [];
    private readonly int _segmentSize = segmentSize > 0
        ? segmentSize
        : throw new ArgumentOutOfRangeException(nameof(segmentSize), segmentSize, "A segment must hold at least one item.");
    private readonly IUpstream<T> _upstream = upstream;

    /// <summary>
    /// The number of items the upstream has produced so far, which is the position just past the filled items.
    /// </summary>
    public long Filled { get; private set; }

    /// <summary>
    /// The first position the buffer still holds. Every position below it has been released.
    /// </summary>
    public long Start => _released * _segmentSize;

    private bool Fill(long position)
    {
        while (position >= Filled)
        {
            if (_ended || !_upstream.TryStep(this))
            {
                _ended = true;
                return false;
            }
        }

        return true;
    }

    private int IndexOf(long position) => (int)(position / _segmentSize - _released);

    private (int Segment, int Offset) Locate(long position)
    {
        if (position < Start || position >= Filled)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "The buffer does not hold this position.");
        }

        return (IndexOf(position), (int)(position % _segmentSize));
    }

    private static PackedSpan Pack(ref Segment segment, SourceSpan span)
    {
        var offset = span.Start - segment.SpanBase;

        if (span.Source == segment.Source
            && span.CharStart == span.Start
            && span.CharEnd == span.End
            && offset is >= int.MinValue and <= int.MaxValue
            && span.Length <= int.MaxValue
        )
        {
            return new PackedSpan((int)offset, (int)span.Length);
        }

        segment.Wide ??= [];
        segment.Wide.Add(span);

        return new PackedSpan(segment.Wide.Count - 1, WideLength);
    }

    private SourceSpan SpanAt(long position)
    {
        var (index, offset) = Locate(position);
        var segment = _segments[index];

        return Unpack(in segment, segment.Slots[offset].Span);
    }

    private static SourceSpan Unpack(in Segment segment, PackedSpan span) =>
        span.Length == WideLength
        ? segment.Wide![span.Offset]
        : new SourceSpan(segment.Source, segment.SpanBase + span.Offset, segment.SpanBase + span.Offset + span.Length);

    /// <summary>
    /// Adds <paramref name="item"/> at the end of the filled items, with <paramref name="slot"/> beside it. Called by the upstream during a step.
    /// </summary>
    public void Add(T item, StreamSlot slot)
    {
        var offset = (int)(Filled % _segmentSize);

        if (offset == 0)
        {
            _segments.Add(new Segment(_pool.Rent(_segmentSize), ArrayPool<PackedSlot>.Shared.Rent(_segmentSize), slot.Span.Start, slot.Span.Source, Wide: null));
        }

        ref var segment = ref CollectionsMarshal.AsSpan(_segments)[^1];

        segment.Items[offset] = item;
        segment.Slots[offset] = new PackedSlot(Pack(ref segment, slot.Span), slot.StepStart, slot.StateBefore, slot.GroupIndex);
        _lastSpan = slot.Span;
        Filled++;
    }

    /// <summary>
    /// The item at <paramref name="position"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The buffer does not hold an item at <paramref name="position"/>: it was released or has not been produced.</exception>
    public T ItemAt(long position)
    {
        var (index, offset) = Locate(position);

        return _segments[index].Items[offset];
    }

    /// <summary>
    /// Returns to the pool every segment whose items all lie below the item just before <paramref name="horizon"/>, after <paramref name="failures"/> has copied what its errors need from them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The segment holding the item at <paramref name="horizon"/> − 1 is kept, as is any segment not yet full.
    /// A cursor at the horizon that has just read the last item of a segment still holds that segment, so returning it would let the cursor read whatever the pool put there.
    /// </para>
    /// <para>
    /// It also keeps the item before the cursor, where a value rejected at the cursor ends.
    /// </para>
    /// </remarks>
    /// <param name="horizon">The position below which nothing will be read again, at or before the cursor of the layer that reads this buffer.</param>
    /// <param name="failures">The failure log of the layer that reads this buffer, or null if it keeps none.</param>
    public void Release(long horizon, FailureLog<T>? failures)
    {
        var count = (int)((Math.Min(horizon, Filled) - 1) / _segmentSize - _released);

        if (count <= 0)
        {
            return;
        }

        failures?.Preserve(this, Start, Start + (long)count * _segmentSize);

        for (var i = 0; i < count; i++)
        {
            _pool.Return(_segments[i].Items, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            ArrayPool<PackedSlot>.Shared.Return(_segments[i].Slots, clearArray: true);
        }

        _segments.RemoveRange(0, count);
        _released += count;
    }

    /// <summary>
    /// The slot beside the item at <paramref name="position"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The buffer does not hold an item at <paramref name="position"/>: it was released or has not been produced.</exception>
    public StreamSlot SlotAt(long position)
    {
        var (index, offset) = Locate(position);
        var segment = _segments[index];
        var slot = segment.Slots[offset];

        return new StreamSlot(Unpack(in segment, slot.Span), slot.StepStart, slot.StateBefore, slot.GroupIndex);
    }

    /// <summary>
    /// The source span of the items from <paramref name="start"/> up to <paramref name="end"/>: from the start of the first to the end of the last.
    /// </summary>
    /// <remarks>
    /// An empty range gives a zero-width span at the start of the item at <paramref name="start"/>.
    /// At the filled end, which is the end of the input once a cursor has found it there, it gives a zero-width span at the end of the last item.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The buffer does not hold an item the span needs.</exception>
    public SourceSpan SpanOf(long start, long end)
    {
        if (end > start)
        {
            var first = SpanAt(start);
            var last = SpanAt(end - 1);

            return new SourceSpan(first.Source, first.Start, last.End, first.CharStart, last.CharEnd);
        }

        if (start < Filled)
        {
            var at = SpanAt(start);

            return new SourceSpan(at.Source, at.Start, at.Start, at.CharStart, at.CharStart);
        }

        return new SourceSpan(_lastSpan.Source, _lastSpan.End, _lastSpan.End, _lastSpan.CharEnd, _lastSpan.CharEnd);
    }

    /// <inheritdoc/>
    public bool TryGetSegment(long position, out ReadOnlyMemory<T> segment, out long segmentStart)
    {
        if (position < Start || !Fill(position))
        {
            segment = default;
            segmentStart = 0;
            return false;
        }

        segmentStart = position - position % _segmentSize;
        segment = _segments[IndexOf(position)].Items.AsMemory(0, (int)Math.Min(_segmentSize, Filled - segmentStart));

        return true;
    }
}
