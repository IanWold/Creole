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
    private readonly ICursorSource<T>? _source;
    private ReadOnlySpan<T> _span;
    private long _segmentStart;
    private int _index;

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

    /// <summary>
    /// The number of items before the cursor, counted from the start of the input.
    /// </summary>
    public readonly long Position => _segmentStart + _index;

    /// <summary>
    /// Whether the cursor is at the end of the input.
    /// </summary>
    public bool IsAtEnd => !TryPeek(out _);

    /// <summary>
    /// The items from the cursor to the end of the current segment. Empty only at the end of the input.
    /// </summary>
    /// <remarks>
    /// Use this for bulk scans, such as <see cref="MemoryExtensions.IndexOfAnyExcept{T}(ReadOnlySpan{T}, System.Buffers.SearchValues{T})"/>,
    /// then move with <see cref="Advance"/>. More input may follow the segment; read again after advancing to the end of it.
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

        SetSegment(segment.Span, start, position);
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
            SetSegment(segment.Span, start, position);
        }
    }

    private void SetSegment(ReadOnlySpan<T> segment, long start, long position)
    {
        _span = segment;
        _segmentStart = start;
        _index = (int)(position - start);
    }
}
