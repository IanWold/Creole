namespace Creole;

/// <summary>
/// One run of a <see cref="TransduceLayer{TIn, TOut, TState}"/>: each step runs the step parser and adds its outputs, and the run ends by adding <c>atEnd</c>'s.
/// </summary>
/// <remarks>
/// Each step runs on a cursor of its own, so a cut, the depth and any rewind inside the step stay inside it.
/// The failure log lasts the whole run, so the layer reports its furthest failure across its steps.
/// </remarks>
file sealed class TransduceUpstream<TIn, TOut, TState>(
    StreamBuffer<TIn> input,
    Plan<TIn, Emit<TOut>> step,
    TState state,
    Func<TState, Emit<TOut>>? atEnd,
    string name,
    int maxDepth,
    FailureLog<TIn>? failures
)
    : IUpstream<TOut>
{
    private readonly Func<TState, Emit<TOut>>? _atEnd = atEnd;
    private bool _ended;
    private readonly FailureLog<TIn>? _failures = failures;
    private readonly StreamBuffer<TIn> _input = input;
    private readonly int _maxDepth = maxDepth;
    private readonly string _name = name;
    private long _position;
    private readonly TState _state = state;
    private readonly Plan<TIn, Emit<TOut>> _step = step;

    private void Add(StreamBuffer<TOut> buffer, Emit<TOut> outputs, ref Cursor<TIn> cursor, long stepStart)
    {
        var derived = _input.SpanOf(stepStart, cursor.Position);
        var before = _input.SpanOf(stepStart, stepStart);
        SourceSpan? after = null;
        var afterDerived = false;
        var groupIndex = 0;

        foreach (var output in outputs)
        {
            SourceSpan span;

            if (!output.IsSynthetic)
            {
                span = derived;
                afterDerived = true;
            }
            else if (afterDerived)
            {
                if (after is null)
                {
                    cursor.TryPeek(out _);
                    after = _input.SpanOf(cursor.Position, cursor.Position);
                }

                span = after.Value;
            }
            else
            {
                span = before;
            }

            buffer.Add(output.Item, new StreamSlot(span, stepStart, StateBefore: null, groupIndex++));
        }
    }

    private void AddAtEnd(StreamBuffer<TOut> buffer)
    {
        if (_atEnd is null)
        {
            return;
        }

        var end = _input.SpanOf(_position, _position);
        var groupIndex = 0;

        foreach (var output in _atEnd(_state))
        {
            buffer.Add(output.Item, new StreamSlot(end, _position, StateBefore: null, groupIndex++));
        }
    }

    private LayerFailedException Failed(ref readonly Cursor<TIn> cursor, long position) =>
        new(ParseErrors.Failure(in cursor, _failures, position, _name, _input.SpanOf));

    public bool TryStep(StreamBuffer<TOut> buffer)
    {
        if (_ended)
        {
            return false;
        }

        var cursor = new Cursor<TIn>(_input) { Failures = _failures, MaxDepth = _maxDepth };
        var stepStart = _position;
        var outputs = default(Emit<TOut>);

        cursor.Reset(stepStart);

        try
        {
            if (cursor.IsAtEnd)
            {
                _ended = true;
                AddAtEnd(buffer);
                return true;
            }

            if (!_step.TryParse(ref cursor, ref outputs))
            {
                throw Failed(in cursor, stepStart);
            }
        }
        catch (DepthExceededException exception)
        {
            throw new LayerFailedException(ParseErrors.Depth(exception, in cursor, _failures, _name, _input.SpanOf));
        }

        if (cursor.Position == stepStart)
        {
            throw NullableRepetition.LayerStep();
        }

        Add(buffer, outputs, ref cursor, stepStart);
        _position = cursor.Position;

        return true;
    }
}

/// <summary>
/// Runs <see cref="Step"/> repeatedly to the end of its input, producing the outputs of each run, then the outputs of <see cref="AtEnd"/>.
/// Backs <c>Layer.Transduce</c>.
/// </summary>
internal sealed class TransduceLayer<TIn, TOut, TState>(TState initialState, Parser<TIn, Emit<TOut>> step, Func<TState, Emit<TOut>>? atEnd, string name) : Layer<TIn, TOut>
{
    public Func<TState, Emit<TOut>>? AtEnd { get; } = atEnd;

    public TState InitialState { get; } = initialState;

    public string Name { get; } = name;

    public Parser<TIn, Emit<TOut>> Step { get; } = step;

    internal override IUpstream<TOut> Open(StreamBuffer<TIn> input, ParseOptions options, bool recordFailures) =>
        new TransduceUpstream<TIn, TOut, TState>(
            input,
            PlanCache.Get(Step),
            InitialState,
            AtEnd,
            Name,
            options.MaxDepth,
            recordFailures
                ? new FailureLog<TIn>()
                : null
        );
}
