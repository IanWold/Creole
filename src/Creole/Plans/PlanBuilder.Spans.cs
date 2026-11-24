namespace Creole;

internal sealed partial class PlanBuilder
{
    public static Plan<TIn, SourceSpan> Position<TIn>() => new PositionPlan<TIn>();

    public Plan<TIn, Spanned<T>> Spanned<TIn, T>(Parser<TIn, T> item) =>
        new SpanPlan<TIn, T, Spanned<T>>(Lower(item), static (value, span) => new Spanned<T>(value, span));

    public Plan<TIn, TResult> WithSpan<TIn, TSource, TResult>(Parser<TIn, TSource> source, Func<TSource, SourceSpan, TResult> selector) =>
        new SpanPlan<TIn, TSource, TResult>(Lower(source), selector);
}
