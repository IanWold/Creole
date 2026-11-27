namespace Creole;

/// <summary>
/// Runs its source and applies a function to its result and the source span of the input it consumed. Fails as its source does.
/// </summary>
/// <remarks>
/// Its start is held while the source runs, so a stream buffer that releases the item there first copies its span.
/// It takes no pin, so it keeps no input alive.
/// </remarks>
internal class SpanPlan<TIn, TSource, TResult>(Plan<TIn, TSource> source, Func<TSource, SourceSpan, TResult> selector) : Plan<TIn, TResult>
{
    private readonly Func<TSource, SourceSpan, TResult> _selector = selector;
    private readonly Plan<TIn, TSource> _source = source;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TResult result)
    {
        var start = cursor.Position;
        TSource value = default!;

        cursor.HoldSpan();

        if (!_source.TryParse(ref cursor, ref value))
        {
            cursor.DropSpan();
            return false;
        }

        var span = cursor.SpanSince(start);

        cursor.DropSpan();
        result = _selector(value, span);
        return true;
    }
}
