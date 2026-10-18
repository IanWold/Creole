namespace Creole.Nodes;

/// <summary>
/// Runs <see cref="Item"/> between <see cref="Min"/> and <see cref="Max"/> times and collects the results.
/// Backs <c>Many</c>, <c>AtLeastOnce</c> and <c>Repeat</c>.
/// </summary>
internal sealed class RepeatNode<TIn, T>(Parser<TIn, T> item, int min, int? max) : Parser<TIn, IReadOnlyList<T>>
{
    public Parser<TIn, T> Item { get; } = item;

    public int Min { get; } = min;

    /// <summary>
    /// The most items to read, or null for no limit.
    /// </summary>
    public int? Max { get; } = max;

    internal override NodeKind Kind => NodeKind.Repeat;

    internal override IReadOnlyList<INode> Children => [Item];
}
