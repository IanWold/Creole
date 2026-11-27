namespace Creole;

/// <summary>
/// One run of a <see cref="WhereLayer{T}"/>: each step reads one item and produces it, with its span, if it passes.
/// </summary>
/// <remarks>
/// It reads its input without a cursor, so it states its own pin as its horizon with each read, and releases the input itself.
/// The pin is the input item of the oldest output its reader can still go back to, or the item it is reading if there is none.
/// On reaching a new segment, it releases what lies below the pin.
/// A read that would hold more of its input than the buffer's limit ends the parse with an error named <see cref="WhereLayer{T}.Name"/>.
/// </remarks>
file class WhereUpstream<T>(ICursorSource<T> input, Func<T, bool> predicate) : IUpstream<T>
{
    private long _bookmarkPosition;
    private long _bookmarkSegmentStart;
    private readonly ICursorSource<T> _input = input;
    private long _position;
    private readonly Func<T, bool> _predicate = predicate;
    private long _segmentStart;

    public SourceSpan InputEnd => _input.InputEnd;

    public SourceSpan InputStart => _input.InputStart;

    public bool Starved => _input.Starved;

    private bool TryGetSegment(long pin, out ReadOnlyMemory<T> segment, out long segmentStart)
    {
        try
        {
            return _input.TryGetSegment(_position, pin, out segment, out segmentStart);
        }
        catch (BufferLimitExceededException exception)
        {
            throw new LayerFailedException(ParseErrors.BufferLimit<T>(exception, failures: null, WhereLayer<T>.Name, _input.SpanOf));
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

    public bool TryStep(StreamBuffer<T> buffer)
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
internal class WhereLayer<T>(Func<T, bool> predicate) : Layer<T, T>
{
    /// <summary>
    /// The layer's name in errors.
    /// </summary>
    public const string Name = "where";

    public Func<T, bool> Predicate { get; } = predicate;

    internal override IUpstream<T> Open(ICursorSource<T> input, PipelineRun run) =>
        new WhereUpstream<T>(input, Predicate);
}
