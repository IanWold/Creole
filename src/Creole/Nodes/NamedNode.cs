namespace Creole;

/// <summary>
/// Runs <see cref="Item"/> and, when it consumes nothing, replaces the expectations it recorded at its start position with <see cref="Label"/>.
/// Backs <c>Named</c>.
/// </summary>
internal sealed class NamedNode<TIn, T>(Parser<TIn, T> item, Label label) : Parser<TIn, T>
{
    internal override IReadOnlyList<INode> Children => [Item];

    internal override NodeKind Kind => NodeKind.Named;

    public Parser<TIn, T> Item { get; } = item;

    public Label Label { get; } = label;

    internal override Plan<TIn, T> Lower(PlanBuilder builder) =>
        builder.Named(Item);

    internal override MatchValue<T>? ValueOfMatch() =>
        Item.ValueOfMatch();
}
