using System.Buffers;

namespace Creole;

/// <summary>
/// Skips a run of characters in one class, as <see cref="SkipRepeatPlan{TIn, T}"/> over that class does, with one vectorized search per segment.
/// </summary>
/// <remarks>
/// <para>
/// <c>SkipMany</c>, <c>SkipAtLeastOnce</c> and bounded skips over a character class lower to this plan when <see cref="LoweringOptions.Scan"/> is on.
/// </para>
/// <para>
/// A character class never touches the cut flag and never succeeds without consuming.
/// So each character needs no light mark, the nullable-repetition guard can never fire, and stopping the run is always a failure without consuming.
/// </para>
/// </remarks>
internal sealed class ScanPlan : Plan<char, Unit>
{
    private readonly bool _except;
    private readonly int? _max;
    private readonly int _min;
    private readonly SearchValues<char> _values;

    /// <param name="values">The characters to search for.</param>
    /// <param name="except">Whether <paramref name="values"/> is the class, so the run ends at the first character not in it, rather than the class's complement.</param>
    /// <param name="min">The fewest characters the run must have.</param>
    /// <param name="max">The most characters to skip, or null for no limit.</param>
    private ScanPlan(SearchValues<char> values, bool except, int min, int? max)
    {
        _values = values;
        _except = except;
        _min = min;
        _max = max;
    }

    private static char[] Chars(RangeSet keys)
    {
        var chars = new List<char>();

        foreach (var (first, last) in keys.Ranges)
        {
            for (var key = first; key <= last; key++)
            {
                chars.Add((char)key);
            }
        }

        return [.. chars];
    }

    /// <summary>
    /// A scan over <paramref name="set"/>, searching for whichever of the set and its complement is smaller.
    /// </summary>
    public static ScanPlan Of(CharSet set, int min, int? max)
    {
        var size = 0L;

        foreach (var (first, last) in set.Keys.Ranges)
        {
            size += last - first + 1;
        }

        var except = size <= char.MaxValue + 1 - size;
        var values = SearchValues.Create(Chars(except ? set.Keys : set.Complement().Keys));

        return new ScanPlan(values, except, min, max);
    }

    public override bool TryParse(ref Cursor<char> cursor, ref Unit result)
    {
        var count = 0L;

        while (_max is not { } max || count < max)
        {
            var available = cursor.Available;

            if (available.IsEmpty)
            {
                break;
            }

            var end = _except ? available.IndexOfAnyExcept(_values) : available.IndexOfAny(_values);
            var run = end < 0 ? available.Length : end;

            if (_max is { } limit && run > limit - count)
            {
                run = (int)(limit - count);
            }

            cursor.Advance(run);
            count += run;

            if (run < available.Length)
            {
                break;
            }
        }

        return count >= _min;
    }
}
