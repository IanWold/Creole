namespace Creole;

/// <summary>
/// Runs <see cref="Source"/> and fails if <see cref="Predicate"/> rejects its result.
/// Backs <c>Where</c>, and the <c>where</c> clause of a query.
/// </summary>
internal sealed class FilterNode<TIn, TOut>(Parser<TIn, TOut> source, Func<TOut, bool> predicate) : Parser<TIn, TOut>
{
    internal override IReadOnlyList<INode> Children => [Source];

    internal override NodeKind Kind => NodeKind.Filter;

    public Func<TOut, bool> Predicate { get; } = predicate;

    public Parser<TIn, TOut> Source { get; } = source;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, TOut>(Kind);
}
