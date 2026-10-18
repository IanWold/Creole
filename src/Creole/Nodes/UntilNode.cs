using Creole.Plans;

namespace Creole.Nodes;

/// <summary>
/// Runs <see cref="Item"/> repeatedly until <see cref="Terminator"/> succeeds, and collects the items.
/// The terminator is consumed and its result discarded.
/// </summary>
internal sealed class UntilNode<TIn, T, TTerm>(Parser<TIn, T> item, Parser<TIn, TTerm> terminator) : Parser<TIn, IReadOnlyList<T>>
{
    public Parser<TIn, T> Item { get; } = item;

    public Parser<TIn, TTerm> Terminator { get; } = terminator;

    internal override NodeKind Kind => NodeKind.Until;

    internal override IReadOnlyList<INode> Children => [Item, Terminator];

    internal override Plan<TIn, IReadOnlyList<T>> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, IReadOnlyList<T>>(Kind);
}
