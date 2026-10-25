namespace Creole;

/// <summary>
/// Runs <see cref="Source"/> and fails if <see cref="Predicate"/> rejects its result.
/// Backs <c>Where</c>, and the <c>where</c> clause of a query.
/// </summary>
/// <remarks>
/// A rejection leaves the cursor where <see cref="Source"/> left it, so it is committed if the source consumed input.
/// It is reported at the source's start position, over the input the source consumed.
/// </remarks>
internal sealed class FilterNode<TIn, TOut>(Parser<TIn, TOut> source, Func<TOut, bool> predicate, string? message) : Parser<TIn, TOut>
{
    internal override IReadOnlyList<INode> Children => [Source];

    internal override NodeKind Kind => NodeKind.Filter;

    /// <summary>
    /// What a rejection reports, or null to report the rejected input as unexpected.
    /// </summary>
    public string? Message { get; } = message;

    public Func<TOut, bool> Predicate { get; } = predicate;

    public Parser<TIn, TOut> Source { get; } = source;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        builder.Filter(Source, Predicate, Message);
}
