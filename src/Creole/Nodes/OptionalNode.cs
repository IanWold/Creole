namespace Creole;

/// <summary>
/// Runs <see cref="Item"/> if it can, and produces an empty <see cref="Maybe{T}"/> if it fails without consuming input.
/// Backs <c>Optional</c>.
/// </summary>
internal sealed class OptionalNode<TIn, T>(Parser<TIn, T> item) : Parser<TIn, Maybe<T>>
{
    internal override IReadOnlyList<INode> Children => [Item];

    internal override NodeKind Kind => NodeKind.Optional;

    public Parser<TIn, T> Item { get; } = item;

    internal override Plan<TIn, Maybe<T>> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, Maybe<T>>(Kind);
}
