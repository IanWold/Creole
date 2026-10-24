namespace Creole;

/// <summary>
/// Runs <see cref="Item"/> and, if it fails after consuming input with no cut in force, rewinds so the failure counts as empty.
/// Backs <c>Atomic</c>.
/// </summary>
internal sealed class AtomicNode<TIn, T>(Parser<TIn, T> item) : Parser<TIn, T>
{
    internal override IReadOnlyList<INode> Children => [Item];

    internal override NodeKind Kind => NodeKind.Atomic;

    public Parser<TIn, T> Item { get; } = item;

    internal override Plan<TIn, T> Lower(PlanBuilder builder) =>
        builder.Atomic(Item);

    internal override MatchValue<T>? ValueOfMatch() =>
        Item.ValueOfMatch();
}
