namespace Creole.Tests;

file enum K { A, B }

file enum Signed : sbyte { MinusOne = -1, One = 1 }

file enum Wide : ulong { Low = 1, High = ulong.MaxValue }

file record Animal;

file sealed record Dog : Animal;

file sealed record Ident;

file sealed record Number;

file readonly record struct Tok(string Text);

public class FirstSetTests
{
    public static TheoryData<string, string, bool> OverlapCases => new()
    {
        { "digit", "letter", false },
        { "digit", "'7'", true },
        { "kind A", "kind B", false },
        { "kind A", "kind A", true },
        { "case Ident", "case Number", false },
        { "case Ident", "case Ident", true },
        { "case Animal", "case Dog", true },
        { "value x", "value y", false },
        { "value x", "value x", true },

        { "kind A", "case Ident", true },
        { "kind A", "value x", true },
        { "case Ident", "value x", true },

        { "any", "digit", true },
        { "any", "case Ident", true },
        { "any", "end", false },
        { "end", "end", true },
        { "end", "digit", false },

        { "empty", "any", false },
        { "empty", "end", false },
    };

    private static FirstSet Set(string name) => name switch
    {
        "any" => FirstSet.AnyItem,
        "case Animal" => FirstSet.OfCase(typeof(Animal)),
        "case Dog" => FirstSet.OfCase(typeof(Dog)),
        "case Ident" => FirstSet.OfCase(typeof(Ident)),
        "case Number" => FirstSet.OfCase(typeof(Number)),
        "digit" => FirstSet.OfChars(CharSet.Digit),
        "empty" => FirstSet.Empty,
        "end" => FirstSet.EndOfInput,
        "kind A" => FirstSet.OfItem(K.A),
        "kind B" => FirstSet.OfItem(K.B),
        "letter" => FirstSet.OfChars(CharSet.Letter),
        "'7'" => FirstSet.OfItem('7'),
        "value x" => FirstSet.OfItem(new Tok("x")),
        "value y" => FirstSet.OfItem(new Tok("y")),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [Fact]
    public void AnyItemAbsorbsOtherItemsButKeepsEnd()
    {
        var union = FirstSet.OfChars(CharSet.Digit).Union(FirstSet.AnyItem).Union(FirstSet.EndOfInput);

        Assert.True(union.MatchesAnyItem);
        Assert.True(union.MatchesEnd);
        Assert.True(union.Keys.IsEmpty);
    }

    [Fact]
    public void CharactersAndEnumValuesAreKeys()
    {
        Assert.Equal([('a', 'a')], FirstSet.OfItem('a').Keys.Ranges.ToArray().Select(range => ((char)range.First, (char)range.Last)));
        Assert.Equal([(1L, 1L)], FirstSet.OfItem(K.B).Keys.Ranges.ToArray());
        Assert.Equal(FirstSet.OfChars(CharSet.Of('a')), FirstSet.OfItem('a'));
    }

    [Fact]
    public void EnumKeysAreDistinctForEveryUnderlyingType()
    {
        Assert.NotEqual(FirstSet.OfItem(Wide.Low), FirstSet.OfItem(Wide.High));
        Assert.NotEqual(FirstSet.OfItem(Signed.MinusOne), FirstSet.OfItem(Signed.One));
        Assert.False(FirstSet.OfItem(Wide.Low).Overlaps(FirstSet.OfItem(Wide.High)));
    }

    [Fact]
    public void EqualityIgnoresOrder()
    {
        var first = FirstSet.OfCase(typeof(Ident)).Union(FirstSet.OfCase(typeof(Number)));
        var second = FirstSet.OfCase(typeof(Number)).Union(FirstSet.OfCase(typeof(Ident)));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ItemsWithoutAnIntegerKeyAreValues()
    {
        var set = FirstSet.OfItem(new Tok("x"));

        Assert.True(set.Keys.IsEmpty);
        Assert.Equal([new Tok("x")], set.Values);
    }

    [Theory]
    [MemberData(nameof(OverlapCases))]
    public void OverlapsIsSymmetricAndFollowsThePartRules(string left, string right, bool overlaps)
    {
        Assert.Equal(overlaps, Set(left).Overlaps(Set(right)));
        Assert.Equal(overlaps, Set(right).Overlaps(Set(left)));
    }

    [Fact]
    public void UnionCombinesEveryPart()
    {
        var union = FirstSet.OfItem(K.A)
            .Union(FirstSet.OfCase(typeof(Ident)))
            .Union(FirstSet.OfItem(new Tok("x")))
            .Union(FirstSet.EndOfInput);

        Assert.Equal([(0L, 0L)], union.Keys.Ranges.ToArray());
        Assert.Equal([typeof(Ident)], union.Cases);
        Assert.Equal([new Tok("x")], union.Values);
        Assert.True(union.MatchesEnd);
        Assert.False(union.MatchesAnyItem);
    }
}

