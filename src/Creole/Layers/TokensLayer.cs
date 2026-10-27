namespace Creole;

/// <summary>
/// One run of a <see cref="TokensLayer{TIn, TOut}"/>: each step skips, then reads one item.
/// </summary>
/// <remarks>
/// Each step runs on a cursor of its own, so a cut, the depth and any rewind inside the step stay inside it.
/// The failure log lasts the whole run, so the layer reports its furthest failure across its steps.
/// The cursor's floor starts where the oldest step whose items are still held began, or at the step's own start, so the input of the running step and of every held item stays in the buffer.
/// Each step starts by releasing the input below that floor, since a step that starts in a new segment reaches it without the cursor releasing anything.
/// </remarks>
file sealed class TokensUpstream<TIn, TOut>(StreamBuffer<TIn> input, Plan<TIn, TOut> item, Plan<TIn, Unit>? skip, string name, int maxDepth, FailureLog<TIn>? failures) : IUpstream<TOut>
{
    private readonly FailureLog<TIn>? _failures = failures;
    private readonly StreamBuffer<TIn> _input = input;
    private readonly Plan<TIn, TOut> _item = item;
    private readonly int _maxDepth = maxDepth;
    private readonly string _name = name;
    private long _position;
    private readonly Plan<TIn, Unit>? _skip = skip;

    private LayerFailedException Failed(ref readonly Cursor<TIn> cursor, long position) =>
        new(ParseErrors.Failure(in cursor, _failures, position, _name, _input.SpanOf));

    public bool TryStep(StreamBuffer<TOut> buffer)
    {
        var stepStart = _position;
        var floor = buffer.OldestStepStart ?? stepStart;
        var cursor = new Cursor<TIn>(_input) { Failures = _failures, Floor = floor, MaxDepth = _maxDepth };
        var itemStart = stepStart;
        var skipped = default(Unit);
        var value = default(TOut)!;

        _input.Release(floor, _failures);
        cursor.Reset(stepStart);

        try
        {
            if (_skip is not null && !_skip.TryParse(ref cursor, ref skipped))
            {
                throw Failed(in cursor, stepStart);
            }

            if (cursor.IsAtEnd)
            {
                _position = cursor.Position;
                return false;
            }

            itemStart = cursor.Position;

            if (!_item.TryParse(ref cursor, ref value))
            {
                throw Failed(in cursor, itemStart);
            }
        }
        catch (DepthExceededException exception)
        {
            throw new LayerFailedException(ParseErrors.Depth(exception, in cursor, _failures, _name, _input.SpanOf));
        }

        if (cursor.Position == itemStart)
        {
            throw NullableRepetition.LayerItem();
        }

        buffer.Add(value, new StreamSlot(_input.SpanOf(itemStart, cursor.Position), stepStart, StateBefore: null, GroupIndex: 0));
        _position = cursor.Position;

        return true;
    }
}

/// <summary>
/// Runs <see cref="Skip"/> and then <see cref="Item"/> repeatedly to the end of its input, and produces each value of <see cref="Item"/>.
/// Backs <c>Layer.Tokens</c>.
/// </summary>
internal sealed class TokensLayer<TIn, TOut>(Parser<TIn, TOut> item, Parser<TIn, Unit>? skip, string name) : Layer<TIn, TOut>
{
    public Parser<TIn, TOut> Item { get; } = item;

    public string Name { get; } = name;

    public Parser<TIn, Unit>? Skip { get; } = skip;

    internal override IUpstream<TOut> Open(StreamBuffer<TIn> input, ParseOptions options, bool recordFailures) =>
        new TokensUpstream<TIn, TOut>(
            input,
            PlanCache.Get(Item),
            Skip is null
                ? null
                : PlanCache.Get(Skip),
            Name,
            options.MaxDepth,
            recordFailures
                ? new FailureLog<TIn>()
                : null
        );
}
