namespace Creole.Tests;

file class Bytes : Grammar<byte>
{
    public static readonly Parser<byte, byte> Anything = Any;
    public static readonly Parser<byte, byte> Delete = Token(0x7F);
    public static readonly Parser<byte, Unit> Finished = End;
    public static readonly Parser<byte, byte> Positive = Satisfy(b => b > 0);
}

file class Chars : CharGrammar
{
    public static readonly Parser<char, char> A = Char('a');
    public static readonly Parser<char, char> Digits = Digit;
    public static readonly Parser<char, string> LessOrEqual = String("<=");
    public static readonly Parser<char, char> LowerHex = Range('a', 'f');
    public static readonly Parser<char, string> Select = CIString("select");
    public static readonly Parser<char, char> StringChar = NoneOf('"', '\\');
    public static readonly Parser<char, char> TokenA = Token('a');
}

public class PrimitiveNodeTests
{
    public static TheoryData<char> CaseSamples => ['s', 'S', 'k', 'i', 'A', '1', '_'];

    private static IPrimitiveNode Primitive<TIn, TOut>(Parser<TIn, TOut> parser, NodeKind kind)
    {
        INode node = parser;

        Assert.Equal(kind, node.Kind);
        Assert.Empty(node.Children);

        return Assert.IsType<IPrimitiveNode>(node, exactMatch: false);
    }

    [Fact]
    public void AnyMatchesAnyItemAndIsShared()
    {
        var any = Primitive(Bytes.Anything, NodeKind.Any);

        Assert.Equal(FirstSet.AnyItem, any.First);
        Assert.False(any.Nullable);
        Assert.False(any.Opaque);
        Assert.Same(Bytes.Anything, Grammar<byte>.Any);
    }

    [Theory]
    [MemberData(nameof(CaseSamples))]
    public void CIStringFirstSetIsEveryCharacterWithTheSameLowerCase(char c)
    {
        var lower = char.ToLowerInvariant(c);
        var expected = CharSet.Of([.. Enumerable.Range(0, char.MaxValue + 1).Select(code => (char)code).Where(other => char.ToLowerInvariant(other) == lower)]);
        var node = Assert.IsType<StringNode>(CharGrammar.CIString(c + "rest"));

        Assert.True(node.IgnoreCase);
        Assert.Equal(FirstSet.OfChars(expected), node.First);
    }

    [Fact]
    public void CIStringComparesByLowerCaseAsPidginDoes()
    {
        Assert.True(Assert.IsType<StringNode>(CharGrammar.CIString("k")).First.Keys.Contains(0x212A));
        Assert.False(Assert.IsType<StringNode>(Chars.Select).First.Keys.Contains(0x017F));
        Assert.True(Assert.IsType<StringNode>(Chars.Select).First.Keys.Contains('S'));
    }

    [Fact]
    public void CharClassesHoldTheirSetAndName()
    {
        var digit = Assert.IsType<CharClassNode>(Chars.Digits);
        var a = Assert.IsType<CharClassNode>(Chars.A);
        var hex = Assert.IsType<CharClassNode>(Chars.LowerHex);

        Assert.Equal(CharSet.Digit, digit.Set);
        Assert.Equal("digit", digit.Name);
        Assert.Equal(CharSet.Of('a'), a.Set);
        Assert.Null(a.Name);
        Assert.Equal(CharSet.Range('a', 'f'), hex.Set);
        Assert.Equal(FirstSet.OfChars(CharSet.Range('a', 'f')), Primitive(Chars.LowerHex, NodeKind.CharClass).First);
    }

    [Fact]
    public void CharMatchesTheSameItemAsToken() =>
        Assert.Equal(Primitive(Chars.A, NodeKind.CharClass).First, Primitive(Chars.TokenA, NodeKind.Token).First);

    [Fact]
    public void EndIsNullableAndStartsOnlyAtTheEndOfInput()
    {
        var end = Primitive(Bytes.Finished, NodeKind.End);

        Assert.Equal(FirstSet.EndOfInput, end.First);
        Assert.True(end.Nullable);
        Assert.False(end.Opaque);
        Assert.Same(Bytes.Finished, Grammar<byte>.End);
    }

    [Fact]
    public void NamedClassesUseTheirCharSets()
    {
        Assert.Equal(CharSet.Letter, Assert.IsType<CharClassNode>(CharGrammar.Letter).Set);
        Assert.Equal(CharSet.LetterOrDigit, Assert.IsType<CharClassNode>(CharGrammar.LetterOrDigit).Set);
        Assert.Equal(CharSet.Whitespace, Assert.IsType<CharClassNode>(CharGrammar.Whitespace).Set);
        Assert.Equal(["letter", "letter or digit", "whitespace"], new[] { CharGrammar.Letter, CharGrammar.LetterOrDigit, CharGrammar.Whitespace }.Select(p => ((CharClassNode)p).Name));
    }

    [Fact]
    public void NoneOfIsTheComplement()
    {
        var set = Assert.IsType<CharClassNode>(Chars.StringChar).Set;

        Assert.False(set.Contains('"'));
        Assert.False(set.Contains('\\'));
        Assert.True(set.Contains('a'));
        Assert.Equal(CharSet.Of('"', '\\').Complement(), set);
        Assert.Equal(CharSet.All, Assert.IsType<CharClassNode>(CharGrammar.NoneOf()).Set);
    }

    [Fact]
    public void RangeRejectsAReversedRange() =>
        Assert.Equal(
            "Range '9'..'0' is empty: '0' comes before '9'.",
            Assert.Throws<GrammarException>(() => CharGrammar.Range('9', '0')).Message
        );

    [Fact]
    public void SatisfyIsOpaqueAndTreatedAsMatchingAnyItem()
    {
        var satisfy = Primitive(Bytes.Positive, NodeKind.Satisfy);

        Assert.Equal(FirstSet.AnyItem, satisfy.First);
        Assert.False(satisfy.Nullable);
        Assert.True(satisfy.Opaque);
    }

    [Fact]
    public void SatisfyRejectsANullPredicate() =>
        Assert.Equal("'predicate' is null.", Assert.Throws<GrammarException>(() => Grammar<int>.Satisfy(null!)).Message);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StringRejectsEmptyAndNullText(bool ignoreCase)
    {
        var name = ignoreCase ? "CIString" : "String";
        Func<string, Parser<char, string>> combinator = ignoreCase
            ? CharGrammar.CIString
            : CharGrammar.String;

        Assert.Equal(
            $"{name}(\"\") matches nothing. To succeed without consuming input, use Return(\"\").",
            Assert.Throws<GrammarException>(() => combinator("")).Message
        );

        Assert.Equal("'text' is null.", Assert.Throws<GrammarException>(() => combinator(null!)).Message);
    }

    [Fact]
    public void StringStartsWithItsFirstCharacter()
    {
        var node = Assert.IsType<StringNode>(Chars.LessOrEqual);

        Assert.Equal("<=", node.Text);
        Assert.False(node.IgnoreCase);
        Assert.Equal(FirstSet.OfChars(CharSet.Of('<')), Primitive(Chars.LessOrEqual, NodeKind.String).First);
        Assert.False(node.Nullable);
    }

    [Fact]
    public void TokenHoldsItsValue()
    {
        var token = Assert.IsType<TokenNode<byte>>(Bytes.Delete);

        Assert.Equal(0x7F, token.Value);
        Assert.Equal(FirstSet.OfItem((byte)0x7F), Primitive(Bytes.Delete, NodeKind.Token).First);
        Assert.False(token.Nullable);
        Assert.False(token.Opaque);
    }
}
