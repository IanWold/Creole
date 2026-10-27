namespace Creole;

/// <summary>
/// Runs every alternative atomically from the same position and keeps the one that consumed the most. A tie goes to the earliest.
/// Fails without consuming if every alternative fails, keeping every alternative's failures.
/// When one succeeds, the furthest failure is put back as it was, so neither the winner's loop exits nor a loser that read further are reported.
/// </summary>
/// <remarks>
/// Each alternative starts with no cut in force, and a cut passed inside one does not survive leaving it (ADR-0011 point 4).
/// When every alternative is Regular, <see cref="DfaLongestPlan{T}"/> replaces this plan (<see cref="LoweringOptions.Dfa"/>).
/// Its start is pinned while it runs, so a stream buffer keeps the input every alternative starts from.
/// </remarks>
internal sealed class LongestPlan<TIn, T>(Plan<TIn, T>[] alternatives) : Plan<TIn, T>
{
    private readonly Plan<TIn, T>[] _alternatives = alternatives;

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        var outerCut = cursor.Cut;
        var start = cursor.Position;
        var floor = cursor.Pin();
        var failures = cursor.SaveFailures();
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
        cursor.Unpin(floor);

        if (bestEnd < 0)
        {
            cursor.Reset(start);
            cursor.KeepFailures(in failures);
            return false;
        }

        cursor.Reset(bestEnd);
        cursor.RestoreFailures(in failures);
        return true;
    }
}
