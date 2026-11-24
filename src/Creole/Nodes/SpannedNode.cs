namespace Creole;

/// <summary>
/// Runs <see cref="Item"/> and produces its result with the source span of the input it consumed.
/// Backs <c>Spanned</c>.
/// </summary>
internal sealed class SpannedNode<TIn, TOut>(Parser<TIn, TOut> item) : Parser<TIn, Spanned<TOut>>
{
    internal override IReadOnlyList<INode> Children => [Item];

    internal override NodeKind Kind => NodeKind.Spanned;

    public Parser<TIn, TOut> Item { get; } = item;

    internal override Plan<TIn, Spanned<TOut>> Lower(PlanBuilder builder) =>
        builder.Spanned(Item);
}
