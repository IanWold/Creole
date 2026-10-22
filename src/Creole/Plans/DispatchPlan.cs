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

    private readonly Plan<TIn, T>[][] _direct;
    private readonly Plan<TIn, T>[][] _ranges;
    private readonly Func<TIn, long> _read;
    private readonly long[] _starts;
    private readonly Plan<TIn, T>[] _unkeyed;

    /// <summary>
    /// The alternatives, in order.
    /// </summary>
    public IReadOnlyList<Plan<TIn, T>> Alternatives { get; }

    /// <param name="alternatives">The alternatives' plans, in order.</param>
    /// <param name="keys">Each alternative's keys, or null for an alternative with none.</param>
    /// <param name="read">Reads the key of an item.</param>
    public DispatchPlan(Plan<TIn, T>[] alternatives, RangeSet?[] keys, Func<TIn, long> read)
    {
        Alternatives = alternatives;
        _read = read;
        _unkeyed = [.. alternatives.Where((_, i) => keys[i] is null)];

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
        _ranges = [.. _starts.Select(start => (Plan<TIn, T>[])[.. alternatives.Where((_, i) => keys[i] is not { } set || set.Contains(start))])];
        _direct = [.. Enumerable.Range(0, DirectKeys).Select(key => Search(key))];
    }

    /// <summary>
    /// The candidates for <paramref name="key"/>, from the range that holds it.
    /// </summary>
    private Plan<TIn, T>[] Search(long key)
    {
        var index = Array.BinarySearch(_starts, key);

        if (index < 0)
        {
            index = ~index - 1;
        }

        return index < 0 ? _unkeyed : _ranges[index];
    }

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        Plan<TIn, T>[] candidates;

        if (cursor.TryPeek(out var item))
        {
            var key = _read(item);
            candidates = (ulong)key < DirectKeys ? _direct[key] : Search(key);
        }
        else
        {
            candidates = _unkeyed;
        }

        foreach (var alternative in candidates)
        {
            var mark = LightMark.Take(ref cursor);

            if (alternative.TryParse(ref cursor, ref result))
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
