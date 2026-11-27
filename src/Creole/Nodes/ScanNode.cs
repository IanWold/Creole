namespace Creole;

/// <summary>
/// Runs <see cref="Source"/>, then passes its result, the layer's state and the source span of the input it consumed to <see cref="Function"/>, which gives the result to produce and the new state.
/// Backs <c>Scan</c>.
/// </summary>
/// <remarks>
/// <see cref="Function"/> decides the state later parsing reads, so engines run it on every success, even where the result is not used.
/// </remarks>
internal class ScanNode<TIn, TSource, TState, TResult>(Parser<TIn, TSource> source, Func<TSource, TState, SourceSpan, (TResult Result, TState State)> function) : Parser<TIn, TResult>
{
    internal override IReadOnlyList<INode> Children => [Source];

    internal override NodeKind Kind => NodeKind.Scan;

    public Func<TSource, TState, SourceSpan, (TResult Result, TState State)> Function { get; } = function;

    public Parser<TIn, TSource> Source { get; } = source;

    internal override Plan<TIn, TResult> Lower(PlanBuilder builder) =>
        builder.Scan(Source, Function);
}
