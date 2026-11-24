using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file record Name(string Text, SourceSpan Span);

/// <summary>
/// The code sample in docs/concepts/spans.md § Spans in grammars. Keep the two in step.
/// </summary>
file class Names : CharGrammar
{
    public static readonly Parser<char, Name> Identifier =
        Letter.AtLeastOnce().Text().WithSpan((text, span) => new Name(text, span));
}

file class Words : Grammar<string>
{
    public static readonly Layer<char, string> Lexer = Layer.Tokens(Letter.AtLeastOnce().Text(), skip: Whitespace.SkipMany());

    public static readonly Parser<string, Spanned<IReadOnlyList<string>>> All = Any.Many().Spanned();

    public static readonly Parser<string, Spanned<int>> Count = Any.Many().Select(static words => words.Count).Spanned();

    public static readonly Parser<string, IReadOnlyList<Spanned<string>>> Each = Any.Spanned().Many();

    public static readonly Parser<string, Spanned<int>> Empty = Return(1).Spanned();

    public static readonly Parser<string, Spanned<IReadOnlyList<Spanned<string>>>> Nested = Any.Spanned().Many().Spanned();

    public static readonly Parser<string, (SourceSpan, string, SourceSpan, string, SourceSpan)> Positions = Seq(Position, Any, Position, Any, Position);

    public static readonly Parser<string, SourceSpan> Start = Position;
}

public class SpanExecutionTests
{
    private static readonly string[] _twoWords = ["ab", "cd"];

    private static SourceSpan At(long start, long end) =>
        new(SourceId.Unnamed, start, end);

    private static T Value<T>(Parser<char, T> parser, string text) =>
        Assert.IsType<Parsed<T>>(parser.Parse(text).Value).Value;

    [Fact]
    public void OverALexerPositionIsWhereTheNextTokenStartsOrWhereTheLastEnds()
    {
        var (ok, (first, _, second, _, end), _) = LayerRun.Run(Words.Lexer, Words.Positions, "  ab  cd  ");

        Assert.True(ok);
        Assert.Equal(At(2, 2), first);
        Assert.Equal(At(6, 6), second);
        Assert.Equal(At(8, 8), end);
    }

    [Fact]
    public void OverALexerSpannedLeavesOutTheInputTheLayerSkipped()
    {
        Assert.Equal([new("ab", At(2, 4)), new Spanned<string>("cd", At(6, 8))], LayerRun.Run(Words.Lexer, Words.Each, "  ab  cd  ").Value);
        Assert.Equal(At(2, 8), LayerRun.Run(Words.Lexer, Words.All, "  ab  cd  ").Value.Span);
    }

    [Fact]
    public void OverALexerSpannedOfAParserThatConsumesNothingIsZeroWidthAtTheNextToken() =>
        Assert.Equal(new Spanned<int>(1, At(2, 2)), LayerRun.Run(Words.Lexer, Words.Empty, "  ab").Value);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void OverALexerWithNoTokensPositionIsAtTheStart(string text) =>
        Assert.Equal((true, At(0, 0), 0L), LayerRun.Run(Words.Lexer, Words.Start, text));

    [Fact]
    public void OverTokensInMemorySpansCountTokens() =>
        Assert.Equal(new Spanned<int>(2, At(0, 2)), Assert.IsType<Parsed<Spanned<int>>>(Words.Count.Parse(_twoWords).Value).Value);

    [Fact]
    public void PositionIsZeroWidthWhereTheNextItemStartsOrAtTheEnd()
    {
        Assert.Equal((At(0, 0), 'a', At(1, 1)), Value(Seq(Position, Char('a'), Position), "ab"));
        Assert.Equal(At(1, 1), Value(Char('a').Then(Position), "a"));
        Assert.Equal(At(0, 0), Value(Position, ""));
    }

    [Fact]
    public void SpannedCoversTheItemsItsParserConsumed() =>
        Assert.Equal(new Spanned<string>("ab", At(1, 3)), Value(Char('x').Then(String("ab").Spanned()), "xabc"));

    [Theory]
    [InlineData("b")]
    [InlineData("ac")]
    public void SpannedFailsAsItsParserDoes(string text)
    {
        var parser = Seq(Char('a'), Char('b'));

        Assert.Equal(Execution.Outcome(parser, text), Execution.Outcome(parser.Spanned(), text));
        Assert.Equal(Execution.Error(parser, text), Execution.Error(parser.Spanned(), text));
    }

    [Theory]
    [InlineData("xz")]
    [InlineData("x")]
    public void SpannedOfAParserThatConsumesNothingIsZeroWidthWhereTheNextItemStarts(string text) =>
        Assert.Equal(At(1, 1), Value(Char('x').Then(Char('y').Optional().Spanned()), text).Span);

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(1024, false)]
    [InlineData(1024, true)]
    public void SpannedReadsTheSameSpansWhenTheBufferReleasesItsInputWhileItRuns(int segmentSize, bool recordFailures)
    {
        var tokens = LayerRun.Open(Words.Lexer, " ab cd  ef g ", out _, segmentSize);
        var cursor = new Cursor<string>(tokens) { Failures = recordFailures ? new FailureLog<string>() : null };
        Spanned<IReadOnlyList<Spanned<string>>> words = default;

        Assert.True(PlanCache.Get(Words.Nested).TryParse(ref cursor, ref words));
        Assert.Equal(At(1, 12), words.Span);
        Assert.Equal([new("ab", At(1, 3)), new("cd", At(4, 6)), new("ef", At(8, 10)), new Spanned<string>("g", At(11, 12))], words.Value);
        Assert.Equal(segmentSize == 1, tokens.Start > 0);
    }

    [Fact]
    public void TheDocsSampleGivesANameItsSpan() =>
        Assert.Equal(new Name("abc", At(0, 3)), Value(Names.Identifier, "abc;"));

    [Fact]
    public void WithSpanBuildsFromTheValueAndItsSpan() =>
        Assert.Equal("abc@2..5", Value(Whitespace.SkipMany().Then(Letter.AtLeastOnce().Text().WithSpan(static (name, span) => $"{name}@{span.Start}..{span.End}")), "  abc"));
}
