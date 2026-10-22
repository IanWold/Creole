namespace Creole;

/// <summary>
/// Runs <see cref="Item"/>, rewinds, and succeeds if it failed. Never consumes input. A cut boundary.
/// Backs <c>Not</c>.
/// </summary>
internal sealed class NotNode<TIn, T>(Parser<TIn, T> item) : Parser<TIn, Unit>
{
    internal override IReadOnlyList<INode> Children => [Item];

    internal override NodeKind Kind => NodeKind.Not;

    public Parser<TIn, T> Item { get; } = item;

    internal override Plan<TIn, Unit> Lower(PlanBuilder builder) =>
        builder.Not(Item);
}
