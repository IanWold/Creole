namespace Creole;

/// <summary>
/// Runs <see cref="Item"/> between <see cref="Min"/> and <see cref="Max"/> times and discards the results.
/// Backs <c>SkipMany</c> and <c>SkipAtLeastOnce</c>.
/// </summary>
internal sealed class SkipRepeatNode<TIn, T>(Parser<TIn, T> item, int min, int? max) : Parser<TIn, Unit>, IRepetitionNode
{
    internal override IReadOnlyList<INode> Children => [Item];

    internal override NodeKind Kind => NodeKind.SkipRepeat;

    public Parser<TIn, T> Item { get; } = item;

    /// <summary>
    /// The most items to read, or null for no limit.
    /// </summary>
    public int? Max { get; } = max;

    public int Min { get; } = min;

    internal override Plan<TIn, Unit> Lower(PlanBuilder builder) =>
        builder.SkipRepeat(Item, Min, Max);

    internal override MatchValue<Unit> ValueOfMatch() =>
        static (ref _, _) => default;
}
