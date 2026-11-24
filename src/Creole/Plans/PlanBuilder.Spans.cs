namespace Creole;

internal sealed partial class PlanBuilder
{
    /// <summary>
    /// What names <paramref name="node"/> in <c>Adjacent</c>'s message: the label of the outermost <c>Named</c> on the way to its leading primitive, or that primitive's expectation, as a choice names an alternative it skips.
    /// </summary>
    private static Expected? NameOf(INode node)
    {
        var (primitive, label) = Keyed(node);

        return label ?? primitive?.Expected;
    }

    public Plan<TIn, (T1, T2)> Adjacent<TIn, T1, T2>(Parser<TIn, T1> first, Parser<TIn, T2> second) =>
        new AdjacentPlan<TIn, T1, T2>(Lower(first), Lower(second), NameOf(first), NameOf(second));

    public static Plan<TIn, SourceSpan> LeadingTrivia<TIn>() => new LeadingTriviaPlan<TIn>();

    public static Plan<TIn, SourceSpan> Position<TIn>() => new PositionPlan<TIn>();

    public Plan<TIn, Spanned<T>> Spanned<TIn, T>(Parser<TIn, T> item) =>
        new SpanPlan<TIn, T, Spanned<T>>(Lower(item), static (value, span) => new Spanned<T>(value, span));

    public Plan<TIn, TResult> WithSpan<TIn, TSource, TResult>(Parser<TIn, TSource> source, Func<TSource, SourceSpan, TResult> selector) =>
        new SpanPlan<TIn, TSource, TResult>(Lower(source), selector);
}
