using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file enum Symbol
{
    Name,
    Lt,
    Gt,
}

file readonly record struct Tok(Symbol Kind, string Text) : IKinded<Symbol>
{
    static string IKinded<Symbol>.Describe(Symbol kind) => kind switch
    {
        Symbol.Lt => "'<'",
        Symbol.Gt => "'>'",
        _ => "name",
    };
}

/// <summary>
/// A lexer that emits each <c>&gt;</c> as its own token, and a grammar that joins two of them into a shift.
/// <see cref="Expr"/> is the code sample in docs/concepts/spans.md § Joining split tokens. Keep the two in step.
/// </summary>
file class Generics : TokenGrammar<Tok, Symbol>
{
    private static readonly Parser<Tok, string> _name = Kind(Symbol.Name).Select(static token => token.Text);

    public static readonly Parser<Tok, (Tok, Tok)> Anything = Adjacent(Any, Any);

    public static readonly Parser<Tok, string> Committed = Expression.Build(_name, static op =>
    [
        [op.InfixL(Adjacent(Kind(Symbol.Gt), Kind(Symbol.Gt)), static (left, _, right) => $"({left} >> {right})")],
        [op.InfixL(Kind(Symbol.Gt), static (left, _, right) => $"({left} > {right})")],
    ]);

    public static readonly Parser<Tok, int> EmptyThenGt = _name.Then(Adjacent(Return(0), Kind(Symbol.Gt))).Select(static (_, _) => 1);

    public static readonly Parser<Tok, string> Expr = Expression.Build(_name, static op =>
    [
        [op.InfixL(Atomic(Adjacent(Kind(Symbol.Gt), Kind(Symbol.Gt))), static (left, _, right) => $"({left} >> {right})")],
        [op.InfixL(Kind(Symbol.Gt), static (left, _, right) => $"({left} > {right})")],
    ]);

    public static readonly Layer<char, Tok> Lexer = Layer.Tokens(
        CharGrammar.OneOf(
            Letter.AtLeastOnce().Text().Select(static text => new Tok(Symbol.Name, text)),
            Char('<').As(new Tok(Symbol.Lt, "<")),
            Char('>').As(new Tok(Symbol.Gt, ">"))
        ),
        skip: Whitespace.SkipMany()
    );

    public static readonly Parser<Tok, (string, Tok)> NameThenGt = Adjacent(_name, Kind(Symbol.Gt));

    public static readonly Parser<Tok, int> NameThenShifts = Adjacent(_name, Kind(Symbol.Gt).AtLeastOnce()).Select(static (_, shifts) => shifts.Count);

    public static readonly Parser<Tok, (Tok, Tok)> Shift = Adjacent(Kind(Symbol.Gt), Kind(Symbol.Gt));

    public static readonly Parser<Tok, int> ShiftsThenName = Adjacent(Kind(Symbol.Gt).AtLeastOnce(), _name).Select(static (shifts, _) => shifts.Count);

    public static readonly Parser<Tok, string> Type =
        Seq(_name, Rec(() => Type).Between(Kind(Symbol.Lt), Kind(Symbol.Gt)).Optional())
            .Select(static (name, argument) => argument switch
            {
                string inner => $"{name}[{inner}]",
                Nothing => name,
            });
}

file class Words : Grammar<string>
{
    public static readonly Parser<string, (string, string)> Pair = Adjacent(Any, Any);
}

public class AdjacentTests
{
    private static readonly string[] _twoWords = ["ab", "cd"];

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(1024, false)]
    [InlineData(1024, true)]
    public void AdjacentReadsTheSameSpansWhenTheBufferReleasesItsInputWhileItRuns(int segmentSize, bool recordFailures)
    {
        Assert.Equal((true, 6, 7L), LayerRun.Run(Generics.Lexer, Generics.NameThenShifts, "a>>>>>>", segmentSize, recordFailures));
        Assert.Equal((false, 0, 7L), LayerRun.Run(Generics.Lexer, Generics.NameThenShifts, "a >>>>>>", segmentSize, recordFailures));
        Assert.Equal((true, 6, 7L), LayerRun.Run(Generics.Lexer, Generics.ShiftsThenName, ">>>>>>a", segmentSize, recordFailures));
        Assert.Equal((false, 0, 7L), LayerRun.Run(Generics.Lexer, Generics.ShiftsThenName, ">>>>>> a", segmentSize, recordFailures));
    }

    [Fact]
    public void AnEmptyFirstParserTouchesWhatFollows() =>
        Assert.Equal((true, 1, 2L), LayerRun.Run(Generics.Lexer, Generics.EmptyThenGt, "a  >"));

    [Fact]
    public void AGtTokenOnItsOwnIsAComparison() =>
        Assert.Equal((true, "(a > b)", 3L), LayerRun.Run(Generics.Lexer, Generics.Expr, "a > b"));

    [Fact]
    public void ARejectionIsReportedWhereTheFirstStartedOverBoth() =>
        Assert.Equal(
            "error[parser] at 0..3: '>' must come right after '>', with nothing between them",
            LayerRun.Error(Generics.Lexer, Generics.Shift, "> >")?.ToString()
        );

    [Fact]
    public void ARejectionNamesTheSecondParserFirst() =>
        Assert.Equal(
            "error[parser] at 0..3: '>' must come right after name, with nothing between them",
            LayerRun.Error(Generics.Lexer, Generics.NameThenGt, "a >")?.ToString()
        );

    [Fact]
    public void ARejectionOfParsersWithNoNameUsesTheGeneralMessage() =>
        Assert.Equal(
            "error[parser] at 0..3: these items must touch, with nothing between them",
            LayerRun.Error(Generics.Lexer, Generics.Anything, "a b")?.ToString()
        );

    [Theory]
    [InlineData(1)]
    [InlineData(1024)]
    public void ARejectionDropsTheFailuresThePairRecordedAndSurvivesTheRelease(int segmentSize) =>
        Assert.Equal(
            "error[parser] at 0..8: these items must touch, with nothing between them",
            LayerRun.Error(Generics.Lexer, Generics.NameThenShifts, "a >>>>>>", segmentSize)?.ToString()
        );

    [Fact]
    public void AShiftIsTwoGtTokensThatTouch() =>
        Assert.Equal((true, "(a >> b)", 4L), LayerRun.Run(Generics.Lexer, Generics.Expr, "a >> b"));

    [Fact]
    public void InMemoryCharactersAlwaysTouch() =>
        Assert.Equal(('a', 'b'), Assert.IsType<Parsed<(char, char)>>(Adjacent(Char('a'), Char('b')).Parse("ab").Value).Value);

    [Fact]
    public void InMemoryTokensAlwaysTouch() =>
        Assert.Equal(("ab", "cd"), Assert.IsType<Parsed<(string, string)>>(Words.Pair.Parse(_twoWords).Value).Value);

    [Theory]
    [InlineData("x")]
    [InlineData("ac")]
    public void ItFailsAsSeqDoesWhenAPartFails(string text)
    {
        Assert.Equal(Execution.Outcome(Seq(Char('a'), Char('b')), text), Execution.Outcome(Adjacent(Char('a'), Char('b')), text));
        Assert.Equal(Execution.Error(Seq(Char('a'), Char('b')), text), Execution.Error(Adjacent(Char('a'), Char('b')), text));
    }

    [Fact]
    public void NestedTypeArgumentsCloseWithSingleGtTokens() =>
        Assert.Equal((true, "List[List[int]]", 7L), LayerRun.Run(Generics.Lexer, Generics.Type, "List<List<int>>"));

    [Fact]
    public void ShiftBindsTighterThanAComparison() =>
        Assert.Equal((true, "((a >> b) > c)", 6L), LayerRun.Run(Generics.Lexer, Generics.Expr, "a >> b > c"));

    [Fact]
    public void TwoGtTokensWithInputBetweenThemAreNotAShift()
    {
        Assert.False(LayerRun.Run(Generics.Lexer, Generics.Expr, "a > > b").Ok);
        Assert.Equal("error[parser] at 4..5: unexpected '>'; expected name", LayerRun.Error(Generics.Lexer, Generics.Expr, "a > > b")?.ToString());
    }

    [Fact]
    public void WithoutAtomicAComparisonFailsCommittedInsideTheShift()
    {
        var (ok, _, position) = LayerRun.Run(Generics.Lexer, Generics.Committed, "a > b");

        Assert.False(ok);
        Assert.Equal(2, position);
        Assert.Equal("error[parser] at 4..5: unexpected name; expected '>'", LayerRun.Error(Generics.Lexer, Generics.Committed, "a > b")?.ToString());
    }
}
