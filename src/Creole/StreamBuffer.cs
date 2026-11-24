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
/// A span whose character offsets differ from its root offsets, as an item decoded from bytes has, also stores its character offsets as two 32-bit numbers, from its segment's character base.
/// A segment rents the array for them only when it stores its first such span, so a segment over text keeps none.
/// A span that does not fit is kept whole in a list beside its segment: one whose offset or length is out of range in either unit, or one from another source than the segment's first span.
/// So every span reads back exactly as it was added, and nothing is allocated per item, over a text root or a byte root.
/// </para>
/// <para>
/// A <see cref="Cursor{T}"/> reads the buffer as its <see cref="ICursorSource{T}"/>.
/// Asking for a position past the filled items runs upstream steps until that position is filled or the input ends, and no further, so no item is produced before the layer above asks for it.
/// Reading a filled position again, after a rewind, never runs the upstream.
/// Each such read states the reader's horizon, and the upstream keeps only the input it needs to produce again the items at or above it (<see cref="OldestStepStart"/>).
/// </para>
/// <para>
/// <see cref="Release"/> returns whole segments below a position to the pool, which may hand their arrays to anything else.
/// Nothing may read a released position afterwards, so the layer above first copies out what its pending errors need.
/// <see cref="Dispose"/> returns every segment when the parse ends, and disposes the upstream, which disposes the buffers below it.
/// </para>
/// <para>
/// The buffer also keeps a stack of the positions its reader's span combinators hold (<see cref="HoldSpan"/>).
/// It copies the span at each one it releases, so <see cref="SpanOf"/> can still start there, whether or not the reader keeps a failure log.
/// </para>
/// </remarks>
/// <param name="upstream">The layer that produces the items.</param>
/// <param name="segmentSize">How many items each segment holds.</param>
/// <param name="pool">Where segments are rented from and returned to; <see cref="ArrayPool{T}.Shared"/> if null.</param>
internal sealed class StreamBuffer<T>(IUpstream<T> upstream, int segmentSize = 1024, ArrayPool<T>? pool = null) : ICursorSource<T>
{
    /// <summary>
    /// A position a span combinator holds, and its span once the buffer has released it.
    /// </summary>
    /// <param name="Position">The held position.</param>
    /// <param name="Copy">The span of the item there, copied when its segment was released, or null while the buffer still holds it.</param>
    private readonly record struct HeldSpan(long Position, SourceSpan? Copy);

    /// <summary>
    /// One segment: its items and slots, and what its spans are stored relative to.
    /// </summary>
    /// <param name="Items">The items, rented from the buffer's pool.</param>
    /// <param name="Slots">The slot beside each item.</param>
    /// <param name="SpanBase">The start of the segment's first span, from which its spans' offsets count.</param>
    /// <param name="Source">The source of the segment's first span, which every span stored in its slots shares.</param>
    /// <param name="Wide">The spans kept whole because they do not fit in a <see cref="PackedSpan"/>, or null if there are none.</param>
    /// <param name="Chars">The character offsets of each slot's span, from <paramref name="CharBase"/>, or null if every span stored so far has character offsets equal to its root offsets.</param>
    /// <param name="CharBase">The character start of the first span whose character offsets differ from its root offsets, from which the offsets in <paramref name="Chars"/> count.</param>
    private record struct Segment(T[] Items, PackedSlot[] Slots, long SpanBase, SourceId Source, List<SourceSpan>? Wide, PackedSpan[]? Chars, long CharBase);

    /// <summary>
    /// A <see cref="StreamSlot"/> as the buffer stores it, with its span packed.
    /// </summary>
    internal readonly record struct PackedSlot(PackedSpan Span, long StepStart, object? StateBefore, int GroupIndex);

    /// <summary>
    /// A span as the buffer stores it: <paramref name="Offset"/> from its segment's base, and <paramref name="Length"/>.
    /// </summary>
    /// <remarks>
    /// A <paramref name="Length"/> of <see cref="WideLength"/> marks a span kept whole, and <paramref name="Offset"/> is then its index in the segment's list of wide spans.
    /// The character offsets beside a span are packed the same way, from the segment's character base, and a <paramref name="Length"/> of <see cref="SameAsRoot"/> there marks character offsets equal to the root offsets.
    /// </remarks>
    internal readonly record struct PackedSpan(int Offset, int Length);

    private const int SameAsRoot = -2;
    private const int WideLength = -1;

    private bool _ended;
    private int _heldCount;
    private HeldSpan[] _heldSpans = [];
    private long _horizon;
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

    /// <inheritdoc/>
    /// <remarks>
    /// It is the upstream's <see cref="IUpstream{T}.InputEnd"/>.
    /// </remarks>
    public SourceSpan InputEnd => _upstream.InputEnd;

    /// <inheritdoc/>
    /// <remarks>
    /// It is the upstream's <see cref="IUpstream{T}.InputStart"/>.
    /// </remarks>
    public SourceSpan InputStart => _upstream.InputStart;

    /// <summary>
    /// Where the step that produced the oldest item the reader can still go back to began in the upstream's input, or null when the upstream has not produced that item yet.
    /// </summary>
    /// <remarks>
    /// <para>
    /// That item is the one at the highest horizon the reader has stated, with <see cref="TryGetSegment"/> or <see cref="Release"/>.
    /// A reader's horizon never falls, so the highest it has stated is still at or below it.
    /// </para>
    /// <para>
    /// The upstream keeps its input from here on, so that any item the reader can still go back to can be produced again.
    /// An item below the horizon is never produced again: it keeps its slot while the buffer holds it, but its step's input may be released.
    /// </para>
    /// </remarks>
    public long? OldestStepStart
    {
        get
        {
            if (_horizon >= Filled)
            {
                return null;
            }

            var (index, offset) = Locate(_horizon);

            return _segments[index].Slots[offset].StepStart;
        }
    }

    /// <summary>
    /// The first position the buffer still holds. Every position below it has been released.
    /// </summary>
    public long Start => _released * _segmentSize;

    private void CopyHeldSpans(long start, long end)
    {
        for (var i = 0; i < _heldCount; i++)
        {
            if (_heldSpans[i] is { Copy: null } held
                && held.Position >= start
                && held.Position < end
            )
            {
                _heldSpans[i] = held with { Copy = SpanAt(held.Position) };
            }
        }
    }

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

    private SourceSpan FirstSpan(long position)
    {
        if (position < Start)
        {
            for (var i = _heldCount - 1; i >= 0; i--)
            {
                if (_heldSpans[i] is { Copy: { } copy } held && held.Position == position)
                {
                    return copy;
                }
            }
        }

        return SpanAt(position);
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

    private PackedSpan Pack(ref Segment segment, int index, SourceSpan span)
    {
        var offset = span.Start - segment.SpanBase;

        if (span.Source == segment.Source
            && offset is >= int.MinValue and <= int.MaxValue
            && span.Length <= int.MaxValue
            && TryPackChars(ref segment, index, span)
        )
        {
            return new PackedSpan((int)offset, (int)span.Length);
        }

        segment.Wide ??= [];
        segment.Wide.Add(span);

        return new PackedSpan(segment.Wide.Count - 1, WideLength);
    }

    private void ReturnSegments(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _pool.Return(_segments[i].Items, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            ArrayPool<PackedSlot>.Shared.Return(_segments[i].Slots, clearArray: true);

            if (_segments[i].Chars is { } chars)
            {
                ArrayPool<PackedSpan>.Shared.Return(chars);
            }
        }

        _segments.RemoveRange(0, count);
    }

    private SourceSpan SpanAt(long position)
    {
        var (index, offset) = Locate(position);

        return Unpack(_segments[index], offset);
    }

    /// <summary>
    /// Stores the character offsets of <paramref name="span"/> beside slot <paramref name="index"/>, renting the segment's array for them on its first span whose character offsets differ from its root offsets.
    /// </summary>
    /// <returns><see langword="false"/> if they do not fit, so the span must be kept whole.</returns>
    private bool TryPackChars(ref Segment segment, int index, SourceSpan span)
    {
        var sameAsRoot = span.CharStart == span.Start && span.CharEnd == span.End;

        if (segment.Chars is null)
        {
            if (sameAsRoot)
            {
                return true;
            }

            segment.Chars = ArrayPool<PackedSpan>.Shared.Rent(_segmentSize);
            segment.Chars.AsSpan(0, index).Fill(new PackedSpan(0, SameAsRoot));
            segment.CharBase = span.CharStart;
        }

        if (sameAsRoot)
        {
            segment.Chars[index] = new PackedSpan(0, SameAsRoot);
            return true;
        }

        var offset = span.CharStart - segment.CharBase;
        var length = span.CharEnd - span.CharStart;

        if (offset is < int.MinValue or > int.MaxValue || length > int.MaxValue)
        {
            return false;
        }

        segment.Chars[index] = new PackedSpan((int)offset, (int)length);
        return true;
    }

    private static SourceSpan Unpack(in Segment segment, int index)
    {
        var span = segment.Slots[index].Span;

        if (span.Length == WideLength)
        {
            return segment.Wide![span.Offset];
        }

        var start = segment.SpanBase + span.Offset;
        var end = start + span.Length;

        return segment.Chars is { } chars && chars[index] is { Length: not SameAsRoot } decoded
            ? new SourceSpan(segment.Source, start, end, segment.CharBase + decoded.Offset, segment.CharBase + decoded.Offset + decoded.Length)
            : new SourceSpan(segment.Source, start, end);
    }

    /// <summary>
    /// Adds <paramref name="item"/> at the end of the filled items, with <paramref name="slot"/> beside it. Called by the upstream during a step.
    /// </summary>
    public void Add(T item, StreamSlot slot)
    {
        var offset = (int)(Filled % _segmentSize);

        if (offset == 0)
        {
            _segments.Add(new Segment(_pool.Rent(_segmentSize), ArrayPool<PackedSlot>.Shared.Rent(_segmentSize), slot.Span.Start, slot.Span.Source, Wide: null, Chars: null, CharBase: 0));
        }

        ref var segment = ref CollectionsMarshal.AsSpan(_segments)[^1];

        segment.Items[offset] = item;
        segment.Slots[offset] = new PackedSlot(Pack(ref segment, offset, slot.Span), slot.StepStart, slot.StateBefore, slot.GroupIndex);
        _lastSpan = slot.Span;
        Filled++;
    }

    /// <summary>
    /// Returns every segment the buffer holds to the pool, and disposes the upstream, so the buffers below return theirs.
    /// </summary>
    /// <remarks>
    /// Nothing may read the buffer afterwards. A parse calls this when it ends, however it ends.
    /// </remarks>
    public void Dispose()
    {
        ReturnSegments(_segments.Count);
        _upstream.Dispose();
    }

    /// <inheritdoc/>
    public void DropSpan() => _heldCount--;

    /// <inheritdoc/>
    public void ForgetFailures() => _upstream.ForgetFailures();

    /// <inheritdoc/>
    public void HoldSpan(long position)
    {
        if (_heldCount == _heldSpans.Length)
        {
            Array.Resize(ref _heldSpans, Math.Max(4, _heldCount * 2));
        }

        _heldSpans[_heldCount++] = new HeldSpan(position, null);
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
    /// Returns to the pool every segment whose items all lie below the item just before <paramref name="horizon"/>, after <paramref name="failures"/> has copied what its errors need from them and the buffer has copied the span at each held position in them.
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
    /// <param name="horizon">The position below which nothing will be read again, at or before the cursor of the layer that reads this buffer. It also states the reader's horizon, as <see cref="TryGetSegment"/> does.</param>
    /// <param name="failures">The failure log of the layer that reads this buffer, or null if it keeps none.</param>
    public void Release(long horizon, FailureLog<T>? failures)
    {
        _horizon = Math.Max(_horizon, horizon);

        var count = (int)((Math.Min(horizon, Filled) - 1) / _segmentSize - _released);

        if (count <= 0)
        {
            return;
        }

        var end = Start + (long)count * _segmentSize;

        failures?.Preserve(this, Start, end);
        CopyHeldSpans(Start, end);

        ReturnSegments(count);
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

        return new StreamSlot(Unpack(in segment, offset), slot.StepStart, slot.StateBefore, slot.GroupIndex);
    }

    /// <summary>
    /// The source span of the items from <paramref name="start"/> up to <paramref name="end"/>: from the start of the first to the end of the last.
    /// </summary>
    /// <remarks>
    /// An empty range gives a zero-width span at the start of the item at <paramref name="start"/>.
    /// At the filled end, which is the end of the input once a cursor has found it there, it gives a zero-width span at the end of the last item.
    /// The item at <paramref name="start"/> may have been released while <see cref="HoldSpan"/> held it; its span is then the copy taken at release.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The buffer does not hold an item the span needs, and no hold copied it.</exception>
    public SourceSpan SpanOf(long start, long end)
    {
        if (end > start)
        {
            var first = FirstSpan(start);
            var last = SpanAt(end - 1);

            return new SourceSpan(first.Source, first.Start, last.End, first.CharStart, last.CharEnd);
        }

        if (start < Filled)
        {
            var at = FirstSpan(start);

            return new SourceSpan(at.Source, at.Start, at.Start, at.CharStart, at.CharStart);
        }

        return new SourceSpan(_lastSpan.Source, _lastSpan.End, _lastSpan.End, _lastSpan.CharEnd, _lastSpan.CharEnd);
    }

    /// <summary>
    /// Whether the segment holding <paramref name="position"/> keeps character offsets beside its spans, which it does once it stores a span whose character offsets differ from its root offsets.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The buffer does not hold an item at <paramref name="position"/>: it was released or has not been produced.</exception>
    public bool StoresCharOffsets(long position) =>
        _segments[Locate(position).Segment].Chars is not null;

    /// <inheritdoc/>
    /// <remarks>
    /// The buffer keeps the highest <paramref name="horizon"/> it has been given before it pulls, so the upstream's steps see it (<see cref="OldestStepStart"/>).
    /// </remarks>
    public bool TryGetSegment(long position, long horizon, out ReadOnlyMemory<T> segment, out long segmentStart)
    {
        _horizon = Math.Max(_horizon, horizon);

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
