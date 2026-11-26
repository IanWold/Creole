namespace Creole.Tests;

/// <summary>
/// Lowering a layer's furthest failure at a position, as a mode change there does: what the failure forgets, what it keeps, and what a later failure there supplies.
/// </summary>
/// <remarks>
/// The log runs over <see cref="Text"/>, so position <c>i</c> holds the <c>i</c>-th letter of the alphabet and has the span <c>[i, i + 1)</c>.
/// </remarks>
public class FailureLoweringTests
{
    private const string Text = "abcdefghij";

    private static readonly Expected _label = Expected.Labelled("value");
    private static readonly Expected _x = Expected.Labelled("x");
    private static readonly Expected _y = Expected.Labelled("y");

    private static ParseError? Error(FailureLog<char> log)
    {
        var cursor = Cursor<char>.FromMemory(Text.AsMemory());

        return log.ToError(in cursor, "parser", SpanOf);
    }

    private static ParseError ErrorAt(long position, Unexpected unexpected, Expectation[] expected, ContextLine[]? context = null) =>
        new(SpanOf(position, position + 1), "parser", unexpected, expected, context ?? []);

    private static SourceSpan SpanOf(long start, long end) =>
        new(SourceId.Unnamed, start, end);

    [Fact]
    public void AChainedOperatorWhoseSpanReachesPastTheIndexLosesItsEndAndItsMessage()
    {
        var log = new FailureLog<char>();

        log.Reject(2, 6, null, ChainMessage.Unnamed);
        log.LowerAt(4);

        Assert.Equal(ErrorAt(2, new UnexpectedItem("'c'"), []), Error(log));
    }

    [Fact]
    public void AFailureGivenBackWhereAFailureWasLoweredSuppliesItsContext()
    {
        var log = new FailureLog<char>();

        log.Enter("in call", 1);
        log.Record(3, _x);
        log.Leave();
        log.RaiseFromFurthestFailure(3);
        log.Record(5, _y);
        log.LowerAt(3);
        log.Withdraw(0);

        Assert.Equal(ErrorAt(3, new UnexpectedItem("'d'"), [new Label("x")], [new ContextLine("in call", SpanOf(1, 2))]), Error(log));
    }

    [Fact]
    public void AFailurePastTheIndexBecomesAFailureThereThatExpectsNothingAndHasNoMessageOrContext()
    {
        var log = new FailureLog<char>();

        log.Enter("in list", 1);
        log.Record(5, _x);
        log.Record(5, "bad item");
        log.Leave();
        log.LowerAt(3);

        Assert.Equal(ErrorAt(3, new UnexpectedItem("'d'"), []), Error(log));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void AFailureAtOrBeforeTheIndexIsUnchanged(long position)
    {
        var log = new FailureLog<char>();

        log.Enter("in list", 1);
        log.Record(position, _x);
        log.Record(position, "bad item");
        log.Leave();

        var before = Error(log);

        log.LowerAt(3);

        Assert.Equal(before, Error(log));
    }

    [Fact]
    public void ALoweredFailureGivenBackLeavesItsContextToTheNextFailureThere()
    {
        var log = new FailureLog<char>();

        log.Record(5, _x);
        log.LowerAt(3);
        log.RaiseFromFurthestFailure(3);
        log.Withdraw(0);
        log.Enter("in call", 1);
        log.Record(3, _y);
        log.Leave();

        Assert.Equal(ErrorAt(3, new UnexpectedItem("'d'"), [new Label("y")], [new ContextLine("in call", SpanOf(1, 2))]), Error(log));
    }

    [Fact]
    public void AMarkKeepsALoweredFailureWaitingForItsContext()
    {
        var log = new FailureLog<char>();

        log.Record(5, _x);
        log.LowerAt(3);

        var mark = log.Save(modeLength: 0);

        log.Record(7, _y);
        log.Restore(in mark);
        log.Enter("in call", 1);
        log.Record(3, _y);
        log.Leave();

        Assert.Equal(ErrorAt(3, new UnexpectedItem("'d'"), [new Label("y")], [new ContextLine("in call", SpanOf(1, 2))]), Error(log));
    }

    [Fact]
    public void AMarkKeepsWhetherARejectionGaveTheMessage()
    {
        var log = new FailureLog<char>();

        log.Reject(2, 6, "too big", null);

        var mark = log.Save(modeLength: 0);

        log.Record(7, _x);
        log.Restore(in mark);
        log.LowerAt(4);

        Assert.Equal(ErrorAt(2, new UnexpectedItem("'c'"), []), Error(log));
    }

    [Fact]
    public void AMessageRecordedBeforeARejectionStaysWhenTheRejectionLosesItsEnd()
    {
        var log = new FailureLog<char>();

        log.Record(2, "custom");
        log.Reject(2, 6, "too big", null);
        log.LowerAt(4);

        Assert.Equal(ErrorAt(2, new CustomMessage("custom"), []), Error(log));
    }

    [Fact]
    public void ARejectionThatEndsAtTheIndexIsUnchanged()
    {
        var log = new FailureLog<char>();

        log.Reject(2, 4, "too big", null);

        var before = Error(log);

        log.LowerAt(4);

        Assert.Equal(before, Error(log));
    }

    [Fact]
    public void ARejectionWhoseSpanReachesPastTheIndexLosesItsEndAndItsMessage()
    {
        var log = new FailureLog<char>();

        log.Record(2, _x);
        log.Reject(2, 6, "too big", null);
        log.LowerAt(4);

        Assert.Equal(ErrorAt(2, new UnexpectedItem("'c'"), [new Label("x")]), Error(log));
    }

    [Fact]
    public void AWithdrawnDiagnosticGivesBackWhetherARejectionGaveTheMessage()
    {
        var log = new FailureLog<char>();

        log.Reject(2, 6, "too big", null);
        log.RaiseFromFurthestFailure(6);
        log.Withdraw(0);
        log.LowerAt(4);

        Assert.Equal(ErrorAt(2, new UnexpectedItem("'c'"), []), Error(log));
    }

    [Fact]
    public void LoweringAnEmptyLogLeavesItEmpty()
    {
        var log = new FailureLog<char>();

        log.LowerAt(3);

        Assert.Null(Error(log));
    }

    [Fact]
    public void LoweringRecordsNothingSoNamedDoesNotRelabelOnItsOwn()
    {
        var log = new FailureLog<char>();

        log.Record(5, _x);

        var note = log.Note();

        log.LowerAt(3);
        log.Relabel(in note, 3, _label);

        Assert.Equal(ErrorAt(3, new UnexpectedItem("'d'"), []), Error(log));
    }

    [Fact]
    public void NamedRelabelsAFailureRecordedAfterTheLoweringWithoutTheExpectationsItReplaced()
    {
        var log = new FailureLog<char>();

        log.Record(3, _x);

        var note = log.Note();

        log.Record(5, _y);
        log.LowerAt(3);
        log.Record(3, _y);
        log.Relabel(in note, 3, _label);

        Assert.Equal(ErrorAt(3, new UnexpectedItem("'d'"), [new Label("value")]), Error(log));
    }

    [Fact]
    public void TheNextFailureAtTheIndexSuppliesTheContext()
    {
        var log = new FailureLog<char>();

        log.Record(5, _x);
        log.LowerAt(3);
        log.Enter("in call", 1);
        log.Record(3, _y);
        log.Leave();

        Assert.Equal(ErrorAt(3, new UnexpectedItem("'d'"), [new Label("y")], [new ContextLine("in call", SpanOf(1, 2))]), Error(log));
    }
}
