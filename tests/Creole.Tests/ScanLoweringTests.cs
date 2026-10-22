using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

/// <summary>
/// The scan rewrite (INT-05): a skipped run of a character class lowers to one vectorized search per segment, and a parse cannot tell.
/// </summary>
public class ScanLoweringTests
{
    private static readonly Gen<CharSet> _sets = Gen.Select(Gen.Char["abc"].Array[0, 3], Gen.Bool, (chars, complement) =>
        complement ? CharSet.Of(chars).Complement() : CharSet.Of(chars));

    private static void AssertAgrees<T>(Parser<char, T> parser, string input, int? segmentSize = null, bool cut = false)
    {
        var (without, with) = Rewrites.Compare(parser, LoweringOptions.Scan, input, segmentSize, cut);

        Assert.Equal(without, with);
    }

    [Fact]
    public void AClassRunAgreesWithRunningTheClassCharacterByCharacter() =>
        Gen
        .Select(_sets, Gen.Int[0, 3], Gen.Int[-1, 3], Gen.Char["abcd"].Array[0, 12], Gen.Int[1, 5], Gen.Bool)
        .Sample((set, min, extra, input, segmentSize, cut) =>
        {
            var max = extra < 0 ? (int?)null : min + extra;
            var parser = new SkipRepeatNode<char, char>(new CharClassNode(set, null), min, max);

            AssertAgrees(parser, new string(input), segmentSize, cut);
            AssertAgrees(parser, new string(input), segmentSize: null, cut);
        });

    [Theory]
    [InlineData("", 0)]
    [InlineData("  x", 2)]
    [InlineData("      ", 6)]
    [InlineData("x", 0)]
    public void ARunCrossesSegmentBoundaries(string input, long position)
    {
        for (var size = 1; size <= 4; size++)
        {
            var (without, with) = Rewrites.Compare(Whitespace.SkipMany(), LoweringOptions.Scan, input, size);

            Assert.Equal(without, with);
            Assert.Equal(position, with.Position);
        }
    }

    [Fact]
    public void ARunInsideASequenceKeepsTheCutBeforeIt()
    {
        var parser = new CutThenNode<Unit>(Digit.SkipAtLeastOnce()).Before(Char(';'));

        AssertAgrees(parser, "12;");
        AssertAgrees(parser, "x");
        AssertAgrees(parser, "12x");
    }

    [Fact]
    public void ARunOfAClassMuchLargerThanItsComplementAgrees()
    {
        AssertAgrees(Letter.SkipMany(), "abcdéñßΩж字 x");
        AssertAgrees(NoneOf('"', '\\').SkipAtLeastOnce(), "plain text, ünïcödé \\ rest");
        AssertAgrees(new CharClassNode(CharSet.All, null).SkipMany(), "anything at all");
    }

    [Fact]
    public void ARunOfTheEmptyClassAgrees()
    {
        AssertAgrees(new CharClassNode(CharSet.Empty, null).SkipMany(), "abc");
        AssertAgrees(new CharClassNode(CharSet.Empty, null).SkipAtLeastOnce(), "abc");
    }

    [Theory]
    [InlineData(2, 2, "aaaa")]
    [InlineData(0, 3, "aaaaa")]
    [InlineData(3, null, "aa")]
    [InlineData(3, 5, "aab")]
    public void ARunStopsAtItsBoundsAsTheItemByItemRunDoes(int min, int? max, string input)
    {
        var parser = new SkipRepeatNode<char, char>(Char('a'), min, max);

        for (var size = 1; size <= 3; size++)
        {
            AssertAgrees(parser, input, size);
        }

        AssertAgrees(parser, input);
    }

    [Fact]
    public void ASkippedRunOfACharacterClassLowersToAScan()
    {
        Assert.IsType<ScanPlan>(new PlanBuilder(LoweringOptions.Scan).Lower(Digit.SkipMany()));
        Assert.IsType<SkipRepeatPlan<char, char>>(new PlanBuilder(LoweringOptions.None).Lower(Digit.SkipMany()));
    }

    [Fact]
    public void ASkippedRunOfSatisfyIsNotScanned() =>
        Assert.IsType<SkipRepeatPlan<char, char>>(new PlanBuilder(LoweringOptions.Scan).Lower(Satisfy(char.IsDigit).SkipMany()));
}
