namespace Creole;

/// <summary>
/// One run of a <see cref="WhereLayer{T}"/>: each step reads one item and produces it, with its span, if it passes.
/// </summary>
/// <remarks>
/// It reads its input without a cursor, so it releases the input itself: on reaching a new segment, it releases what lies below the input item of the oldest output still held.
/// </remarks>
file sealed class WhereUpstream<T>(StreamBuffer<T> input, Func<T, bool> predicate) : IUpstream<T>
{
    private readonly StreamBuffer<T> _input = input;
    private long _position;
    private readonly Func<T, bool> _predicate = predicate;
    private long _segmentStart;

    public bool TryStep(StreamBuffer<T> buffer)
    {
        if (!_input.TryGetSegment(_position, out var segment, out var segmentStart))
        {
            return false;
        }

        if (segmentStart != _segmentStart)
        {
            _segmentStart = segmentStart;
            _input.Release(buffer.OldestStepStart ?? _position, failures: null);
        }

        var item = segment.Span[(int)(_position - segmentStart)];

        if (_predicate(item))
        {
            buffer.Add(item, new StreamSlot(_input.SpanOf(_position, _position + 1), _position, StateBefore: null, GroupIndex: 0));
        }

        _position++;

        return true;
    }
}

/// <summary>
/// Keeps the items for which <see cref="Predicate"/> is true. Backs <c>Layer.Where</c>.
/// </summary>
internal sealed class WhereLayer<T>(Func<T, bool> predicate) : Layer<T, T>
{
    public Func<T, bool> Predicate { get; } = predicate;

    internal override IUpstream<T> Open(StreamBuffer<T> input, ParseOptions options, bool recordFailures) =>
        new WhereUpstream<T>(input, Predicate);
}
