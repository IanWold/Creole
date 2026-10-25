using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file static class Json
{
    public static readonly Parser<char, char> Value =
        Char('1').Or(Char('[').Then(Char(']')).Context("in array")).Named("value");

    public static readonly Parser<char, char> Member =
        Char('{').Then(Value).Context("in object");

    public static readonly Parser<char, Unit> Objects =
        Char('{').Then(Char('a').Many()).Then(Char('}')).Context("in object").SkipMany();
}

public class ControlFailureTests
{
    private static ParseError At(long start, long end, Unexpected unexpected, Expectation[] expected, params ContextLine[] context) =>
        new(new SourceSpan(SourceId.Unnamed, start, end), "parser", unexpected, expected, context);

    private static ContextLine Line(string text, long start) =>
        new(text, new SourceSpan(SourceId.Unnamed, start, start + 1));

    [Fact]
    public void NamedReplacesWhatItsParserExpected() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("'x'"), [new Label("sign")]), Execution.Error(Char('+').Or(Char('-')).Named("sign"), "x"));

    [Fact]
    public void NamedKeepsWhatWasExpectedThereBeforeItRan() =>
        Assert.Equal(
            At(0, 1, new UnexpectedItem("'x'"), [new ExpectedItem("','"), new Label("letter")]),
            Execution.Error(Char(',').Or(Char('a').Or(Char('b')).Named("letter")), "x")
        );

    [Fact]
    public void NamedLabelsAnExpectationAlreadyRecordedThere()
    {
        var a = Char('a');

        Assert.Equal(At(0, 1, new UnexpectedItem("'x'"), [new ExpectedItem("'a'"), new Label("A")]), Execution.Error(a.Or(a.Named("A")), "x"));
    }

    [Fact]
    public void NamedLabelsAFailureThatExpectedNothing() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("'z'"), [new Label("hex digit")]), Execution.Error(Satisfy(char.IsAsciiHexDigit).Named("hex digit"), "z"));

    [Fact]
    public void NamedLabelsAParserThatSucceedsWithoutConsuming() =>
        Assert.Equal(
            At(0, 1, new UnexpectedItem("'x'"), [new Label("digits"), new ExpectedItem("';'")]),
            Execution.Error(Digit.Many().Named("digits").Then(Char(';')), "x")
        );

    [Fact]
    public void NamedChangesNothingOnceItsParserConsumed()
    {
        Assert.Equal(At(1, 2, new UnexpectedItem("'x'"), [new ExpectedItem("'b'")]), Execution.Error(Char('a').Then(Char('b')).Named("ab"), "ax"));
        Assert.Equal(At(1, 2, new UnexpectedItem("'x'"), [new ExpectedItem("'b'")]), Execution.Error(Atomic(Char('a').Then(Char('b'))).Named("ab"), "ax"));
    }

    [Fact]
    public void NamedKeepsAMessage() =>
        Assert.Equal(At(0, 1, new CustomMessage("bad"), [new Label("thing")]), Execution.Error(Fail<char>("bad").Named("thing"), "x"));

    [Fact]
    public void AnOuterLabelReplacesAnInnerOne() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("'x'"), [new Label("outer")]), Execution.Error(Char('a').Named("inner").Named("outer"), "x"));

    [Fact]
    public void NamedHasNothingToLabelAfterALookaheadPutTheFailureBack()
    {
        Assert.Null(Execution.Error(Lookahead(Char('a')).Named("a"), "a"));
        Assert.Equal(At(0, 1, new UnexpectedItem("'a'"), [new Label("not a")]), Execution.Error(Not(Char('a')).Named("not a"), "a"));
    }

    [Fact]
    public void ALookaheadThatSucceedsLeavesNoTrace() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("'1'"), [new ExpectedItem("'x'")]), Execution.Error(Lookahead(Digit.SkipMany()).Then(Char('x')), "12y"));

    [Fact]
    public void NotKeepsWhatItRulesOutOutOfTheReport() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("'1'"), [new Label("letter")]), Execution.Error(Not(String("if")).Then(Letter), "1"));

    [Fact]
    public void ALongestThatMatchesLeavesNoTrace() =>
        Assert.Equal(
            At(2, 3, new UnexpectedItem("'x'"), [new ExpectedItem("';'")]),
            Execution.Error(Longest(Digit.SkipAtLeastOnce().As(1), Char('-').As(2)).Then(Char(';')), "12x")
        );

    [Fact]
    public void ALongestThatFailsReportsEveryAlternative() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("'c'"), [new ExpectedItem("'a'"), new ExpectedItem("'b'")]), Execution.Error(Longest(Char('a'), Char('b')), "c"));

    [Fact]
    public void AContextLineAppliesPastWhereItsConstructStarted()
    {
        var obj = Char('{').Then(Char('}')).Context("in object");

        Assert.Equal(At(1, 2, new UnexpectedItem("'x'"), [new ExpectedItem("'}'")], Line("in object", 0)), Execution.Error(obj, "{x"));
        Assert.Equal(At(0, 1, new UnexpectedItem("'x'"), [new ExpectedItem("'{'")]), Execution.Error(obj, "x"));
    }

    [Fact]
    public void ContextLinesAreInnermostFirst() =>
        Assert.Equal(
            At(2, 3, new UnexpectedItem("'x'"), [new ExpectedItem("'}'")], Line("in object", 1), Line("in array", 0)),
            Execution.Error(Char('[').Then(Char('{').Then(Char('}')).Context("in object")).Context("in array"), "[{x")
        );

    [Fact]
    public void TheFirstFailureAtAPositionKeepsItsContext() =>
        Assert.Equal(
            At(1, 2, new UnexpectedItem("'d'"), [new ExpectedItem("'b'"), new ExpectedItem("'c'")], Line("in x", 0)),
            Execution.Error(Atomic(Char('a').Then(Char('b'))).Context("in x").Or(Char('a').Then(Char('c')).Context("in y")), "ad")
        );

    [Fact]
    public void ALabelKeepsTheContextOutsideItsParser() =>
        Assert.Equal(At(1, 2, new UnexpectedItem("'x'"), [new Label("value")], Line("in object", 0)), Execution.Error(Json.Member, "{x"));

    [Fact]
    public void ContextFramesStayFewOverALongInput()
    {
        var input = string.Concat(Enumerable.Repeat("{a}", 500)) + "{ax";
        var plan = new PlanBuilder(LoweringOptions.None).Lower(Json.Objects);
        var log = new FailureLog<char>();
        var cursor = new Cursor<char>(input) { Failures = log };
        var value = default(Unit);

        Assert.False(plan.TryParse(ref cursor, ref value));
        Assert.InRange(log.FrameCount, 0, 8);
        Assert.Equal(
            At(1502, 1503, new UnexpectedItem("'x'"), [new ExpectedItem("'a'"), new ExpectedItem("'}'")], Line("in object", 1500)),
            log.ToError(in cursor, "parser", static (start, end) => new SourceSpan(SourceId.Unnamed, start, end))
        );
    }

    [Theory]
    [InlineData("JSON")]
    [InlineData("C#")]
    public void ACompiledLexerRecordsWhatTheInterpreterRecords(string lexer)
    {
        var rules = lexer == "JSON" ? LexerSamples.Json() : LexerSamples.CSharp();
        var text = (lexer == "JSON" ? LexerSamples.JsonText : LexerSamples.CSharpText) + "§";
        var parser = LexerSamples.Longest([.. rules.Select(static rule => rule.Text())]).Then(Char('§'));
        var interpreted = new PlanBuilder(LoweringOptions.None).Lower(parser);
        var compiled = new PlanBuilder(LoweringOptions.Dfa).Lower(parser);

        for (var position = 0; position < text.Length; position++)
        {
            Assert.Equal(ErrorAt(interpreted, text, position), ErrorAt(compiled, text, position));
        }
    }

    [Fact]
    public void ACompiledLongestRecordsWhatTheInterpreterRecords() =>
        Gen.Select(Gen.Int[2, 3].SelectMany(static count => RegularShapes.Regular(4).Array[count]), Gen.Char["abc"].Array[0, 6]).Sample(
            static (shapes, input) =>
            {
                var longest = LexerSamples.Longest([.. shapes.Select(static (shape, index) => shape.Parser.As(index))]);
                var parser = longest.Then(Char('b').Named("b"));
                var text = new string(input);

                return new PlanBuilder(LoweringOptions.Dfa).Lower(longest) is DfaLongestPlan<int>
                    && Equals(Execution.Error(parser, text), Execution.Error(parser, text, LoweringOptions.Dfa));
            },
            iter: 2_000,
            print: static t => $"{string.Join(" | ", t.Item1.Select(static shape => shape.Text))} on \"{new string(t.Item2)}\""
        );

    private static ParseError? ErrorAt<T>(Plan<char, T> plan, string text, int position)
    {
        var log = new FailureLog<char>();
        var cursor = new Cursor<char>(text) { Failures = log };
        T value = default!;

        cursor.Reset(position);
        _ = plan.TryParse(ref cursor, ref value);

        return log.ToError(in cursor, "lexer", static (start, end) => new SourceSpan(SourceId.Unnamed, start, end));
    }
}
