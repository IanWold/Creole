namespace Creole;

/// <summary>
/// One run of a <see cref="TransduceLayer{TIn, TOut, TState}"/>: each step runs the step parser and adds its outputs, and the run ends by adding <c>atEnd</c>'s.
/// </summary>
/// <remarks>
/// Each step runs on a cursor of its own, so a cut, the depth and any rewind inside the step stay inside it.
/// The failure log lasts the whole run, so the layer reports its furthest failure across its steps.
/// So does the state: each step's cursor starts with the state the step before left, and each output's slot records the state its step started with.
/// <c>atEnd</c> gets the state the last step left.
/// The cursor's floor starts at the layer's pin: where the step began that produced the oldest output its reader can still go back to, or the step's own start if there is none.
/// So the input of the running step and of every output the reader can still go back to stays in the buffer, and the cursor states the floor as the layer's horizon when it pulls.
/// Each step starts by releasing the input below that floor, since a step that starts in a new segment reaches it without the cursor releasing anything.
/// </remarks>
file sealed class TransduceUpstream<TIn, TOut, TState>(
    ICursorSource<TIn> input,
    Plan<TIn, Emit<TOut>> step,
    InitialState initialState,
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
    private readonly InitialState _initialState = initialState;
    private readonly ICursorSource<TIn> _input = input;
    private readonly int _maxDepth = maxDepth;
    private readonly string _name = name;
    private long _position;
    private object? _state = initialState.Held;
    private readonly Plan<TIn, Emit<TOut>> _step = step;

    public SourceSpan InputEnd => _input.InputEnd;

    public SourceSpan InputStart => _input.InputStart;

    private void Add(StreamBuffer<TOut> buffer, Emit<TOut> outputs, ref Cursor<TIn> cursor, long stepStart, object? stateBefore)
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

            buffer.Add(output.Item, new StreamSlot(span, stepStart, stateBefore, groupIndex++));
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

        foreach (var output in _atEnd(StateBox.Read<TState>(_state)))
        {
            buffer.Add(output.Item, new StreamSlot(end, _position, _state, groupIndex++));
        }
    }

    private LayerFailedException Failed(ref readonly Cursor<TIn> cursor, long position) =>
        new(ParseErrors.Failure(in cursor, _failures, position, _name, _input.SpanOf));

    public void Dispose() => _input.Dispose();

    public void ForgetFailures()
    {
        _failures?.ForgetFailure();
        _input.ForgetFailures();
    }

    public bool TryStep(StreamBuffer<TOut> buffer)
    {
        if (_ended)
        {
            return false;
        }

        var stepStart = _position;
        var floor = buffer.OldestStepStart ?? stepStart;
        var cursor = new Cursor<TIn>(_input) { Failures = _failures, Floor = floor, MaxDepth = _maxDepth, State = _state };
        var outputs = default(Emit<TOut>);

        _failures?.StepStart = stepStart;
        _input.Release(floor, _failures);

        try
        {
            cursor.Reset(stepStart);

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

            if (cursor.Position == stepStart)
            {
                throw NullableRepetition.LayerStep();
            }

            _initialState.Check(cursor.State, _name);
            Add(buffer, outputs, ref cursor, stepStart, _state);
        }
        catch (DepthExceededException exception)
        {
            throw new LayerFailedException(ParseErrors.Depth(exception, in cursor, _failures, _name, _input.SpanOf));
        }
        catch (LayerStateException exception)
        {
            throw exception.ToGrammarException(_name);
        }
        catch (BufferLimitExceededException exception)
        {
            throw new LayerFailedException(ParseErrors.BufferLimit(exception, _failures, _name, _input.SpanOf));
        }

        _position = cursor.Position;
        _state = cursor.State;

        return true;
    }
}

/// <summary>
/// Runs <see cref="Step"/> repeatedly to the end of its input, producing the outputs of each run, then the outputs of <see cref="AtEnd"/>.
/// Backs <c>Layer.Transduce</c>.
/// </summary>
/// <remarks>
/// The layer starts with its <see cref="InitialState"/> and checks that each step leaves a state of its type, which <see cref="AtEnd"/> takes.
/// </remarks>
internal sealed class TransduceLayer<TIn, TOut, TState>(TState initialState, Parser<TIn, Emit<TOut>> step, Func<TState, Emit<TOut>>? atEnd, string name) : Layer<TIn, TOut>
{
    public Func<TState, Emit<TOut>>? AtEnd { get; } = atEnd;

    public InitialState InitialState { get; } = InitialState.Of(initialState);

    public string Name { get; } = name;

    public Parser<TIn, Emit<TOut>> Step { get; } = step;

    internal override IUpstream<TOut> Open(ICursorSource<TIn> input, ParseOptions options, bool recordFailures) =>
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
