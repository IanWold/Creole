using Creole.Plans;

namespace Creole.Nodes;

/// <summary>
/// Runs <see cref="Item"/> between <see cref="Min"/> and <see cref="Max"/> times and discards the results.
/// Backs <c>SkipMany</c> and <c>SkipAtLeastOnce</c>.
/// </summary>
internal sealed class SkipRepeatNode<TIn, T>(Parser<TIn, T> item, int min, int? max) : Parser<TIn, Unit>
{
    public Parser<TIn, T> Item { get; } = item;

    public int Min { get; } = min;

    /// <summary>
    /// The most items to read, or null for no limit.
    /// </summary>
    public int? Max { get; } = max;

    internal override NodeKind Kind => NodeKind.SkipRepeat;

    internal override IReadOnlyList<INode> Children => [Item];

    internal override Plan<TIn, Unit> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, Unit>(Kind);
}
