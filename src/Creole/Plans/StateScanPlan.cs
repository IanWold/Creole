namespace Creole;

/// <summary>
/// Runs its source, then passes its result, the layer's state and the source span of the input it consumed to a function that gives the result and the new state. Fails as its source does.
/// </summary>
/// <remarks>
/// <para>
/// The function sees the state as the source left it, and runs on every success, because the state it returns decides later parsing.
/// It throws <see cref="LayerStateException"/> when the layer has no state or its state is not a <typeparamref name="TState"/>.
/// </para>
/// <para>
/// Its start is held while the source runs, as <see cref="SpanPlan{TIn, TSource, TResult}"/>'s is, so a stream buffer that releases the item there first copies its span.
/// A value-type state is boxed each time the function returns it.
/// </para>
/// </remarks>
internal sealed class StateScanPlan<TIn, TSource, TState, TResult>(Plan<TIn, TSource> source, Func<TSource, TState, SourceSpan, (TResult Result, TState State)> function) : Plan<TIn, TResult>
{
    private readonly Func<TSource, TState, SourceSpan, (TResult Result, TState State)> _function = function;
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

        var (scanned, state) = _function(value, StateBox.Read<TState>(cursor.State), span);

        cursor.State = StateBox.Hold(state);
        result = scanned;
        return true;
    }
}
