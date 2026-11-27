namespace Creole;

/// <summary>
/// The item plans one run of a <see cref="ModalLayer{TIn, TOut, TMode}"/> has used, by mode, so that a step finds its plan without taking the layer's lock.
/// </summary>
/// <param name="layer">The layer, which plans a mode the run has not used yet.</param>
/// <param name="link">The run's link for the layer's channel, whose top is the current mode.</param>
file sealed class ModalItems<TIn, TOut, TMode>(ModalLayer<TIn, TOut, TMode> layer, ModeLink<TMode> link)
{
    private readonly ModalLayer<TIn, TOut, TMode> _layer = layer;
    private readonly ModeLink<TMode> _link = link;
    private readonly List<(TMode Mode, Plan<TIn, TOut> Plan)> _used = [];

    /// <summary>
    /// The item plan for the channel's current mode.
    /// </summary>
    public Plan<TIn, TOut> Current()
    {
        var mode = _link.Stack.Top;

        foreach (var (used, plan) in _used)
        {
            if (EqualityComparer<TMode>.Default.Equals(used, mode))
            {
                return plan;
            }
        }

        var planned = _layer.PlanFor(mode);
        _used.Add((mode, planned));

        return planned;
    }
}

/// <summary>
/// Runs <see cref="Skip"/> and then the item parser for its channel's current mode repeatedly to the end of its input, and produces each item's value.
/// Backs <c>Layer.Modal</c>.
/// </summary>
/// <remarks>
/// <para>
/// It runs as a <see cref="TokensLayer{TIn, TOut}"/> does, except that each step runs the item parser <see cref="Selector"/> gives for the mode on top of the channel's stack.
/// <see cref="Selector"/> runs at most once for each distinct mode for the life of the layer, under a lock, and its plan is kept with the mode.
/// </para>
/// <para>
/// Opening the layer adds its channel to the run, after making the layer's own writer, so layers above it may change the channel and the layer itself may not.
/// </para>
/// </remarks>
internal sealed class ModalLayer<TIn, TOut, TMode>(
    ModeChannel<TMode> channel,
    Func<TMode, Parser<TIn, TOut>> selector,
    Parser<TIn, Unit>? skip,
    InitialState? initialState,
    string name
)
    : Layer<TIn, TOut>
{
    private readonly Lock _gate = new();
    private readonly List<(TMode Mode, Plan<TIn, TOut> Plan)> _planned = [];

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
    /// The item plan for <paramref name="mode"/>, from the item parser <see cref="Selector"/> gives for it the first time any run asks.
    /// </summary>
    /// <exception cref="GrammarException"><see cref="Selector"/> returns null.</exception>
    internal Plan<TIn, TOut> PlanFor(TMode mode)
    {
        lock (_gate)
        {
            foreach (var (planned, plan) in _planned)
            {
                if (EqualityComparer<TMode>.Default.Equals(planned, mode))
                {
                    return plan;
                }
            }

            var item = Selector(mode) ?? throw new GrammarException($"The selector of the {Name} layer returned null for the mode {mode}.");
            var made = PlanCache.Get(item);

            _planned.Add((mode, made));

            return made;
        }
    }
}
