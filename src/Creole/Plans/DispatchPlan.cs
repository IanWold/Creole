namespace Creole;

/// <summary>
/// An <c>Or</c> that runs only the alternatives that can start with the next item (ADR-0016).
/// </summary>
/// <remarks>
/// <para>
/// Each alternative either has keys, the FIRST keys of its leading primitive, or none.
/// The candidates for a key are the alternatives whose keys contain it and every alternative with no keys, in their original order.
/// At the end of the input the candidates are the alternatives with no keys.
/// The candidates run exactly as <see cref="OrPlan{TIn, T}"/> runs its alternatives, each with its light mark.
/// </para>
/// <para>
/// Before each candidate, and after the last, the plan records what the alternatives skipped since the previous candidate would have expected, in their order.
/// So it records the same failures as running every alternative in order, and stops recording where a candidate succeeds or fails after consuming, as that would.
/// </para>
/// <para>
/// An alternative that is not a candidate would have failed without consuming input and without passing a cut, so skipping it changes nothing a parse can observe (ADR-0011 point 6).
/// The alternatives a key skips are those of <see cref="Alternatives"/> not among its candidates; the error model records what they would have expected (ERR-02).
/// </para>
/// </remarks>
internal sealed class DispatchPlan<TIn, T> : Plan<TIn, T>
{
    /// <summary>
    /// Keys below this are looked up in a table; others by binary search.
    /// </summary>
    private const int DirectKeys = 128;

    private readonly DispatchStep<TIn, T>[][] _direct;
    private readonly DispatchStep<TIn, T>[][] _ranges;
    private readonly Func<TIn, long> _read;
    private readonly long[] _starts;
    private readonly DispatchStep<TIn, T>[] _unkeyed;

    /// <summary>
    /// The alternatives, in order.
    /// </summary>
    public IReadOnlyList<Plan<TIn, T>> Alternatives { get; }

    /// <param name="alternatives">The alternatives' plans, in order.</param>
    /// <param name="keys">Each alternative's keys, or null for an alternative with none.</param>
    /// <param name="skipped">What each alternative with keys records when it is skipped; null for an alternative with none.</param>
    /// <param name="read">Reads the key of an item.</param>
    public DispatchPlan(Plan<TIn, T>[] alternatives, RangeSet?[] keys, Expected?[] skipped, Func<TIn, long> read)
    {
        Alternatives = alternatives;
        _read = read;
        _unkeyed = Steps(alternatives, skipped, i => keys[i] is null);

        var starts = new SortedSet<long>();

        foreach (var set in keys.OfType<RangeSet>())
        {
            foreach (var (first, last) in set.Ranges)
            {
                starts.Add(first);

                if (last < long.MaxValue)
                {
                    starts.Add(last + 1);
                }
            }
        }

        _starts = [.. starts];
        _ranges = [.. _starts.Select(start => Steps(alternatives, skipped, i => keys[i] is not { } set || set.Contains(start)))];
        _direct = [.. Enumerable.Range(0, DirectKeys).Select(key => Search(key))];
    }

    /// <summary>
    /// The steps for <paramref name="key"/>, from the range that holds it.
    /// </summary>
    private DispatchStep<TIn, T>[] Search(long key)
    {
        var index = Array.BinarySearch(_starts, key);

        if (index < 0)
        {
            index = ~index - 1;
        }

        return index < 0 ? _unkeyed : _ranges[index];
    }

    /// <summary>
    /// The steps for one candidate list: each candidate in order, with a recording step for the skipped alternatives before it, and one after the last.
    /// </summary>
    private static DispatchStep<TIn, T>[] Steps(Plan<TIn, T>[] alternatives, Expected?[] skipped, Func<int, bool> isCandidate)
    {
        var steps = new List<DispatchStep<TIn, T>>();
        var gap = new List<Expected>();

        for (var i = 0; i < alternatives.Length; i++)
        {
            if (isCandidate(i))
            {
                if (gap.Count > 0)
                {
                    steps.Add(new DispatchStep<TIn, T>(null, [.. gap]));
                    gap.Clear();
                }

                steps.Add(new DispatchStep<TIn, T>(alternatives[i], null));
            }
            else if (skipped[i] is { } expected)
            {
                gap.Add(expected);
            }
        }

        if (gap.Count > 0)
        {
            steps.Add(new DispatchStep<TIn, T>(null, [.. gap]));
        }

        return [.. steps];
    }

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        DispatchStep<TIn, T>[] steps;

        if (cursor.TryPeek(out var item))
        {
            var key = _read(item);
            steps = (ulong)key < DirectKeys ? _direct[key] : Search(key);
        }
        else
        {
            steps = _unkeyed;
        }

        foreach (var (alternative, skipped) in steps)
        {
            if (skipped is not null)
            {
                cursor.FailAll(skipped);
                continue;
            }

            var mark = LightMark.Take(ref cursor);

            if (alternative!.TryParse(ref cursor, ref result))
            {
                return true;
            }

            if (!mark.FailedEmpty(ref cursor))
            {
                return false;
            }
        }

        return false;
    }
}
