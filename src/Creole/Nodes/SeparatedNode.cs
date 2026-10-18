using Creole.Plans;

namespace Creole.Nodes;

/// <summary>
/// Runs <see cref="Item"/> repeatedly with <see cref="Separator"/> between items and collects the items.
/// Backs <c>SepBy</c>, <c>SepBy1</c> and <c>SepEndBy</c>.
/// </summary>
internal sealed class SeparatedNode<TIn, T, TSep>(Parser<TIn, T> item, Parser<TIn, TSep> separator, int min, bool allowTrailing) : Parser<TIn, IReadOnlyList<T>>
{
    public Parser<TIn, T> Item { get; } = item;

    public Parser<TIn, TSep> Separator { get; } = separator;

    /// <summary>
    /// The fewest items to read: 0 or 1.
    /// </summary>
    public int Min { get; } = min;

    /// <summary>
    /// Whether a separator may follow the last item.
    /// </summary>
    public bool AllowTrailing { get; } = allowTrailing;

    internal override NodeKind Kind => NodeKind.Separated;

    internal override IReadOnlyList<INode> Children => [Item, Separator];

    internal override Plan<TIn, IReadOnlyList<T>> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, IReadOnlyList<T>>(Kind);
}
