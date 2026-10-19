namespace Creole;

/// <summary>
/// Runs <see cref="Item"/> between <see cref="Min"/> and <see cref="Max"/> times and collects the results.
/// Backs <c>Many</c>, <c>AtLeastOnce</c> and <c>Repeat</c>.
/// </summary>
internal sealed class RepeatNode<TIn, T>(Parser<TIn, T> item, int min, int? max) : Parser<TIn, IReadOnlyList<T>>
{
    internal override IReadOnlyList<INode> Children => [Item];

    internal override NodeKind Kind => NodeKind.Repeat;

    public Parser<TIn, T> Item { get; } = item;

    /// <summary>
    /// The most items to read, or null for no limit.
    /// </summary>
    public int? Max { get; } = max;

    public int Min { get; } = min;

    internal override Plan<TIn, IReadOnlyList<T>> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, IReadOnlyList<T>>(Kind);
}
