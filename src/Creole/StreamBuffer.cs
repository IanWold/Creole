using System.Buffers;
using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// A layer boundary: the items an upstream layer has produced, held so that the layer above can read and rewind over them without the upstream running again.
/// </summary>
/// <typeparam name="T">The item type, such as <see cref="char"/> or a token type.</typeparam>
/// <remarks>
/// <para>
/// Items are indexed by absolute position and held in segments of a fixed size, rented from a pool.
/// Beside each item the buffer keeps a <see cref="StreamSlot"/>: the item's span, and what is needed to produce it again.
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
    private bool _ended;
    private readonly List<T[]> _items = [];
    private SourceSpan _lastSpan;
    private readonly ArrayPool<T> _pool = pool ?? ArrayPool<T>.Shared;
    private long _released;
    private readonly int _segmentSize = segmentSize > 0
        ? segmentSize
        : throw new ArgumentOutOfRangeException(nameof(segmentSize), segmentSize, "A segment must hold at least one item.");
    private readonly List<StreamSlot[]> _slots = [];
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

    /// <summary>
    /// Adds <paramref name="item"/> at the end of the filled items, with <paramref name="slot"/> beside it. Called by the upstream during a step.
    /// </summary>
    public void Add(T item, StreamSlot slot)
    {
        var offset = (int)(Filled % _segmentSize);

        if (offset == 0)
        {
            _items.Add(_pool.Rent(_segmentSize));
            _slots.Add(ArrayPool<StreamSlot>.Shared.Rent(_segmentSize));
        }

        _items[^1][offset] = item;
        _slots[^1][offset] = slot;
        _lastSpan = slot.Span;
        Filled++;
    }

    /// <summary>
    /// The item at <paramref name="position"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The buffer does not hold an item at <paramref name="position"/>: it was released or has not been produced.</exception>
    public T ItemAt(long position)
    {
        var (segment, offset) = Locate(position);

        return _items[segment][offset];
    }

    /// <summary>
    /// Returns to the pool every segment whose items all lie below <paramref name="horizon"/>, after <paramref name="failures"/> has copied what its errors need from them.
    /// </summary>
    /// <remarks>
    /// A segment that still holds an item at or past <paramref name="horizon"/>, or that is not yet full, is kept.
    /// </remarks>
    /// <param name="horizon">The position below which nothing will be read again.</param>
    /// <param name="failures">The failure log of the layer that reads this buffer, or null if it keeps none.</param>
    public void Release(long horizon, FailureLog<T>? failures = null)
    {
        var count = (int)(Math.Min(horizon, Filled) / _segmentSize - _released);

        if (count <= 0)
        {
            return;
        }

        failures?.Preserve(this, Start, Start + (long)count * _segmentSize);

        for (var i = 0; i < count; i++)
        {
            _pool.Return(_items[i], RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            ArrayPool<StreamSlot>.Shared.Return(_slots[i], clearArray: true);
        }

        _items.RemoveRange(0, count);
        _slots.RemoveRange(0, count);
        _released += count;
    }

    /// <summary>
    /// The slot beside the item at <paramref name="position"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The buffer does not hold an item at <paramref name="position"/>: it was released or has not been produced.</exception>
    public StreamSlot SlotAt(long position)
    {
        var (segment, offset) = Locate(position);

        return _slots[segment][offset];
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
            var first = SlotAt(start).Span;
            var last = SlotAt(end - 1).Span;

            return new SourceSpan(first.Source, first.Start, last.End, first.CharStart, last.CharEnd);
        }

        if (start < Filled)
        {
            var at = SlotAt(start).Span;

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
        segment = _items[IndexOf(position)].AsMemory(0, (int)Math.Min(_segmentSize, Filled - segmentStart));
        return true;
    }
}
