using System.Diagnostics;

namespace Creole;

/// <summary>
/// One run of a <see cref="TokensLayer{TIn, TOut}"/>: each step skips, then reads one item.
/// </summary>
/// <remarks>
/// Each step runs on a cursor of its own, so a cut, the depth and any rewind inside the step stay inside it.
/// The failure log lasts the whole run, so the layer reports its furthest failure across its steps.
/// So does the state: each step's cursor starts with the state the step before left, and each item's slot records the state its step started with.
/// The cursor's floor starts at the layer's pin: where the step began that produced the oldest output its reader can still go back to, or the step's own start if there is none.
/// So the input of the running step and of every output the reader can still go back to stays in the buffer, and the cursor states the floor as the layer's horizon when it pulls.
/// Each step starts by releasing the input below that floor, since a step that starts in a new segment reaches it without the cursor releasing anything.
/// Each step asks for its item plan when it reaches its item: <c>Layer.Tokens</c> always gives the same one, and <c>Layer.Modal</c> gives the current mode's.
/// Each step's cursor carries the layer's mode writer, so the layer's grammar changes only channels read below it.
/// A step whose input starves, wherever it was, produces nothing and reports no failure.
/// When the layer above changes the mode of the channel a <c>Layer.Modal</c> reads, the run goes back to the step that produced the first item it discards.
/// That step's diagnostics, and those of every step after it, are withdrawn, because those steps run again, and the layer forgets its failures from the step's start on.
/// A restart bookmark notes the run's position, state, mode log and diagnostic count, and a restart puts them back and forgets every failure.
/// Going back to a step before the bookmark, as a mode change at an item's start can make it, lowers the bookmark to that step and its diagnostics to those still raised, so a restart produces those items again instead of skipping them.
/// </remarks>
file class TokensUpstream<TIn, TOut>(
    ICursorSource<TIn> input,
    Func<Plan<TIn, TOut>> item,
    Plan<TIn, Unit>? skip,
    InitialState? initialState,
    string name,
    int maxDepth,
    FailureLog<TIn>? failures,
    ModeWriter modes,
    ModeLink? reads
)
    : IUpstream<TOut>
{
    private int _bookmarkDiagnostics;
    private long _bookmarkPosition;
    private object? _bookmarkState;
    private readonly FailureLog<TIn>? _failures = failures;
    private readonly InitialState? _initialState = initialState;
    private readonly ICursorSource<TIn> _input = input;
    private readonly Func<Plan<TIn, TOut>> _item = item;
    private readonly int _maxDepth = maxDepth;
    private readonly ModeWriter _modes = modes;
    private readonly string _name = name;
    private long _position;
    private readonly ModeLink? _reads = reads;
    private readonly Plan<TIn, Unit>? _skip = skip;
    private object? _state = initialState?.Held;

    public SourceSpan InputEnd => _input.InputEnd;

    public SourceSpan InputStart => _input.InputStart;

    public bool Starved => _input.Starved;

    private LayerFailedException Failed(ref readonly Cursor<TIn> cursor, long position) =>
        new(ParseErrors.Failure(in cursor, _failures, position, _name, _input.SpanOf));

    public void Bookmark()
    {
        _bookmarkDiagnostics = _failures?.DiagnosticCount ?? 0;
        _bookmarkPosition = _position;
        _bookmarkState = _state;
        _modes.Bookmark();
        _input.Bookmark();
    }

    public void Dispose() => _input.Dispose();

    public void ForgetFailures()
    {
        _failures?.ForgetFailure();
        _input.ForgetFailures();
    }

    public bool Restart()
    {
        _modes.Restart();
        _failures?.Restart(_bookmarkDiagnostics);
        _position = _bookmarkPosition;
        _state = _bookmarkState;
        _input.Restart();

        return true;
    }

    public bool TryGoBack(StreamSlot slot, ModeLink channel)
    {
        if (!ReferenceEquals(channel, _reads))
        {
            return false;
        }

        Debug.Assert(slot.GroupIndex == 0, "A Layer.Modal step produces one item.");

        _position = slot.StepStart;
        _state = slot.StateBefore;
        _failures?.WithdrawFromStep(slot.StepStart);
        _failures?.LowerAt(slot.StepStart);

        if (slot.StepStart < _bookmarkPosition)
        {
            _bookmarkPosition = slot.StepStart;
            _bookmarkState = slot.StateBefore;
        }

        _bookmarkDiagnostics = Math.Min(_bookmarkDiagnostics, _failures?.DiagnosticCount ?? 0);

        return true;
    }

    public bool TryStep(StreamBuffer<TOut> buffer)
    {
        var stepStart = _position;
        var floor = buffer.OldestStepStart ?? stepStart;
        var cursor = new Cursor<TIn>(_input) { Failures = _failures, Floor = floor, MaxDepth = _maxDepth, Modes = _modes, State = _state };
        var itemStart = stepStart;
        var skipped = default(Unit);
        var value = default(TOut)!;

        _failures?.StepStart = stepStart;
        _input.Release(floor, _failures);

        try
        {
            cursor.Reset(stepStart);

            if (_skip is not null && !_skip.TryParse(ref cursor, ref skipped))
            {
                if (cursor.Starved)
                {
                    return false;
                }

                throw Failed(in cursor, stepStart);
            }

            if (cursor.IsAtEnd)
            {
                if (!cursor.Starved)
                {
                    _position = cursor.Position;
                }

                return false;
            }

            itemStart = cursor.Position;

            var parsed = _item().TryParse(ref cursor, ref value);

            if (cursor.Starved)
            {
                return false;
            }

            if (!parsed)
            {
                throw Failed(in cursor, itemStart);
            }
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

        if (cursor.Position == itemStart)
        {
            throw NullableRepetition.LayerItem();
        }

        _initialState?.Check(cursor.State, _name);
        buffer.Add(value, new StreamSlot(_input.SpanOf(itemStart, cursor.Position), stepStart, StateBefore: _state, GroupIndex: 0));
        _position = cursor.Position;
        _state = cursor.State;

        return true;
    }
}

/// <summary>
/// Runs <see cref="Skip"/> and then <see cref="Item"/> repeatedly to the end of its input, and produces each value of <see cref="Item"/>.
/// Backs <c>Layer.Tokens</c>.
/// </summary>
/// <remarks>
/// A layer given an <see cref="InitialState"/> starts with it and checks that each step leaves a state of its type.
/// Otherwise it starts with no state, which its grammar may set.
/// </remarks>
internal class TokensLayer<TIn, TOut>(Parser<TIn, TOut> item, Parser<TIn, Unit>? skip, InitialState? initialState, string name) : Layer<TIn, TOut>
{
    public InitialState? InitialState { get; } = initialState;

    public Parser<TIn, TOut> Item { get; } = item;

    public string Name { get; } = name;

    public Parser<TIn, Unit>? Skip { get; } = skip;

    /// <summary>
    /// A run of a layer that skips, then reads one item, at each step, with the item plan <paramref name="item"/> gives at that step.
    /// </summary>
    /// <remarks>
    /// <c>Layer.Tokens</c> and <c>Layer.Modal</c> both run this way.
    /// A <c>Layer.Modal</c> passes the channel it reads as <paramref name="reads"/>, so the run goes back when the layer above changes that channel's mode.
    /// </remarks>
    internal static IUpstream<TOut> Steps(ICursorSource<TIn> input, Func<Plan<TIn, TOut>> item, Parser<TIn, Unit>? skip, InitialState? initialState, string name, PipelineRun run, ModeWriter modes, ModeLink? reads) =>
        new TokensUpstream<TIn, TOut>(
            input,
            item,
            skip is null
                ? null
                : PlanCache.Get(skip),
            initialState,
            name,
            run.Options.MaxDepth,
            run.Log<TIn>(),
            modes,
            reads
        );

    internal override IUpstream<TOut> Open(ICursorSource<TIn> input, PipelineRun run)
    {
        var item = PlanCache.Get(Item);

        return Steps(input, () => item, Skip, InitialState, Name, run, run.Writer(Name), reads: null);
    }
}
