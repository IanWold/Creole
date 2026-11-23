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
/// When the mark saves more than the position and the cut flag, and another alternative ran after the winner, the winner runs again, so those parts are as the winner left them.
/// </remarks>
internal sealed class LongestPlan<TIn, T, TMark>(Plan<TIn, T>[] alternatives) : Plan<TIn, T>
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
            mark.Rewind(ref cursor);
            cursor.Reset(mark.Position);
            cursor.Cut = false;
            T value = default!;

            if (alternatives[i].TryParse(ref cursor, ref value) && cursor.Position > bestEnd)
            {
                bestEnd = cursor.Position;
                result = value;
                winner = i;
            }
        }

        if (TMark.Rewinds && winner >= 0 && winner < alternatives.Length - 1)
        {
            mark.Rewind(ref cursor);
            cursor.Reset(mark.Position);
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
            mark.Rewind(ref cursor);
            cursor.Reset(mark.Position);
            cursor.KeepFailures(in failures);
            return false;
        }

        cursor.Reset(bestEnd);
        cursor.RestoreFailures(in failures);
        return true;
    }
}
