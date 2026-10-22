namespace Creole;

/// <summary>
/// Runs <see cref="Source"/>, passes its result to <see cref="Next"/> to choose the parser to run after it, and combines both results with <see cref="Selector"/>.
/// Backs <c>Bind</c>, and a query clause that uses an earlier range variable.
/// </summary>
/// <remarks>
/// The next parser is code, so structural consumers see only <see cref="Source"/>, and analysis assumes the worst about what follows it.
/// <see cref="Next"/> is a delegate, compiled once when the node is built, so running the node never touches an expression tree.
/// </remarks>
/// <param name="source">The parser that runs first.</param>
/// <param name="next">Chooses the parser to run after <paramref name="source"/> from its result.</param>
/// <param name="selector">Combines the two results.</param>
/// <param name="nextExpression">The caller's expression for <paramref name="next"/>, which names it if it returns null.</param>
internal sealed class BindNode<TIn, TSource, TNext, TResult>(Parser<TIn, TSource> source, Func<TSource, Parser<TIn, TNext>> next, Func<TSource, TNext, TResult> selector, string nextExpression) : Parser<TIn, TResult>
{
    internal override IReadOnlyList<INode> Children => [Source];

    internal override NodeKind Kind => NodeKind.Bind;

    public Func<TSource, Parser<TIn, TNext>> Next { get; } = next;

    public string NextExpression { get; } = nextExpression;

    public Func<TSource, TNext, TResult> Selector { get; } = selector;

    public Parser<TIn, TSource> Source { get; } = source;

    internal override Plan<TIn, TResult> Lower(PlanBuilder builder) =>
        builder.Bind(Source, Next, Selector, NextExpression);
}
