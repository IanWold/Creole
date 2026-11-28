using System.Diagnostics;

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
/// Each alternative starts from the parts the mark saved, as if it alone had run.
/// When the mark saves more than the position and the cut flag, the plan runs the winner again from the furthest failure it started with, so the winner's diagnostics and state are those it leaves when it runs alone.
/// It does so whenever another alternative ran after the winner, and in a run with a failure log whenever there are several alternatives.
/// An alternative that starves fails the plan at once, without rewinding or trying the alternatives after it.
/// </remarks>
internal class LongestPlan<TIn, T, TMark>(Plan<TIn, T>[] alternatives) : Plan<TIn, T>
    where TMark : struct, IMark<TMark>
{
    private readonly Plan<TIn, T>[] _alternatives = alternatives;

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        var alternatives = _alternatives;
        var outerCut = cursor.Cut;
        var mark = TMark.Take(ref cursor);
        var floor = cursor.Pin();
        var failures = cursor.SaveFailures();
        var bestEnd = -1L;
        var winner = -1;

        for (var i = 0; i < alternatives.Length; i++)
        {
            cursor.Reset(mark.Position);
            mark.Rewind(ref cursor);
            cursor.Cut = false;
            T value = default!;

            var ok = alternatives[i].TryParse(ref cursor, ref value);

            if (cursor.Starved)
            {
                cursor.Cut = outerCut;
                cursor.Unpin(floor);
                cursor.KeepFailures(in failures);
                return false;
            }

            if (ok && cursor.Position > bestEnd)
            {
                bestEnd = cursor.Position;
                result = value;
                winner = i;
            }
        }

        if (TMark.Rewinds
            && winner >= 0
            && alternatives.Length > 1
            && (winner < alternatives.Length - 1 || cursor.Failures is not null))
        {
            cursor.Reset(mark.Position);
            mark.Rewind(ref cursor);
            cursor.ResetFailures(in failures);
            cursor.Cut = false;
            T value = default!;

            var again = alternatives[winner].TryParse(ref cursor, ref value);

            Debug.Assert(again && cursor.Position == bestEnd, "The winner of a Longest ended somewhere else when it ran again.");

            if (again)
            {
                result = value;
            }
        }

        cursor.Cut = outerCut;
        cursor.Unpin(floor);

        if (bestEnd < 0)
        {
            cursor.Reset(mark.Position);
            mark.Rewind(ref cursor);
            cursor.KeepFailures(in failures);
            return false;
        }

        cursor.Reset(bestEnd);
        cursor.RestoreFailures(in failures);
        return true;
    }
}
