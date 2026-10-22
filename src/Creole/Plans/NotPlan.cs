namespace Creole;

/// <summary>
/// Runs its item, rewinds, and succeeds exactly when the item failed. Never consumes.
/// </summary>
/// <remarks>
/// A cut passed inside does not survive leaving it (ADR-0011 point 4).
/// </remarks>
internal sealed class NotPlan<TIn, T>(Plan<TIn, T> item) : Plan<TIn, Unit>
{
    private readonly Plan<TIn, T> _item = item;

    public override bool TryParse(ref Cursor<TIn> cursor, ref Unit result)
    {
        var outerCut = cursor.Cut;
        var start = cursor.Position;
        cursor.Cut = false;
        T value = default!;

        var ok = _item.TryParse(ref cursor, ref value);

        cursor.Reset(start);
        cursor.Cut = outerCut;
        return !ok;
    }
}
