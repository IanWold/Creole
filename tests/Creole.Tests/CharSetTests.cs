namespace Creole.Tests;

public class CharSetTests
{
    public static TheoryData<string> NamedSets => [nameof(CharSet.Digit), nameof(CharSet.Letter), nameof(CharSet.LetterOrDigit), nameof(CharSet.Whitespace)];

    private static (CharSet Set, Func<char, bool> Predicate) Named(string name) => name switch
    {
        nameof(CharSet.Digit) => (CharSet.Digit, char.IsDigit),
        nameof(CharSet.Letter) => (CharSet.Letter, char.IsLetter),
        nameof(CharSet.LetterOrDigit) => (CharSet.LetterOrDigit, char.IsLetterOrDigit),
        nameof(CharSet.Whitespace) => (CharSet.Whitespace, char.IsWhiteSpace),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [Fact]
    public void AllHoldsEveryCodeUnit()
    {
        Assert.True(CharSet.All.Contains(char.MinValue));
        Assert.True(CharSet.All.Contains(char.MaxValue));
        Assert.True(CharSet.All.Complement().IsEmpty);
        Assert.Equal(CharSet.All, CharSet.Empty.Complement());
    }

    [Fact]
    public void ComplementStaysInsideTheCharacterRange()
    {
        var complement = CharSet.Of('a').Complement();

        Assert.Equal([(0L, 'a' - 1L), ('a' + 1L, (long)char.MaxValue)], complement.Keys.Ranges.ToArray());
    }

    [Fact]
    public void DigitIsUnicodeNotOnlyAscii()
    {
        Assert.True(CharSet.Digit.Contains('7'));
        Assert.True(CharSet.Digit.Contains((char)0x0663)); // Arabic-Indic digit three
        Assert.False(CharSet.Digit.Contains('x'));
    }

    [Theory]
    [MemberData(nameof(NamedSets))]
    public void NamedSetsMatchTheirCharPredicateOnEveryCodeUnit(string name)
    {
        var (set, predicate) = Named(name);

        for (var code = 0; code <= char.MaxValue; code++)
        {
            Assert.True(set.Contains((char)code) == predicate((char)code), $"{name} disagrees with its predicate at U+{code:X4}");
        }
    }

    [Fact]
    public void OfIgnoresOrderAndRepeats() =>
        Assert.Equal(CharSet.Range('a', 'c'), CharSet.Of('c', 'a', 'b', 'a'));

    [Fact]
    public void RangeIsInclusive()
    {
        var set = CharSet.Range('a', 'f');

        Assert.True(set.Contains('a'));
        Assert.True(set.Contains('f'));
        Assert.False(set.Contains('g'));
    }

    [Fact]
    public void RangeRejectsAReversedRange() =>
        Assert.Equal(
            "Range 'z'..'a' is empty: 'a' comes before 'z'.",
            Assert.Throws<GrammarException>(() => CharSet.Range('z', 'a')).Message
        );

    [Fact]
    public void ToStringUsesCharacterClassNotation()
    {
        Assert.Equal("[0-9A-Fa-f]", CharSet.Range('0', '9').Union(CharSet.Range('a', 'f')).Union(CharSet.Range('A', 'F')).ToString());
        Assert.Equal(@"[\-\\\^]", CharSet.Of('-', '\\', '^').ToString());
        Assert.Equal(@"[\]]", CharSet.Of(']').ToString());
        Assert.Equal(@"[\u0009\u0020\u00E9]", CharSet.Of(' ', '\t', (char)0xE9).ToString());
        Assert.Equal("[]", CharSet.Empty.ToString());
    }

    [Fact]
    public void UnionMergesAdjacentRanges() =>
        Assert.Equal([('a', 'z')], CharSet.Range('a', 'm').Union(CharSet.Range('n', 'z')).Keys.Ranges.ToArray().Select(range => ((char)range.First, (char)range.Last)));
}
