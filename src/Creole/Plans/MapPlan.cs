namespace Creole;

/// <summary>
/// Runs its source and applies a function to the result. Fails as its source does.
/// </summary>
internal sealed class MapPlan<TIn, TSource, TResult>(Plan<TIn, TSource> source, Func<TSource, TResult> selector) : Plan<TIn, TResult>
{
    private readonly Func<TSource, TResult> _selector = selector;
    private readonly Plan<TIn, TSource> _source = source;

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
