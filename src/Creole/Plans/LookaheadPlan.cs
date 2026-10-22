namespace Creole;

/// <summary>
/// Runs its item and, if it succeeds, rewinds and keeps its result. A failure after consuming stays committed.
/// </summary>
/// <remarks>
/// A cut passed inside does not survive leaving it (ADR-0011 point 4).
/// </remarks>
internal sealed class LookaheadPlan<TIn, T>(Plan<TIn, T> item) : Plan<TIn, T>
{
    private readonly Plan<TIn, T> _item = item;

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        var outerCut = cursor.Cut;
        var start = cursor.Position;
        cursor.Cut = false;

        var ok = _item.TryParse(ref cursor, ref result);

        if (ok)
        {
            cursor.Reset(start);
        }

        cursor.Cut = outerCut;
        return ok;
    }
}
