namespace Creole;

/// <summary>
/// Runs its item and, if it succeeds, rewinds and keeps its result. A failure after consuming stays committed.
/// A success also puts the furthest failure back as it was, so what the item matched is never reported as expected.
/// </summary>
/// <remarks>
/// A cut passed inside does not survive leaving it (ADR-0011 point 4).
/// Its start is pinned while it runs, so a stream buffer keeps the input it may rewind to.
/// </remarks>
internal sealed class LookaheadPlan<TIn, T>(Plan<TIn, T> item) : Plan<TIn, T>
{
    private readonly Plan<TIn, T> _item = item;

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        var outerCut = cursor.Cut;
        var start = cursor.Position;
        var floor = cursor.Pin();
        var failures = cursor.SaveFailures();
        cursor.Cut = false;

        var ok = _item.TryParse(ref cursor, ref result);

        if (ok)
        {
            cursor.Reset(start);
            cursor.RestoreFailures(in failures);
        }
        else
        {
            cursor.KeepFailures(in failures);
        }

        cursor.Unpin(floor);
        cursor.Cut = outerCut;
        return ok;
    }
}
