namespace Creole;

/// <summary>
/// Runs <see cref="Item"/> with <see cref="Text"/> pushed onto the context stack, so failures recorded meanwhile carry it.
/// Backs <c>Context</c>.
/// </summary>
internal sealed class ContextNode<TIn, T>(Parser<TIn, T> item, string text) : Parser<TIn, T>
{
    internal override IReadOnlyList<INode> Children => [Item];

    internal override NodeKind Kind => NodeKind.Context;

    public Parser<TIn, T> Item { get; } = item;

    public string Text { get; } = text;

    internal override Plan<TIn, T> Lower(PlanBuilder builder) =>
        builder.Context(Item);

    internal override MatchValue<T>? ValueOfMatch() =>
        Item.ValueOfMatch();
}
