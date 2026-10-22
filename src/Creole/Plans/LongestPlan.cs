namespace Creole;

/// <summary>
/// Runs every alternative atomically from the same position and keeps the one that consumed the most. A tie goes to the earliest.
/// Fails without consuming if every alternative fails.
/// </summary>
/// <remarks>
/// Each alternative starts with no cut in force, and a cut passed inside one does not survive leaving it (ADR-0011 point 4).
/// A DFA replaces this plan for regular alternatives in INT-06.
/// </remarks>
internal sealed class LongestPlan<TIn, T>(Plan<TIn, T>[] alternatives) : Plan<TIn, T>
{
    private readonly Plan<TIn, T>[] _alternatives = alternatives;

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        var outerCut = cursor.Cut;
        var start = cursor.Position;
        var bestEnd = -1L;

        foreach (var alternative in _alternatives)
        {
            cursor.Reset(start);
            cursor.Cut = false;
            T value = default!;

            if (alternative.TryParse(ref cursor, ref value) && cursor.Position > bestEnd)
            {
                bestEnd = cursor.Position;
                result = value;
            }
        }

        cursor.Cut = outerCut;
        cursor.Reset(bestEnd < 0 ? start : bestEnd);
        return bestEnd >= 0;
    }
}
