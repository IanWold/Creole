namespace Creole;

/// <summary>
/// Runs <see cref="Item"/> and rewinds if it succeeds. A cut boundary.
/// Backs <c>Lookahead</c>.
/// </summary>
internal sealed class LookaheadNode<TIn, T>(Parser<TIn, T> item) : Parser<TIn, T>
{
    internal override IReadOnlyList<INode> Children => [Item];

    internal override NodeKind Kind => NodeKind.Lookahead;

    public Parser<TIn, T> Item { get; } = item;

    internal override Plan<TIn, T> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, T>(Kind);
}
