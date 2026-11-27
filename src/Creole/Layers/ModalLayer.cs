namespace Creole;

/// <summary>
/// The item plans one run of a <see cref="ModalLayer{TIn, TOut, TMode}"/> uses, so that a step finds its plan without taking the layer's lock.
/// </summary>
/// <remarks>
/// It remembers the last stack it read and that stack's plan, so a step whose stack has not changed compares nothing.
/// A run that reads a second stack keeps the plans of the modes it has used, up to <see cref="RecentPlans{TIn, TOut, TMode}.Capacity"/>, and asks the layer only for a mode it has not kept.
/// </remarks>
/// <param name="layer">The layer, which plans a mode the run has not kept.</param>
/// <param name="link">The run's link for the layer's channel, whose top is the current mode.</param>
file class ModalItems<TIn, TOut, TMode>(ModalLayer<TIn, TOut, TMode> layer, ModeLink<TMode> link)
{
    private (ModeStack<TMode> Stack, Plan<TIn, TOut> Plan)? _last;
    private readonly ModalLayer<TIn, TOut, TMode> _layer = layer;
    private readonly ModeLink<TMode> _link = link;
    private RecentPlans<TIn, TOut, TMode>? _used;

    private Plan<TIn, TOut> PlanFor(TMode mode)
    {
        if (_last is not { } last)
        {
            return _layer.PlanFor(mode);
        }

        if (_used is null)
        {
            _used = new();
            _used.Add(last.Stack.Top, last.Plan);
        }

        if (_used.TryGet(mode, out var plan))
        {
            return plan;
        }

        plan = _layer.PlanFor(mode);
        _used.Add(mode, plan);

        return plan;
    }

    /// <summary>
    /// The item plan for the channel's current mode.
    /// </summary>
    public Plan<TIn, TOut> Current()
    {
        var stack = _link.Stack;

        if (_last is { } last && ReferenceEquals(last.Stack, stack))
        {
            return last.Plan;
        }

        var plan = PlanFor(stack.Top);

        _last = (stack, plan);

        return plan;
    }
}

/// <summary>
/// Runs <see cref="Skip"/> and then the item parser for its channel's current mode repeatedly to the end of its input, and produces each item's value.
/// Backs <c>Layer.Modal</c>.
/// </summary>
/// <remarks>
/// <para>
/// It runs as a <see cref="TokensLayer{TIn, TOut}"/> does, except that each step runs the item parser <see cref="Selector"/> gives for the mode on top of the channel's stack.
/// The layer keeps the plans of the <see cref="RecentPlans{TIn, TOut, TMode}.Capacity"/> modes used most recently, under a lock.
/// <see cref="Selector"/> runs, under the lock, only for a mode whose plan the layer does not keep: at most once for each distinct mode while its plan is kept.
/// </para>
/// <para>
/// Opening the layer adds its channel to the run, after making the layer's own writer, so layers above it may change the channel and the layer itself may not.
/// </para>
/// </remarks>
internal class ModalLayer<TIn, TOut, TMode>(
    ModeChannel<TMode> channel,
    Func<TMode, Parser<TIn, TOut>> selector,
    Parser<TIn, Unit>? skip,
    InitialState? initialState,
    string name
)
    : Layer<TIn, TOut>
{
    private readonly Lock _gate = new();
    private readonly RecentPlans<TIn, TOut, TMode> _planned = new();

    /// <summary>
    /// The number of modes whose plans the layer keeps.
    /// </summary>
    internal int PlannedCount
    {
        get
        {
            lock (_gate)
            {
                return _planned.Count;
            }
        }
    }

    internal override IEnumerable<(object Channel, string Layer)> Readers => [(Channel, Name)];

    public ModeChannel<TMode> Channel { get; } = channel;

    public InitialState? InitialState { get; } = initialState;

    public string Name { get; } = name;

    public Func<TMode, Parser<TIn, TOut>> Selector { get; } = selector;

    public Parser<TIn, Unit>? Skip { get; } = skip;

    internal override IUpstream<TOut> Open(ICursorSource<TIn> input, PipelineRun run)
    {
        var modes = run.Writer(Name);
        var link = run.Read(Channel);
        var items = new ModalItems<TIn, TOut, TMode>(this, link);

        return TokensLayer<TIn, TOut>.Steps(input, items.Current, Skip, InitialState, Name, run, modes, link);
    }

    /// <summary>
    /// The item plan for <paramref name="mode"/>: the one the layer keeps, or one made from the item parser <see cref="Selector"/> gives for it.
    /// </summary>
    /// <exception cref="GrammarException"><see cref="Selector"/> returns null.</exception>
    internal Plan<TIn, TOut> PlanFor(TMode mode)
    {
        lock (_gate)
        {
            if (_planned.TryGet(mode, out var plan))
            {
                return plan;
            }

            var item = Selector(mode) ?? throw new GrammarException($"The selector of the {Name} layer returned null for the mode {mode}.");
            var made = PlanCache.Get(item);

            _planned.Add(mode, made);

            return made;
        }
    }
}
