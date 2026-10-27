namespace Creole;

/// <summary>
/// Runs its item, rewinds, and succeeds exactly when the item failed. Never consumes.
/// Either way it puts the furthest failure back as it was, so what the item rules out is never reported as expected.
/// When the item succeeds, it then records a failure where it started that expects nothing in particular.
/// </summary>
/// <remarks>
/// A cut passed inside does not survive leaving it (ADR-0011 point 4).
/// Its start is pinned while it runs, so a stream buffer keeps the input it rewinds to.
/// </remarks>
internal sealed class NotPlan<TIn, T>(Plan<TIn, T> item) : Plan<TIn, Unit>
{
    private readonly Plan<TIn, T> _item = item;

    public override bool TryParse(ref Cursor<TIn> cursor, ref Unit result)
    {
        var outerCut = cursor.Cut;
        var start = cursor.Position;
        var floor = cursor.Pin();
        var failures = cursor.SaveFailures();
        cursor.Cut = false;
        T value = default!;

        var ok = _item.TryParse(ref cursor, ref value);

        cursor.Reset(start);
        cursor.Unpin(floor);
        cursor.RestoreFailures(in failures);
        cursor.Cut = outerCut;

        if (ok)
        {
            cursor.Fail(null);
        }

        return !ok;
    }
}
