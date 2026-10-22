namespace Creole;

/// <summary>
/// Runs its source and fails if the predicate rejects the result. Fails as its source does.
/// </summary>
/// <remarks>
/// A rejection leaves the cursor where the source left it, so it is committed if the source consumed input (ADR-0014).
/// </remarks>
internal sealed class FilterPlan<TIn, T>(Plan<TIn, T> source, Func<T, bool> predicate) : Plan<TIn, T>
{
    private readonly Func<T, bool> _predicate = predicate;
    private readonly Plan<TIn, T> _source = source;

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        T value = default!;

        if (!_source.TryParse(ref cursor, ref value) || !_predicate(value))
        {
            return false;
        }

        result = value;
        return true;
    }
}
