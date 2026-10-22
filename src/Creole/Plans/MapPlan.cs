namespace Creole;

/// <summary>
/// Runs its source and applies a function to the result. Fails as its source does.
/// </summary>
internal sealed class MapPlan<TIn, TSource, TResult>(Plan<TIn, TSource> source, Func<TSource, TResult> selector) : Plan<TIn, TResult>
{
    private readonly Func<TSource, TResult> _selector = selector;
    private readonly Plan<TIn, TSource> _source = source;

    /// <summary>
    /// Returns one plan that runs this plan's source and applies this plan's function, then <paramref name="selector"/>.
    /// </summary>
    public override Plan<TIn, TNext> Select<TNext>(Func<TResult, TNext> selector) =>
        new MapPlan<TIn, TSource, TResult, TNext>(_source, _selector, selector);

    public override bool TryParse(ref Cursor<TIn> cursor, ref TResult result)
    {
        TSource value = default!;

        if (!_source.TryParse(ref cursor, ref value))
        {
            return false;
        }

        result = _selector(value);
        return true;
    }
}

/// <summary>
/// Runs its source and applies two functions in turn: a chain of <c>Select</c> lowered as one plan. Fails as its source does.
/// </summary>
internal sealed class MapPlan<TIn, TSource, TMiddle, TResult>(Plan<TIn, TSource> source, Func<TSource, TMiddle> first, Func<TMiddle, TResult> second) : Plan<TIn, TResult>
{
    private readonly Func<TSource, TMiddle> _first = first;
    private readonly Func<TMiddle, TResult> _second = second;
    private readonly Plan<TIn, TSource> _source = source;

    /// <summary>
    /// Returns one plan that runs this plan's source and its first function, then its second function composed with <paramref name="selector"/>.
    /// </summary>
    public override Plan<TIn, TNext> Select<TNext>(Func<TResult, TNext> selector)
    {
        var second = _second;

        return new MapPlan<TIn, TSource, TMiddle, TNext>(_source, _first, value => selector(second(value)));
    }

    public override bool TryParse(ref Cursor<TIn> cursor, ref TResult result)
    {
        TSource value = default!;

        if (!_source.TryParse(ref cursor, ref value))
        {
            return false;
        }

        result = _second(_first(value));
        return true;
    }
}
