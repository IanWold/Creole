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
    private const int UncheckedDepth = 20;

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
    /// The levels of recursion this layer's parse is inside.
    /// </summary>
    /// <remarks>
    /// A level is counted while a <c>Rec</c>, the parser a <c>Bind</c> continuation returned, a prefix operator's operand or an <c>InfixR</c> right operand runs.
    /// Each layer has its own cursor, so each layer counts its own depth.
    /// </remarks>
    internal int Depth { get; private set; }

    /// <summary>
    /// Where this layer's parse records its failures, or null to record none.
    /// </summary>
    internal FailureLog<T>? Failures { get; init; }

    /// <summary>
    /// The most levels this layer's parse may be inside at once. <see cref="int.MaxValue"/> leaves only the stack check.
    /// </summary>
    internal int MaxDepth { get; init; } = int.MaxValue;

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

    private void EnterLevelSlow()
    {
        if (Depth >= MaxDepth)
        {
            throw new DepthExceededException(Position, Depth, DepthCause.MaxDepth);
        }

        if (!RuntimeHelpers.TryEnsureSufficientExecutionStack())
        {
            throw new DepthExceededException(Position, Depth, DepthCause.Stack);
        }

        Depth++;
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
    /// Stops holding the start that the matching <see cref="HoldStart"/> held.
    /// </summary>
    internal readonly void DropStart() => Failures?.DropStart();

    /// <summary>
    /// Enters a context with <paramref name="text"/> at the cursor, for the failures recorded until <see cref="LeaveContext"/>.
    /// </summary>
    internal readonly void EnterContext(string text) => Failures?.Enter(text, Position);

    /// <summary>
    /// Enters one level of recursion at the cursor's position. Call <see cref="LeaveLevel"/> when the level returns, whether it succeeded or failed.
    /// </summary>
    /// <remarks>
    /// Deeper than 20 levels, it also checks that the thread has enough stack left to go on.
    /// </remarks>
    /// <exception cref="DepthExceededException">The parse is already <see cref="MaxDepth"/> levels deep, or the thread is short of stack.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void EnterLevel()
    {
        if (Depth < UncheckedDepth && Depth < MaxDepth)
        {
            Depth++;
            return;
        }

        EnterLevelSlow();
    }

    /// <summary>
    /// Records a failure at the cursor that expected <paramref name="expected"/>, or nothing in particular when it is null.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly void Fail(Expected? expected) => Failures?.Record(Position, expected);

    /// <summary>
    /// Records at the cursor, as one failure, the failures of several parsers that expected <paramref name="expected"/> in turn.
    /// </summary>
    internal readonly void FailAll(Expected[] expected) => Failures?.Record(Position, expected);

    /// <summary>
    /// Records a failure at <paramref name="position"/>, which the cursor has passed, that expected <paramref name="expected"/>, or nothing in particular when it is null.
    /// </summary>
    internal readonly void FailAt(long position, Expected? expected) => Failures?.Record(position, expected);

    /// <summary>
    /// Records a failure at the cursor that reports <paramref name="message"/>.
    /// </summary>
    internal readonly void FailWith(string message) => Failures?.Record(Position, message);

    /// <summary>
    /// Creates a cursor at the start of an input held entirely in memory, whose items <see cref="Since"/> can return without copying.
    /// </summary>
    internal static Cursor<T> FromMemory(ReadOnlyMemory<T> input) => new(input.Span) { _segment = input };

    /// <summary>
    /// Holds the cursor's position in the failure log until <see cref="DropStart"/>, as the start of a filter, a chained operator or a custom scanner that may record a failure there after the cursor has moved on.
    /// </summary>
    internal readonly void HoldStart() => Failures?.HoldStart(Position);

    /// <summary>
    /// The item at <paramref name="position"/>, which must be a position this cursor has already passed and the input still holds. The cursor does not move.
    /// </summary>
    internal readonly T ItemAt(long position)
    {
        TryPeekAt(position, out var item);

        return item;
    }

    /// <summary>
    /// Ends a <see cref="SaveFailures"/> and keeps the furthest failure as it is now.
    /// </summary>
    internal readonly void KeepFailures(in FailureMark mark) => Failures?.Keep(in mark);

    /// <summary>
    /// Leaves the context the matching <see cref="EnterContext"/> entered.
    /// </summary>
    internal readonly void LeaveContext() => Failures?.Leave();

    /// <summary>
    /// Leaves the level the matching <see cref="EnterLevel"/> entered.
    /// </summary>
    internal void LeaveLevel() => Depth--;

    /// <summary>
    /// The furthest failure as it is now, for <see cref="Relabel"/>.
    /// </summary>
    internal readonly FailureMark NoteFailures() => Failures?.Note() ?? default;

    /// <summary>
    /// Records a failure behind the cursor at <paramref name="start"/>, naming the input from there to the cursor: a rejected filter with <paramref name="message"/>, or a chained operator with <paramref name="chain"/>.
    /// </summary>
    /// <param name="start">Where the named input starts, which the innermost <see cref="HoldStart"/> holds.</param>
    /// <param name="message">The filter's message, or null.</param>
    /// <param name="chain">The chained operator's message, or null.</param>
    internal readonly void Reject(long start, string? message, ChainMessage? chain) =>
        Failures?.Reject(start, Position, message, chain);

    /// <summary>
    /// Replaces the expectations recorded at <paramref name="start"/> since <paramref name="note"/> with <paramref name="label"/>, as <c>Named</c> does.
    /// </summary>
    internal readonly void Relabel(in FailureMark note, long start, Expected label) => Failures?.Relabel(in note, start, label);

    /// <summary>
    /// Moves the cursor to <paramref name="position"/>, which must be a position this cursor has already reached and the input still holds.
    /// </summary>
    /// <remarks>
    /// The end of the input holds no item, so no segment holds it; the cursor then stands at the end of the segment that holds the last item.
    /// </remarks>
    internal void Reset(long position)
    {
        var offset = position - _segmentStart;

        if ((ulong)offset <= (ulong)_span.Length)
        {
            _index = (int)offset;
            return;
        }

        if (_source is not null && position >= 0 && _source.TryGetSegment(position, out var segment, out var start))
        {
            SetSegment(segment, start, position);
            return;
        }

        if (_source is not null && position > 0 && _source.TryGetSegment(position - 1, out segment, out start) && start + segment.Length == position)
        {
            SetSegment(segment, start, position);
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(position), position, "The input does not hold this position.");
    }

    /// <summary>
    /// Ends a <see cref="SaveFailures"/> and puts the furthest failure back as it was then.
    /// </summary>
    internal readonly void RestoreFailures(in FailureMark mark) => Failures?.Restore(in mark);

    /// <summary>
    /// Saves the furthest failure until <see cref="RestoreFailures"/> or <see cref="KeepFailures"/>, as <c>Lookahead</c>, <c>Not</c> and <c>Longest</c> do.
    /// </summary>
    internal readonly FailureMark SaveFailures() => Failures?.Save() ?? default;

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
    /// Reads the item at <paramref name="position"/>, which must be a position this cursor has already reached and the input still holds. The cursor does not move.
    /// </summary>
    /// <returns><see langword="false"/> if <paramref name="position"/> is the end of the input.</returns>
    internal readonly bool TryPeekAt(long position, out T item)
    {
        var at = this;
        at.Reset(position);

        return at.TryPeek(out item);
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
