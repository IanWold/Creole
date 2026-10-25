using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file enum Letter
{
    A,
    B,
    C,
    D,
}

file enum Shade
{
    Light,
    Dark,
}

file readonly record struct Token(Letter Kind) : IKinded<Letter>;

file readonly record struct TwoKinds(Letter Kind, Shade Shade) : IKinded<Letter>, IKinded<Shade>
{
    Shade IKinded<Shade>.Kind => Shade;
}

file static class Letters
{
    public static readonly Parser<Letter, string>[] Alternatives =
    [
        Grammar<Letter>.Token(Letter.A).Select(static _ => "A"),
        Grammar<Letter>.Token(Letter.B).As("B"),
        Grammar<Letter>.Seq(Grammar<Letter>.Token(Letter.A), Grammar<Letter>.Token(Letter.B)).Select(static (_, _) => "AB"),
        Grammar<Letter>.Token(Letter.C).Optional().Select(static _ => "C?"),
        Grammar<Letter>.Return("r"),
    ];
}

file static class Tokens
{
    public static readonly Parser<Token, string>[] Alternatives =
    [
        TokenGrammar<Token, Letter>.Kind(Letter.A).Select(static _ => "A"),
        TokenGrammar<Token, Letter>.Kind(Letter.B).As("B"),
        Grammar<Token>.Seq(TokenGrammar<Token, Letter>.Kind(Letter.A), TokenGrammar<Token, Letter>.Kind(Letter.B)).Select(static (_, _) => "AB"),
        TokenGrammar<Token, Letter>.Kind(Letter.C).Optional().Select(static _ => "C?"),
        TokenGrammar<Token, Letter>.Kind(Letter.D).Named("d").Select(static _ => "D"),
        Grammar<Token>.Return("r"),
    ];
}

public class DispatchLoweringTests
{
    private static readonly Parser<char, string>[] _alternatives =
    [
        Char('a').Select(static _ => "a"),
        String("ab"),
        Char('a').Then(Char('c')).Select(static _ => "ac"),
        Char('a').Then(new CutThenNode<char>(Char('b'))).Select(static _ => "a!b"),
        Atomic(Char('b').Then(Char('a'))).Select(static _ => "ba"),
        CIString("k").Named("k"),
        Char('a').Named("a").Then(Char('d')).Named("ad").Select(static _ => "ad"),
        Digit.Slice().Select(static text => text.ToString()),
        Char('c').Optional().Select(static _ => "c?"),
        new CutThenNode<string>(String("b")),
        Char('b').Or(Char('c')).Select(static c => c.ToString()),
        Satisfy(char.IsDigit).Select(static _ => "d"),
        Return("r"),
        Fail<string>("never"),
    ];

    private static void AssertAgrees<TIn, T>(Parser<TIn, T> parser, TIn[] input, int? segmentSize = null, bool cut = false)
    {
        var (without, with) = Rewrites.Compare(parser, LoweringOptions.Dispatch, input, segmentSize, cut);

        Assert.Equal(without, with);
    }

    private static Gen<int?> SegmentSizes =>
        Gen.Int[0, 3].Select(static size => size == 0 ? (int?)null : size);

    [Fact]
    public void AChoiceOverCharactersAgreesWithTryingEveryAlternativeInOrder() =>
        Gen
        .Select(Gen.Int[0, _alternatives.Length - 1].Array[2, 6], Gen.Char["abcK1K"].Array[0, 4], SegmentSizes, Gen.Bool)
        .Sample((picks, input, segmentSize, cut) =>
            AssertAgrees(new OrNode<char, string>([.. picks.Select(i => _alternatives[i])]), input, segmentSize, cut));

    [Fact]
    public void AChoiceOverEnumItemsAgreesWithTryingEveryAlternativeInOrder() =>
        Gen
        .Select(Gen.Int[0, Letters.Alternatives.Length - 1].Array[2, 5], Gen.Enum<Letter>().Array[0, 3], SegmentSizes, Gen.Bool)
        .Sample((picks, input, segmentSize, cut) =>
            AssertAgrees(new OrNode<Letter, string>([.. picks.Select(i => Letters.Alternatives[i])]), input, segmentSize, cut));

    [Fact]
    public void AChoiceOverTokenKindsAgreesWithTryingEveryAlternativeInOrder() =>
        Gen
        .Select(Gen.Int[0, Tokens.Alternatives.Length - 1].Array[2, 5], Gen.Enum<Letter>().Array[0, 3], SegmentSizes, Gen.Bool)
        .Sample((picks, kinds, segmentSize, cut) =>
            AssertAgrees(
                new OrNode<Token, string>([.. picks.Select(i => Tokens.Alternatives[i])]),
                [.. kinds.Select(static kind => new Token(kind))],
                segmentSize,
                cut));

    [Fact]
    public void AChoiceThatCommitsLeavesTheSameCutAsTryingInOrder()
    {
        var parser = Atomic(Char('x').Then(OneOf(Char('a').Select(static _ => "a"), new CutThenNode<string>(Return("cut")), Char('b').Select(static _ => "b")).Then(Char('z'))));

        foreach (var input in new[] { "xaz", "xbz", "xb", "xcz", "x" })
        {
            AssertAgrees(parser, input.ToCharArray());
        }
    }

    [Fact]
    public void AChoiceDispatchesFromFourKeyedAlternatives()
    {
        var three = OneOf(Char('a').As(1), String("bc").As(2), Digit.As(3), Return(0));
        var four = OneOf(Char('a').As(1), String("bc").As(2), Digit.As(3), Char('x').As(4), Return(0));

        Assert.Equal(4, PlanBuilder.DispatchThreshold);
        Assert.IsType<OrPlan<char, int>>(new PlanBuilder(LoweringOptions.Dispatch).Lower(three));
        Assert.IsType<DispatchPlan<char, int>>(new PlanBuilder(LoweringOptions.Dispatch).Lower(four));
        Assert.IsType<OrPlan<char, int>>(new PlanBuilder(LoweringOptions.None).Lower(four));
    }

    [Fact]
    public void KindsReadThroughDifferentKindTypesStayInOrder()
    {
        var mixed = TokenGrammar<TwoKinds, Letter>.Kind(Letter.A).Or(TokenGrammar<TwoKinds, Shade>.Kind(Shade.Dark));
        var same = TokenGrammar<TwoKinds, Letter>.Kind(Letter.A).Or(TokenGrammar<TwoKinds, Letter>.Kind(Letter.B));

        Assert.IsType<OrPlan<TwoKinds, TwoKinds>>(new PlanBuilder(LoweringOptions.Dispatch, dispatchThreshold: 1).Lower(mixed));
        Assert.IsType<DispatchPlan<TwoKinds, TwoKinds>>(new PlanBuilder(LoweringOptions.Dispatch, dispatchThreshold: 1).Lower(same));
        AssertAgrees(same, [new TwoKinds(Letter.B, Shade.Dark)]);
    }

    [Fact]
    public void ADispatchedChoiceRecordsWhatTheAlternativesItSkipsExpected()
    {
        var choice = OneOf(Char('a'), Char('b').Named("B"), Char('c').Context("in c"), Char('d'), Return('r'));
        var parser = choice.Then(Char('!'));
        var expected = new ParseError(
            new SourceSpan(SourceId.Unnamed, 0, 1),
            "parser",
            new UnexpectedItem("'x'"),
            [new ExpectedItem("'a'"), new Label("B"), new ExpectedItem("'c'"), new ExpectedItem("'d'"), new ExpectedItem("'!'")],
            []
        );

        Assert.IsType<DispatchPlan<char, char>>(new PlanBuilder(LoweringOptions.Dispatch).Lower(choice));
        Assert.Equal(expected, Execution.Error(parser, "x", LoweringOptions.Dispatch));
        Assert.Equal(expected, Execution.Error(parser, "x"));
    }

    [Fact]
    public void ADispatchedChoiceNamesASkippedAlternativeByItsOutermostLabel()
    {
        var choice = OneOf(Char('a').Named("inner").Then(Char('b')).Named("outer"), Char('c'), Char('d'), Char('e'), Return('r'));
        var parser = choice.Then(Char('!'));
        var expected = new ParseError(
            new SourceSpan(SourceId.Unnamed, 0, 1),
            "parser",
            new UnexpectedItem("'x'"),
            [new Label("outer"), new ExpectedItem("'c'"), new ExpectedItem("'d'"), new ExpectedItem("'e'"), new ExpectedItem("'!'")],
            []
        );

        Assert.IsType<DispatchPlan<char, char>>(new PlanBuilder(LoweringOptions.Dispatch).Lower(choice));
        Assert.Equal(expected, Execution.Error(parser, "x", LoweringOptions.Dispatch));
        Assert.Equal(expected, Execution.Error(parser, "x"));
    }

    [Fact]
    public void ADispatchedChoiceRecordsNothingPastACandidateThatSucceeds()
    {
        var parser = OneOf(Char('a'), Char('b'), Return('r'), Char('c'), Char('d'), Char('e')).Then(Char('!'));

        Assert.Equal(
            new ParseError(new SourceSpan(SourceId.Unnamed, 0, 1), "parser", new UnexpectedItem("'x'"), [new ExpectedItem("'a'"), new ExpectedItem("'b'"), new ExpectedItem("'!'")], []),
            Execution.Error(parser, "x", LoweringOptions.Dispatch)
        );
    }

    [Fact]
    public void ADispatchedChoiceAtTheEndOfInputRecordsEveryAlternative() =>
        Assert.Equal(
            new ParseError(new SourceSpan(SourceId.Unnamed, 0, 0), "parser", new EndOfInput(), [new ExpectedItem("'a'"), new ExpectedItem("'b'"), new ExpectedItem("'c'"), new ExpectedItem("'d'")], []),
            Execution.Error(OneOf(Char('a'), Char('b'), Char('c'), Char('d')), "", LoweringOptions.Dispatch)
        );
}
