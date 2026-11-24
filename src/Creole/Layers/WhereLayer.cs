namespace Creole;

/// <summary>
/// One run of a <see cref="WhereLayer{T}"/>: each step reads one item and produces it, with its span, if it passes.
/// </summary>
/// <remarks>
/// It reads its input without a cursor, so it states its own pin as its horizon with each read, and releases the input itself.
/// The pin is the input item of the oldest output its reader can still go back to, or the item it is reading if there is none.
/// On reaching a new segment, it releases what lies below the pin.
/// </remarks>
file sealed class WhereUpstream<T>(ICursorSource<T> input, Func<T, bool> predicate) : IUpstream<T>
{
    private readonly ICursorSource<T> _input = input;
    private long _position;
    private readonly Func<T, bool> _predicate = predicate;
    private long _segmentStart;

    public SourceSpan InputEnd => _input.InputEnd;

    public SourceSpan InputStart => _input.InputStart;

    public void Dispose() => _input.Dispose();

    public void ForgetFailures() => _input.ForgetFailures();

    public bool TryStep(StreamBuffer<T> buffer)
    {
        var pin = buffer.OldestStepStart ?? _position;

        if (!_input.TryGetSegment(_position, pin, out var segment, out var segmentStart))
        {
            return false;
        }

        if (segmentStart != _segmentStart)
        {
            _segmentStart = segmentStart;
            _input.Release(pin, failures: null);
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

    internal override IUpstream<T> Open(ICursorSource<T> input, ParseOptions options, bool recordFailures) =>
        new WhereUpstream<T>(input, Predicate);
}
