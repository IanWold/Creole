namespace Creole;

/// <summary>
/// Runs <see cref="Source"/> and applies <see cref="Selector"/> to its result and the source span of the input it consumed.
/// Backs <c>WithSpan</c>.
/// </summary>
/// <remarks>
/// It is a node of its own rather than <c>Spanned</c> followed by <c>Select</c>, so that a node that keeps what it builds can find it.
/// </remarks>
internal class WithSpanNode<TIn, TSource, TResult>(Parser<TIn, TSource> source, Func<TSource, SourceSpan, TResult> selector) : Parser<TIn, TResult>
{
    internal override IReadOnlyList<INode> Children => [Source];

    internal override NodeKind Kind => NodeKind.WithSpan;

    public Func<TSource, SourceSpan, TResult> Selector { get; } = selector;

    public Parser<TIn, TSource> Source { get; } = source;

    internal override Plan<TIn, TResult> Lower(PlanBuilder builder) =>
        builder.WithSpan(Source, Selector);
}
