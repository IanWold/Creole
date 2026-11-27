namespace Creole;

/// <summary>
/// One run of a <see cref="SelectLayer{TIn, TOut}"/>: each step reads one item and produces its image, with its span.
/// </summary>
/// <remarks>
/// It reads its input without a cursor, so it states its own pin as its horizon with each read, and releases the input itself.
/// The pin is the input item of the oldest output its reader can still go back to, or the item it is reading if there is none.
/// On reaching a new segment, it releases what lies below the pin.
/// A read that would hold more of its input than the buffer's limit ends the parse with an error named <see cref="SelectLayer{TIn, TOut}.Name"/>.
/// </remarks>
file class SelectUpstream<TIn, TOut>(ICursorSource<TIn> input, Func<TIn, TOut> selector) : IUpstream<TOut>
{
    private long _bookmarkPosition;
    private long _bookmarkSegmentStart;
    private readonly ICursorSource<TIn> _input = input;
    private long _position;
    private long _segmentStart;
    private readonly Func<TIn, TOut> _selector = selector;

    public SourceSpan InputEnd => _input.InputEnd;

    public SourceSpan InputStart => _input.InputStart;

    public bool Starved => _input.Starved;

    private bool TryGetSegment(long pin, out ReadOnlyMemory<TIn> segment, out long segmentStart)
    {
        try
        {
            return _input.TryGetSegment(_position, pin, out segment, out segmentStart);
        }
        catch (BufferLimitExceededException exception)
        {
            throw new LayerFailedException(ParseErrors.BufferLimit<TIn>(exception, failures: null, SelectLayer<TIn, TOut>.Name, _input.SpanOf));
        }
    }

    public void Bookmark()
    {
        _bookmarkPosition = _position;
        _bookmarkSegmentStart = _segmentStart;
        _input.Bookmark();
    }

    public void Dispose() => _input.Dispose();

    public void ForgetFailures() => _input.ForgetFailures();

    public bool Restart()
    {
        _position = _bookmarkPosition;
        _segmentStart = _bookmarkSegmentStart;
        _input.Restart();

        return true;
    }

    public bool TryStep(StreamBuffer<TOut> buffer)
    {
        var pin = buffer.OldestStepStart ?? _position;

        if (!TryGetSegment(pin, out var segment, out var segmentStart))
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
internal class SelectLayer<TIn, TOut>(Func<TIn, TOut> selector) : Layer<TIn, TOut>
{
    /// <summary>
    /// The layer's name in errors.
    /// </summary>
    public const string Name = "select";

    public Func<TIn, TOut> Selector { get; } = selector;

    internal override IUpstream<TOut> Open(ICursorSource<TIn> input, PipelineRun run) =>
        new SelectUpstream<TIn, TOut>(input, Selector);
}
