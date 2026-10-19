using CsCheck;

namespace Creole.Tests;

public class RangeSetTests
{
    private const int Domain = 64;

    private static readonly Gen<RangeSet> _set =
        Gen.Select(Gen.Int[0, Domain - 1], Gen.Int[0, 8], (first, length) => ((long)first, (long)Math.Min(Domain - 1, first + length)))
            .Array[0, 6]
            .Select(RangeSet.FromRanges);

    private static HashSet<long> Model(RangeSet set) =>
        [.. Enumerable.Range(0, Domain).Select(key => (long)key).Where(set.Contains)];

    [Fact]
    public void ComplementHoldsExactlyTheMissingKeysInTheBounds() =>
        _set.Sample(set =>
        {
            var complement = set.Complement(0, Domain - 1);

            return Enumerable.Range(0, Domain).All(key => complement.Contains(key) != set.Contains(key))
                && !complement.Contains(-1)
                && !complement.Contains(Domain);
        });

    [Fact]
    public void ComplementOfAnEmptySetIsTheWholeRange() =>
        Assert.Equal(RangeSet.FromRanges([(3, 9)]), RangeSet.Empty.Complement(3, 9));

    [Fact]
    public void ComplementOfTheWholeRangeIsEmpty() =>
        Assert.True(RangeSet.FromRanges([(0, 10)]).Complement(0, 10).IsEmpty);

    [Fact]
    public void FromRangesRejectsARangeThatEndsBeforeItStarts() =>
        Assert.Throws<ArgumentException>(() => RangeSet.FromRanges([(5, 4)]));

    [Fact]
    public void KeysAtTheEndsOfTheLongRangeDoNotOverflow()
    {
        var set = RangeSet.FromRanges([(long.MaxValue, long.MaxValue), (long.MinValue, long.MinValue), (long.MaxValue - 1, long.MaxValue - 1)]);

        Assert.Equal([(long.MinValue, long.MinValue), (long.MaxValue - 1, long.MaxValue)], set.Ranges.ToArray());
        Assert.True(set.Complement(long.MinValue, long.MaxValue).Contains(0));
        Assert.False(set.Complement(long.MinValue, long.MaxValue).Contains(long.MaxValue));
    }

    [Fact]
    public void OverlapsMatchesTheModel() =>
        Gen.Select(_set, _set).Sample((a, b) => a.Overlaps(b) == Model(a).Overlaps(Model(b)));

    [Fact]
    public void RangesAreNormalized()
    {
        var set = RangeSet.FromRanges([(10, 12), (1, 3), (4, 4), (2, 3), (13, 13), (20, 20)]);

        Assert.Equal([(1L, 4L), (10L, 13L), (20L, 20L)], set.Ranges.ToArray());
    }

    [Fact]
    public void SetsWithTheSameKeysAreEqual() =>
        _set.Sample(set =>
        {
            var rebuilt = RangeSet.FromRanges(Model(set).Select(key => (key, key)));

            return rebuilt.Equals(set) && rebuilt.GetHashCode() == set.GetHashCode();
        });

    [Fact]
    public void StoredRangesAreSortedDisjointAndNonAdjacent() =>
        _set.Sample(set =>
        {
            var ranges = set.Ranges.ToArray();

            return ranges.All(range => range.First <= range.Last)
                && ranges.Zip(ranges.Skip(1)).All(pair => pair.First.Last + 1 < pair.Second.First);
        });

    [Fact]
    public void UnionMatchesTheModel() =>
        Gen.Select(_set, _set).Sample((a, b) => Model(a.Union(b)).SetEquals(Model(a).Union(Model(b))));
}
