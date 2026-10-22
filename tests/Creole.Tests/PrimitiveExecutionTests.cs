using System.Globalization;
using CsCheck;

namespace Creole.Tests;

file enum K {
    Name,
    Number,
    Comma
}

file readonly record struct KTok(K Kind, string Text) : IKinded<K>;

file sealed record Ident(string Name);

file sealed record Num(int Value);

file union UTok(Ident, Num) : IKinded<K>
{
    public readonly K Kind => this switch
    {
        Ident => K.Name,
        Num => K.Number,
    };
}

file sealed class KTokens : TokenGrammar<KTok, K>
{
    public static readonly Parser<KTok, KTok> Comma = Kind(K.Comma);
}

file sealed class UTokens : TokenGrammar<UTok, K>
{
    public static readonly Parser<UTok, Ident> Name = Case<Ident>();
    public static readonly Parser<UTok, UTok> NumberKind = Kind(K.Number);
}

public class PrimitiveExecutionTests
{
    private static readonly Outcome _failEmpty = new Outcome.FailEmpty();

    private static readonly Gen<char> _caseChars = Gen.Char["kKsSiIKſİı"];

    private static Outcome.Ok Ok(string value, int consumed) => new(value, consumed);

    [Fact]
    public void AnyReadsOneItem() =>
        Assert.Equal(Ok("a", 1), Execution.Outcome(Grammar<char>.Any, "ab"));

    [Fact]
    public void AnyFailsWithoutConsumingAtTheEnd() =>
        Assert.Equal(_failEmpty, Execution.Outcome(Grammar<char>.Any, ""));

    [Fact]
    public void CaseFailsWithoutConsumingOnAnotherCase() =>
        Assert.Equal(_failEmpty, Execution.Outcome(UTokens.Name, [new UTok(new Num(1))]));

    [Fact]
    public void CaseProducesTheCaseValue() =>
        Assert.Equal(Ok("Ident { Name = x }", 1), Execution.Outcome(UTokens.Name, [new UTok(new Ident("x")), new UTok(new Num(1))]));

    [Fact]
    public void CharacterClassesFailWithoutConsumingOnAnotherCharacter()
    {
        Assert.Equal(_failEmpty, Execution.Outcome(CharGrammar.Char('a'), "b"));
        Assert.Equal(_failEmpty, Execution.Outcome(CharGrammar.Range('a', 'f'), "g"));
        Assert.Equal(_failEmpty, Execution.Outcome(CharGrammar.NoneOf('"', '\\'), "\""));
        Assert.Equal(_failEmpty, Execution.Outcome(CharGrammar.Digit, ""));
    }

    [Fact]
    public void CharacterClassesReadOneMatchingCharacter()
    {
        Assert.Equal(Ok("a", 1), Execution.Outcome(CharGrammar.Char('a'), "ab"));
        Assert.Equal(Ok("f", 1), Execution.Outcome(CharGrammar.Range('a', 'f'), "f"));
        Assert.Equal(Ok("x", 1), Execution.Outcome(CharGrammar.NoneOf('"', '\\'), "x"));
        Assert.Equal(Ok("٣", 1), Execution.Outcome(CharGrammar.Digit, "٣"));
    }

    [Fact]
    public void CIStringIsAtomic() =>
        Assert.Equal(_failEmpty, Execution.Outcome(CharGrammar.CIString("select"), "SELEX"));

    [Fact]
    public void CIStringMatchesWhenLowerCaseFormsAreEqual() =>
        Gen.Select(_caseChars.Array[1, 3], _caseChars.Array[0, 4]).Sample((text, input) =>
        {
            var matches = input.Length >= text.Length && text.Index().All(c => char.ToLowerInvariant(c.Item) == char.ToLowerInvariant(input[c.Index]));
            var expected = matches ? Ok(new string(input, 0, text.Length), text.Length) : _failEmpty;

            return Execution.Outcome(CharGrammar.CIString(new string(text)), new string(input)) == expected;
        });

    [Fact]
    public void CIStringProducesTheInputAsWritten() =>
        Assert.Equal(Ok("SeLeCt", 6), Execution.Outcome(CharGrammar.CIString("select"), "SeLeCt;"));

    [Fact]
    public void CIStringUsesInvariantLowerCase()
    {
        Assert.Equal(Ok("K", 1), Execution.Outcome(CharGrammar.CIString("k"), "K"));
        Assert.Equal(_failEmpty, Execution.Outcome(CharGrammar.CIString("s"), "ſ"));
    }

    [Fact]
    public void EndFailsWithoutConsumingBeforeTheEnd() =>
        Assert.Equal(_failEmpty, Execution.Outcome(Grammar<char>.End, "a"));

    [Fact]
    public void EndSucceedsAtTheEndWithoutConsuming() =>
        Assert.Equal(Ok("()", 0), Execution.Outcome(Grammar<char>.End, "", _ => "()"));

    [Fact]
    public void KindMatchesATokenOfThatKind()
    {
        Assert.Equal(Ok(",", 1), Execution.Outcome(KTokens.Comma, [new KTok(K.Comma, ","), new KTok(K.Name, "a")], t => t.Text));
        Assert.Equal(_failEmpty, Execution.Outcome(KTokens.Comma, [new KTok(K.Name, "a")]));
    }

    [Fact]
    public void KindMatchesAUnionTokenByItsKind()
    {
        Assert.Equal(Ok("1", 1), Execution.Outcome(UTokens.NumberKind, [new UTok(new Num(1))], t => t is Num n ? n.Value.ToString(CultureInfo.InvariantCulture) : "?"));
        Assert.Equal(_failEmpty, Execution.Outcome(UTokens.NumberKind, [new UTok(new Ident("x"))]));
    }

    [Fact]
    public void SatisfyReadsAnItemItsPredicateAccepts()
    {
        var positive = Grammar<byte>.Satisfy(b => b > 0);

        Assert.Equal(Ok("3", 1), Execution.Outcome<byte, byte>(positive, [3, 0]));
        Assert.Equal(_failEmpty, Execution.Outcome<byte, byte>(positive, [0, 3]));
    }

    [Fact]
    public void SingleItemPrimitivesConsumeOneItemOrNothing() =>
        Gen.Select(Gen.Int[0, 5], Gen.Char["a0 \"٣"].Array[0, 3]).Sample((which, input) =>
        {
            var (parser, accepts) = which switch
            {
                0 => (CharGrammar.Char('a'), (Func<char, bool>)(c => c == 'a')),
                1 => (CharGrammar.Range('0', '9'), c => c is >= '0' and <= '9'),
                2 => (CharGrammar.NoneOf('"'), c => c != '"'),
                3 => (CharGrammar.Digit, char.IsDigit),
                4 => (CharGrammar.Whitespace, char.IsWhiteSpace),
                _ => (Grammar<char>.Any, _ => true),
            };
            var expected = input.Length > 0 && accepts(input[0]) ? Ok(input[0].ToString(), 1) : _failEmpty;

            return Execution.Outcome(parser, new string(input)) == expected;
        });

    [Fact]
    public void StringIsAtomic() =>
        Assert.Equal(_failEmpty, Execution.Outcome(CharGrammar.String("abc"), "abd"));

    [Fact]
    public void StringMatchesAcrossSegments()
    {
        var cursor = new Cursor<char>(new SegmentedSource<char>("xabcdx".ToCharArray(), 2));
        cursor.Advance(1);

        var (ok, value) = Execution.Run(CharGrammar.String("abcd"), ref cursor);

        Assert.True(ok);
        Assert.Equal("abcd", value);
        Assert.Equal(5, cursor.Position);
    }

    [Fact]
    public void StringMatchesExactlyWhenTheInputStartsWithIt() =>
        Gen.Select(Gen.Char["ab"].Array[1, 3], Gen.Char["ab"].Array[0, 4]).Sample((text, input) =>
        {
            var expected = new string(input).StartsWith(new string(text), StringComparison.Ordinal) ? Ok(new string(text), text.Length) : _failEmpty;

            return Execution.Outcome(CharGrammar.String(new string(text)), new string(input)) == expected;
        });

    [Fact]
    public void StringProducesItsOwnText()
    {
        const string text = "<=";
        var cursor = new Cursor<char>("<=>");

        var (ok, value) = Execution.Run(CharGrammar.String(text), ref cursor);

        Assert.True(ok);
        Assert.Same(text, value);
    }

    [Fact]
    public void StringThatFailsAcrossSegmentsRewinds()
    {
        var cursor = new Cursor<char>(new SegmentedSource<char>("xabcex".ToCharArray(), 2));
        cursor.Advance(1);

        var (ok, _) = Execution.Run(CharGrammar.String("abcd"), ref cursor);

        Assert.False(ok);
        Assert.Equal(1, cursor.Position);
    }

    [Fact]
    public void TokenReadsAnEqualItem()
    {
        var delete = Grammar<byte>.Token(0x7F);

        Assert.Equal(Ok("127", 1), Execution.Outcome<byte, byte>(delete, [0x7F, 1]));
        Assert.Equal(_failEmpty, Execution.Outcome<byte, byte>(delete, [1]));
    }
}
