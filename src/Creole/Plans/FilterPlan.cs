namespace Creole;

/// <summary>
/// Runs its source and fails if the predicate rejects the result. Fails as its source does.
/// </summary>
/// <remarks>
/// <para>
/// A rejection leaves the cursor where the source left it, so it is committed if the source consumed input.
/// </para>
/// <para>
/// It is reported where the source started, over the input the source consumed.
/// The failures the source recorded are dropped first, so they cannot outrank it, and then the rejection is recorded with the message if there is one.
/// The source's first item is captured before the source runs, so the error never reads it back from behind the cursor.
/// </para>
/// </remarks>
internal sealed class FilterPlan<TIn, T>(Plan<TIn, T> source, Func<T, bool> predicate, string? message) : Plan<TIn, T>
{
    private readonly string? _message = message;
    private readonly Func<T, bool> _predicate = predicate;
    private readonly Plan<TIn, T> _source = source;

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        var start = cursor.Position;
        var hasFirst = cursor.TryPeek(out var first);
        var failures = cursor.SaveFailures();
        T value = default!;

        if (!_source.TryParse(ref cursor, ref value))
        {
            cursor.KeepFailures(in failures);
            return false;
        }

        if (!_predicate(value))
        {
            cursor.RestoreFailures(in failures);
            cursor.Reject(start, hasFirst, first, _message, null);
            return false;
        }

        cursor.KeepFailures(in failures);
        result = value;
        return true;
    }
}
