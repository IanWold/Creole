using System.Diagnostics;

namespace Creole;

/// <summary>
/// Runs a <c>Longest</c> whose alternatives are all Regular as one commit-aware automaton, reading each character once.
/// </summary>
/// <remarks>
/// <para>
/// The scan records how far each alternative's match reaches. When no path can read further, the longest match wins and a tie goes to the earliest alternative.
/// The value comes from the matched input when the winner's value depends on that alone (<see cref="MatchValue{T}"/>); otherwise the winner runs again on the interpreter.
/// </para>
/// <para>
/// The scan stops at the end of the input without taking the automaton's end-of-input transition: the paths still reading can only die there, which removes no match.
/// When nothing matches, the interpreter's <see cref="LongestPlan{TIn, T}"/> runs instead, so a failure never comes from the tables.
/// A Regular alternative contains no <c>Cut</c>, so a match leaves the cut flag as it found it.
/// </para>
/// </remarks>
internal sealed class DfaLongestPlan<T>(CommitAutomaton automaton, Plan<char, T>[] alternatives, MatchValue<T>?[] values) : Plan<char, T>
{
    private const int MaxStackAlternatives = 1_024;
    private readonly ushort[] _classOf = automaton.ClassOf;
    private readonly int[][] _ends = automaton.Ends;
    private readonly LongestPlan<char, T> _fallback = new(alternatives);
    private readonly int _initial = automaton.Initial;
    private readonly bool[] _live = automaton.Live;
    private readonly int[][] _matched = automaton.Matched;
    private readonly int[] _next = automaton.Next;
    private readonly int _symbols = automaton.Symbols;

    private bool Rerun(ref Cursor<char> cursor, int winner, long start, ref T result)
    {
        var end = cursor.Position;
        var outerCut = cursor.Cut;
        cursor.Reset(start);
        cursor.Cut = false;

        var matched = alternatives[winner].TryParse(ref cursor, ref result);
        Debug.Assert(matched && cursor.Position == end, "The automaton and the interpreter disagree on the winning alternative.");

        cursor.Cut = outerCut;
        return true;
    }

    public override bool TryParse(ref Cursor<char> cursor, ref T result)
    {
        var start = cursor.Position;
        var classOf = _classOf;
        var ends = _ends;
        var live = _live;
        var next = _next;
        var symbols = _symbols;
        var count = alternatives.Length;
        var reach = count <= MaxStackAlternatives
            ? stackalloc int[count]
            : new int[count];

        reach.Clear();

        var state = _initial;
        var read = 0;

        while (live[state])
        {
            var available = cursor.Available;

            if (available.IsEmpty)
            {
                break;
            }

            var i = 0;

            while (i < available.Length && live[state])
            {
                var transition = (state * symbols) + classOf[available[i]];
                state = next[transition];
                i++;

                foreach (var alternative in ends[transition])
                {
                    reach[alternative] = read + i;
                }
            }

            read += i;
            cursor.Advance(i);
        }

        var best = -1;
        var winner = -1;

        foreach (var alternative in _matched[state])
        {
            if (reach[alternative] > best)
            {
                best = reach[alternative];
                winner = alternative;
            }
        }

        if (winner < 0)
        {
            cursor.Reset(start);
            return _fallback.TryParse(ref cursor, ref result);
        }

        cursor.Reset(start + best);

        if (values[winner] is { } value)
        {
            result = value(ref cursor, start);
            return true;
        }

        return Rerun(ref cursor, winner, start, ref result);
    }
}
