namespace Creole;

/// <summary>
/// Runs <see cref="Source"/> and transforms its result with <see cref="Selector"/>.
/// Backs the one-parameter <c>Select</c>, <c>As</c> and <c>Ignore</c>, and an N-ary <c>Select</c> over a parser not built by <c>Seq</c>.
/// </summary>
internal sealed class MapNode<TIn, TSource, TResult>(Parser<TIn, TSource> source, Func<TSource, TResult> selector) : Parser<TIn, TResult>
{
    internal override IReadOnlyList<INode> Children => [Source];

    internal override NodeKind Kind => NodeKind.Map;

    public Func<TSource, TResult> Selector { get; } = selector;

    public Parser<TIn, TSource> Source { get; } = source;

    internal override Plan<TIn, TResult> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, TResult>(Kind);
}
