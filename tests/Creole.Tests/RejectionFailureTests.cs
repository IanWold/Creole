using System.Globalization;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed class ReleasingSource(string text) : ICursorSource<char>
{
    private long _released;

    public void Release(long below) =>
        _released = below;

    public bool TryGetSegment(long position, out ReadOnlyMemory<char> segment, out long segmentStart)
    {
        if (position < _released || position >= text.Length)
        {
            segment = default;
            segmentStart = 0;
            return false;
        }

        segmentStart = position;
        segment = text.AsMemory((int)position, 1);
        return true;
    }
}

file static class Grammars
{
    public static readonly Parser<char, int> Number =
        Digit.SkipAtLeastOnce().Text().Select(static text => int.Parse(text, CultureInfo.InvariantCulture));

    public static readonly Parser<char, string> Word =
        Letter.SkipAtLeastOnce().Text();

    public static readonly Parser<char, int> Comparison = Expression.Build(Letter.Select(static c => (int)c), op =>
    [
        [op.InfixL(Char('+'), static (l, _, r) => l + r)],
        [op.InfixN(Char('<'), static (l, _, r) => l < r ? 1 : 0)],
    ]);

    public static readonly Parser<char, int> Range = Expression.Build(Letter.Select(static c => (int)c), op =>
    [
        [op.InfixN(String("<="), static (l, _, r) => l <= r ? 1 : 0)],
    ]);

    public static readonly Parser<char, int> NamedOperator = Expression.Build(Letter.Select(static c => (int)c), op =>
    [
        [op.InfixN(Char('<').Named("less-than"), static (l, _, r) => l < r ? 1 : 0)],
    ]);
}

public class RejectionFailureTests
{
    private static ParseError At(long start, long end, Unexpected unexpected, params Expectation[] expected) =>
        new(new SourceSpan(SourceId.Unnamed, start, end), "parser", unexpected, expected, []);

    private static ParseError? ErrorAfterRelease<T>(Parser<char, T> parser, string text)
    {
        var plan = new PlanBuilder(LoweringOptions.All).Lower(parser);
        var source = new ReleasingSource(text);
        var log = new FailureLog<char>();
        var cursor = new Cursor<char>(source) { Failures = log };
        T value = default!;

        _ = plan.TryParse(ref cursor, ref value);
        source.Release(cursor.Position);

        return log.ToError(in cursor, "parser", static (start, end) => new SourceSpan(SourceId.Unnamed, start, end));
    }

    private static bool ReadsBehindAfterRelease(string text)
    {
        var source = new ReleasingSource(text);
        var cursor = new Cursor<char>(source);

        cursor.Advance(text.Length);
        source.Release(text.Length);

        try
        {
            _ = cursor.ItemAt(0);
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return true;
        }
    }

    [Fact]
    public void ARejectedValueIsReportedWhereItStartsOverTheWholeValue() =>
        Assert.Equal(At(0, 3, new UnexpectedItem("'3'")), Execution.Error(Grammars.Number.Where(static n => n <= 255), "300;"));

    [Fact]
    public void ARejectionCarriesItsMessage() =>
        Assert.Equal(At(0, 3, new CustomMessage("number too large")), Execution.Error(Grammars.Number.Where(static n => n <= 255, "number too large"), "300;"));

    [Fact]
    public void ARejectionMergesWithWhatWasExpectedThereBeforeTheFilterRan() =>
        Assert.Equal(
            At(0, 2, new UnexpectedItem("'i'"), new ExpectedItem("'#'")),
            Execution.Error(Char('#').Select(static c => c.ToString()).Or(Grammars.Word.Where(static word => word != "if")), "if(")
        );

    [Fact]
    public void AFurtherFailureRecordedBeforeTheFilterStillWins() =>
        Assert.Equal(
            At(2, 3, new UnexpectedItem("'b'"), new ExpectedItem("'a'")),
            Execution.Error(Atomic(Char('a').Then(Char('a')).Then(Char('a'))).Or(Return('x')).Then(Grammars.Word.Where(static word => word.Length > 5)), "aab")
        );

    [Fact]
    public void UnderAtomicALabelNamesARejection()
    {
        var identifier = Atomic(Grammars.Word.Where(static word => word != "if"));

        Assert.Equal(At(0, 2, new UnexpectedItem("'i'"), new Label("identifier")), Execution.Error(identifier.Named("identifier"), "if"));
        Assert.Equal(
            At(0, 2, new CustomMessage("keyword is reserved"), new Label("identifier")),
            Execution.Error(Atomic(Grammars.Word.Where(static word => word != "if", "keyword is reserved")).Named("identifier"), "if")
        );
    }

    [Fact]
    public void AnAcceptedValueKeepsWhatItsParserRecorded() =>
        Assert.Equal(
            At(2, 3, new UnexpectedItem("'!'"), new Label("letter"), new ExpectedItem("';'")),
            Execution.Error(Grammars.Word.Where(static _ => true).Then(Char(';')), "ab!")
        );

    [Fact]
    public void ARejectedEmptyValueIsReportedLikeAnyFailureThere() =>
        Assert.Equal(At(0, 1, new UnexpectedItem("'x'")), Execution.Error(Return("").Where(static text => text.Length > 0), "x"));

    [Fact]
    public void AChainedNonAssociativeOperatorIsReportedAtItself()
    {
        Assert.Equal(
            At(3, 4, new CustomMessage("'<' cannot be chained; add parentheses"), new ExpectedItem("'+'")),
            Execution.Error(Grammars.Comparison, "a<b<c")
        );

        Assert.Equal(
            At(4, 6, new CustomMessage("\"<=\" cannot be chained; add parentheses")),
            Execution.Error(Grammars.Range, "a<=b<=c")
        );

        Assert.Equal(
            At(3, 4, new CustomMessage("less-than cannot be chained; add parentheses")),
            Execution.Error(Grammars.NamedOperator, "a<b<c")
        );
    }

    [Fact]
    public void BuildingTheErrorsReadsNothingBehindTheCursor()
    {
        Assert.Equal(Execution.Error(Grammars.Number.Where(static n => n <= 255), "300;"), ErrorAfterRelease(Grammars.Number.Where(static n => n <= 255), "300;"));
        Assert.Equal(Execution.Error(Grammars.Comparison, "a<b<c"), ErrorAfterRelease(Grammars.Comparison, "a<b<c"));
    }

    [Fact]
    public void ReadingBehindAReleasedCursorThrows() =>
        Assert.True(ReadsBehindAfterRelease("ab"));
}
