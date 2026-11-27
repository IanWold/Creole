namespace Creole;

/// <summary>
/// Runs its item and, if it fails, rewinds so the failure consumed nothing, unless a cut passed inside the item is still in force.
/// </summary>
/// <remarks>
/// The item starts with no cut in force, so a cut passed before the <c>Atomic</c> does not stop it rewinding.
/// On exit, a cut passed inside stays in force for the enclosing parse, whether the item succeeded or failed (ADR-0011 points 2 and 7).
/// Its start is pinned while it runs, so a stream buffer keeps the input it may rewind to.
/// It never rewinds an item that starved.
/// </remarks>
internal class AtomicPlan<TIn, T, TMark>(Plan<TIn, T> item) : Plan<TIn, T>
    where TMark : struct, IMark<TMark>
{
    private readonly Plan<TIn, T> _item = item;

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        var outerCut = cursor.Cut;
        var mark = TMark.Take(ref cursor);
        var floor = cursor.Pin();
        cursor.Cut = false;

        var ok = _item.TryParse(ref cursor, ref result);

        if (!ok && !cursor.Cut && !cursor.Starved)
        {
            mark.Rewind(ref cursor);
            cursor.Reset(mark.Position);
        }

        cursor.Unpin(floor);
        cursor.Cut |= outerCut;
        return ok;
    }
}
