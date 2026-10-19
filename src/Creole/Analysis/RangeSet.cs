namespace Creole;

/// <summary>
/// An immutable set of integer item keys, stored as sorted, disjoint, non-adjacent inclusive ranges.
/// </summary>
/// <remarks>
/// <para>
/// The key space is shared by every kind of item that has an integer identity: a character is its UTF-16 code, a token kind is its enum value.
/// A character class such as <c>Letter</c> is a few hundred ranges; a set of kinds is a list of one-key ranges, which is also the sorted key list a jump table needs.
/// </para>
/// <para>
/// Ranges are normalized on construction, so two sets with the same keys have the same ranges and compare equal.
/// </para>
/// </remarks>
internal sealed class RangeSet : IEquatable<RangeSet>
{
    private readonly (long First, long Last)[] _ranges;

    public static RangeSet Empty { get; } = new([]);

    /// <summary>
    /// Whether the set has no keys.
    /// </summary>
    public bool IsEmpty => _ranges.Length == 0;

    /// <summary>
    /// The ranges, sorted and disjoint. Adjacent ranges are always merged, so no range ends one before the next begins.
    /// </summary>
    public ReadOnlySpan<(long First, long Last)> Ranges => _ranges;

    private RangeSet((long First, long Last)[] ranges) => _ranges = ranges;

    private static RangeSet Normalize(List<(long First, long Last)> ranges)
    {
        if (ranges.Count == 0)
        {
            return Empty;
        }

        ranges.Sort();

        var merged = new List<(long First, long Last)> { ranges[0] };

        foreach (var range in ranges.Skip(1))
        {
            var (first, last) = merged[^1];

            if (range.First <= last || (last != long.MaxValue && range.First == last + 1))
            {
                merged[^1] = (first, Math.Max(last, range.Last));
            }
            else
            {
                merged.Add(range);
            }
        }

        return new([.. merged]);
    }

    /// <summary>
    /// The keys in <c>[<paramref name="min"/>, <paramref name="max"/>]</c> that are not in this set.
    /// </summary>
    public RangeSet Complement(long min, long max)
    {
        var result = new List<(long First, long Last)>();
        var next = min;

        foreach (var (first, last) in _ranges)
        {
            if (last < next)
            {
                continue;
            }

            if (first > max)
            {
                break;
            }

            if (first > next)
            {
                result.Add((next, first - 1));
            }

            if (last >= max)
            {
                return new([.. result]);
            }

            next = last + 1;
        }

        result.Add((next, max));

        return new([.. result]);
    }

    /// <summary>
    /// Whether <paramref name="key"/> is in the set. A binary search over the ranges.
    /// </summary>
    public bool Contains(long key)
    {
        var low = 0;
        var high = _ranges.Length - 1;

        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            var (first, last) = _ranges[middle];

            if (key < first)
            {
                high = middle - 1;
            }
            else if (key > last)
            {
                low = middle + 1;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    public bool Equals(RangeSet? other) =>
        other is not null && _ranges.AsSpan().SequenceEqual(other._ranges);

    public override bool Equals(object? obj) => Equals(obj as RangeSet);

    /// <summary>
    /// A set holding the keys of every range in <paramref name="ranges"/>, which may be unsorted, overlapping or adjacent.
    /// </summary>
    /// <exception cref="ArgumentException">A range's last key is less than its first.</exception>
    public static RangeSet FromRanges(IEnumerable<(long First, long Last)> ranges)
    {
        var list = ranges.ToList();

        foreach (var (first, last) in list)
        {
            if (last < first)
            {
                throw new ArgumentException($"Range {first}..{last} ends before it starts.", nameof(ranges));
            }
        }

        return Normalize(list);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var range in _ranges)
        {
            hash.Add(range);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Whether this set and <paramref name="other"/> share at least one key.
    /// </summary>
    public bool Overlaps(RangeSet other)
    {
        var i = 0;
        var j = 0;

        while (i < _ranges.Length && j < other._ranges.Length)
        {
            if (_ranges[i].Last < other._ranges[j].First)
            {
                i++;
            }
            else if (other._ranges[j].Last < _ranges[i].First)
            {
                j++;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    public override string ToString() =>
        $"{{{string.Join(", ", _ranges.Select(range => range.First == range.Last ? $"{range.First}" : $"{range.First}..{range.Last}"))}}}";

    /// <summary>
    /// The keys in this set, <paramref name="other"/>, or both.
    /// </summary>
    public RangeSet Union(RangeSet other) =>
        other.IsEmpty
        ? this
        : IsEmpty
            ? other
            : Normalize([.. _ranges, .. other._ranges]);
}
