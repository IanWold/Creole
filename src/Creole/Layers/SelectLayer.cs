namespace Creole;

/// <summary>
/// One run of a <see cref="SelectLayer{TIn, TOut}"/>: each step reads one item and produces its image, with its span.
/// </summary>
/// <remarks>
/// It reads its input without a cursor, so it states its own pin as its horizon with each read, and releases the input itself.
/// The pin is the input item of the oldest output its reader can still go back to, or the item it is reading if there is none.
/// On reaching a new segment, it releases what lies below the pin.
/// </remarks>
file sealed class SelectUpstream<TIn, TOut>(ICursorSource<TIn> input, Func<TIn, TOut> selector) : IUpstream<TOut>
{
    private readonly ICursorSource<TIn> _input = input;
    private long _position;
    private long _segmentStart;
    private readonly Func<TIn, TOut> _selector = selector;

    public SourceSpan InputEnd => _input.InputEnd;

    public SourceSpan InputStart => _input.InputStart;

    public void Dispose() => _input.Dispose();

    public void ForgetFailures() => _input.ForgetFailures();

    public bool TryStep(StreamBuffer<TOut> buffer)
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

        buffer.Add(_selector(item), new StreamSlot(_input.SpanOf(_position, _position + 1), _position, StateBefore: null, GroupIndex: 0));
        _position++;

        return true;
    }
}

/// <summary>
/// Applies <see cref="Selector"/> to each item. Backs <c>Layer.Select</c>, and <c>Layer.Identity</c> when it runs on its own.
/// </summary>
internal sealed class SelectLayer<TIn, TOut>(Func<TIn, TOut> selector) : Layer<TIn, TOut>
{
    public Func<TIn, TOut> Selector { get; } = selector;

    internal override IUpstream<TOut> Open(ICursorSource<TIn> input, ParseOptions options, bool recordFailures) =>
        new SelectUpstream<TIn, TOut>(input, Selector);
}
