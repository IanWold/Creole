using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// A position in a parser's input, and the only way engines and custom scanners read that input.
/// </summary>
/// <typeparam name="T">The input item type, such as <see cref="char"/> or a token type.</typeparam>
/// <remarks>
/// <para>
/// The input is held as one or more segments. Reads inside the current segment index a span directly and do not allocate.
/// Moving past the end of a segment loads the next one; an input held entirely in memory is a single segment.
/// </para>
/// <para>
/// Positions are absolute: <see cref="Position"/> counts items from the start of the input, whatever segment holds them.
/// A step consumed input if and only if it moved the cursor.
/// </para>
/// <para>
/// A cursor is passed by reference (<c>ref Cursor&lt;T&gt;</c>). Copying it gives an independent position over the same input.
/// </para>
/// </remarks>
public ref struct Cursor<T>
{
    private int _index;
    private ReadOnlyMemory<T> _segment;
    private long _segmentStart;
    private readonly ICursorSource<T>? _source;
    private ReadOnlySpan<T> _span;

    /// <summary>
    /// Whether a cut is in force in this layer's parse.
    /// </summary>
    /// <remarks>
    /// Each layer has its own cursor, so this is the one cut flag per layer that the cut rule needs.
    /// <c>Atomic</c>, <c>Lookahead</c>, <c>Not</c>, <c>Longest</c> alternatives and every <see cref="LightMark"/> save and restore it.
    /// </remarks>
    internal bool Cut { get; set; }

    /// <summary>
    /// The items from the cursor to the end of the current segment. Empty only at the end of the input.
    /// </summary>
    /// <remarks>
    /// Use this for bulk scans, such as <see cref="MemoryExtensions.IndexOfAnyExcept{T}(ReadOnlySpan{T}, System.Buffers.SearchValues{T})"/>, then move with <see cref="Advance"/>.
    /// More input may follow the segment; read again after advancing to the end of it.
    /// </remarks>
    public ReadOnlySpan<T> Available
    {
        get
        {
            if ((uint)_index >= (uint)_span.Length)
            {
                LoadSegment();
            }

            return _span[_index..];
        }
    }

    /// <summary>
    /// Whether the cursor is at the end of the input.
    /// </summary>
    public bool IsAtEnd => !TryPeek(out _);

    /// <summary>
    /// The number of items before the cursor, counted from the start of the input.
    /// </summary>
    public readonly long Position => _segmentStart + _index;

    /// <summary>
    /// Creates a cursor at the start of an input held entirely in memory.
    /// </summary>
    internal Cursor(ReadOnlySpan<T> input)
    {
        _span = input;
    }

    /// <summary>
    /// Creates a cursor at the start of an input whose segments come from <paramref name="source"/>.
    /// </summary>
    internal Cursor(ICursorSource<T> source)
    {
        _source = source;
        _span = [];
    }

    private void AdvanceSlow(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        while (count > 0)
        {
            if (_index == _span.Length)
            {
                LoadSegment();

                if (_index == _span.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), count, "Fewer items remain than the count.");
                }
            }

            var step = Math.Min(count, _span.Length - _index);
            _index += step;
            count -= step;
        }
    }

    private void LoadSegment()
    {
        var position = Position;

        if (_source is not null && _source.TryGetSegment(position, out var segment, out var start))
        {
            SetSegment(segment, start, position);
        }
    }

    private void SetSegment(ReadOnlyMemory<T> segment, long start, long position)
    {
        _segment = segment;
        _span = segment.Span;
        _segmentStart = start;
        _index = (int)(position - start);
    }

    private bool TryPeekSlow(out T item)
    {
        LoadSegment();

        if ((uint)_index < (uint)_span.Length)
        {
            item = _span[_index];
            return true;
        }

        item = default!;
        return false;
    }

    /// <summary>
    /// Creates a cursor at the start of an input held entirely in memory, whose items <see cref="Since"/> can return without copying.
    /// </summary>
    internal static Cursor<T> FromMemory(ReadOnlyMemory<T> input) => new(input.Span) { _segment = input };

    /// <summary>
    /// Moves the cursor to <paramref name="position"/>, which must be a position this cursor has already reached and the input still holds.
    /// </summary>
    internal void Reset(long position)
    {
        var offset = position - _segmentStart;

        if ((ulong)offset <= (ulong)_span.Length)
        {
            _index = (int)offset;
            return;
        }

        if (_source is null || position < 0 || !_source.TryGetSegment(position, out var segment, out var start))
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "The input does not hold this position.");
        }

        SetSegment(segment, start, position);
    }

    /// <summary>
    /// The items from <paramref name="start"/> up to the cursor, which must be a position this cursor has already reached and the input still holds.
    /// </summary>
    /// <remarks>
    /// The result is a view of the input only for a cursor created by <see cref="FromMemory"/>, whose memory belongs to the caller.
    /// Otherwise it is a copy: a span has no memory to view, and a source's segments may be pooled and refilled once released (ADR-0013).
    /// </remarks>
    internal readonly ReadOnlyMemory<T> Since(long start)
    {
        var length = checked((int)(Position - start));
        var offset = start - _segmentStart;

        if (offset >= 0)
        {
            // Only FromMemory sets _segment without a source. A cursor created from a span leaves it empty.
            return _source is null && _segment.Length == _span.Length
                ? _segment.Slice((int)offset, length)
                : _span.Slice((int)offset, length).ToArray();
        }

        var items = new T[length];

        for (var copied = 0; copied < length;)
        {
            if (_source is null || !_source.TryGetSegment(start + copied, out var segment, out var segmentStart))
            {
                throw new ArgumentOutOfRangeException(nameof(start), start, "The input does not hold this position.");
            }

            var from = (int)(start + copied - segmentStart);
            var count = Math.Min(length - copied, segment.Length - from);

            segment.Span.Slice(from, count).CopyTo(items.AsSpan(copied));
            copied += count;
        }

        return items;
    }

    /// <summary>
    /// Moves past <paramref name="count"/> items, which may span several segments.
    /// </summary>
    /// <param name="count">The number of items to move past. Zero does nothing.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="count"/> is negative, or fewer than <paramref name="count"/> items remain. In the second case the cursor is left at the end of the input.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count)
    {
        if ((uint)count <= (uint)(_span.Length - _index))
        {
            _index += count;
            return;
        }

        AdvanceSlow(count);
    }

    /// <summary>
    /// Reads the item at the cursor without moving.
    /// </summary>
    /// <param name="item">The item at the cursor, or <see langword="default"/> at the end of the input.</param>
    /// <returns><see langword="false"/> at the end of the input.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryPeek(out T item)
    {
        if ((uint)_index < (uint)_span.Length)
        {
            item = _span[_index];
            return true;
        }

        return TryPeekSlow(out item);
    }

    /// <summary>
    /// Reads the item at the cursor and moves past it.
    /// </summary>
    /// <param name="item">The item read, or <see langword="default"/> at the end of the input.</param>
    /// <returns><see langword="false"/> at the end of the input, in which case the cursor does not move.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead(out T item)
    {
        if (!TryPeek(out item))
        {
            return false;
        }

        _index++;
        return true;
    }
}
