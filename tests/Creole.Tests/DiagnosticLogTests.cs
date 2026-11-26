namespace Creole.Tests;

/// <summary>
/// The diagnostics a layer's failure log holds: raising them, withdrawing them to a count or from a step, and the failure a withdrawn diagnostic gives back.
/// </summary>
/// <remarks>
/// The log runs over <see cref="Text"/>, so position <c>i</c> holds the <c>i</c>-th letter of the alphabet and has the span <c>[i, i + 1)</c>.
/// </remarks>
public class DiagnosticLogTests
{
    private const string Text = "abcdefghij";

    private static readonly Expected _label = Expected.Labelled("value");
    private static readonly Expected _x = Expected.Labelled("x");
    private static readonly Expected _y = Expected.Labelled("y");

    private static List<ParseError> Diagnostics(FailureLog<char> log)
    {
        var cursor = Cursor<char>.FromMemory(Text.AsMemory());

        return log.ToDiagnostics(in cursor, "parser", SpanOf);
    }

    private static ParseError? Error(FailureLog<char> log)
    {
        var cursor = Cursor<char>.FromMemory(Text.AsMemory());

        return log.ToError(in cursor, "parser", SpanOf);
    }

    private static ParseError ErrorAt(long position, Unexpected unexpected, Expectation[] expected, ContextLine[]? context = null) =>
        new(SpanOf(position, position + 1), "parser", unexpected, expected, context ?? []);

    private static IEnumerable<string> Messages(FailureLog<char> log) =>
        Diagnostics(log).Select(error => ((CustomMessage)error.Unexpected.Value!).Text);

    private static SourceSpan SpanOf(long start, long end) =>
        new(SourceId.Unnamed, start, end);

    [Fact]
    public void ADiagnosticRaisedWithAMessageReportsItWhereItWasRaisedInTheContextInForce()
    {
        var log = new FailureLog<char>();

        log.Enter("in list", 1);
        log.Raise(3, "missing item");
        log.Leave();

        Assert.Equal([ErrorAt(3, new CustomMessage("missing item"), [], [new ContextLine("in list", SpanOf(1, 2))])], Diagnostics(log));
        Assert.Null(Error(log));
    }

    [Fact]
    public void ADiagnosticFromTheFurthestFailureReportsItAndTakesItOutOfTheLog()
    {
        var log = new FailureLog<char>();

        log.Enter("in object", 2);
        log.Record(5, _x);
        log.Leave();

        var reported = Error(log);

        log.RaiseFromFurthestFailure(7);

        Assert.Equal([reported!], Diagnostics(log));
        Assert.Null(Error(log));
    }

    [Fact]
    public void WithNoFailureRecordedADiagnosticFromTheFurthestFailureReportsTheItemWhereItWasRaisedAndGivesNothingBack()
    {
        var log = new FailureLog<char>();

        log.RaiseFromFurthestFailure(4);

        Assert.Equal([ErrorAt(4, new UnexpectedItem("'e'"), [])], Diagnostics(log));

        log.Withdraw(0);

        Assert.Null(Error(log));
    }

    [Fact]
    public void WithdrawingToACountKeepsTheDiagnosticsRaisedBeforeIt()
    {
        var log = new FailureLog<char>();

        log.Raise(1, "first");

        var count = log.DiagnosticCount;

        log.Raise(2, "second");
        log.Raise(3, "third");
        log.Withdraw(count);

        Assert.Equal(["first"], Messages(log));
    }

    [Fact]
    public void AWithdrawnDiagnosticGivesItsFailureBackWithItsContext()
    {
        var log = new FailureLog<char>();

        log.Enter("in object", 2);
        log.Record(5, _x);
        log.Leave();

        var reported = Error(log);

        log.RaiseFromFurthestFailure(7);
        log.Record(3, _y);
        log.Withdraw(0);

        Assert.Equal(reported, Error(log));
        Assert.Empty(Diagnostics(log));
    }

    [Fact]
    public void AFailureGivenBackWhereTheFurthestFailureIsComesAfterWhatWasRecordedThereSinceAndKeepsItsContext()
    {
        var log = new FailureLog<char>();

        log.Enter("in item", 3);
        log.Record(4, _x);
        log.Leave();
        log.RaiseFromFurthestFailure(6);
        log.Enter("in call", 1);
        log.Record(4, _y);
        log.Leave();
        log.Withdraw(0);

        Assert.Equal(ErrorAt(4, new UnexpectedItem("'e'"), [new Label("y"), new Label("x")], [new ContextLine("in call", SpanOf(1, 2))]), Error(log));
    }

    [Fact]
    public void AFailureGivenBackBringsItsMessageWhereNoneWasRecordedSince()
    {
        var log = new FailureLog<char>();

        log.Record(4, "bad digit");
        log.RaiseFromFurthestFailure(6);
        log.Record(4, _y);
        log.Withdraw(0);

        Assert.Equal(ErrorAt(4, new CustomMessage("bad digit"), [new Label("y")]), Error(log));
    }

    [Fact]
    public void AFailureGivenBackBeforeTheFurthestFailureIsIgnored()
    {
        var log = new FailureLog<char>();

        log.Record(3, _x);
        log.RaiseFromFurthestFailure(6);
        log.Record(5, _y);
        log.Withdraw(0);

        Assert.Equal(ErrorAt(5, new UnexpectedItem("'f'"), [new Label("y")]), Error(log));
    }

    [Fact]
    public void WithdrawingGivesFailuresBackInTheOrderTheirDiagnosticsWereRaised()
    {
        var log = new FailureLog<char>();

        log.Record(4, _x);
        log.RaiseFromFurthestFailure(6);
        log.Record(4, _y);
        log.RaiseFromFurthestFailure(6);
        log.Withdraw(0);

        Assert.Equal(ErrorAt(4, new UnexpectedItem("'e'"), [new Label("x"), new Label("y")]), Error(log));
    }

    [Fact]
    public void TakingTheFurthestFailureLeavesTheSaveOfAnEnclosingConstruct()
    {
        var log = new FailureLog<char>();

        log.Record(2, _x);

        var save = log.Save(modeLength: 0);

        log.Record(5, _y);
        log.RaiseFromFurthestFailure(6);
        log.Record(1, _y);
        log.Restore(in save);

        Assert.Equal(ErrorAt(2, new UnexpectedItem("'c'"), [new Label("x")]), Error(log));
    }

    [Fact]
    public void NamedReplacesOnlyWhatWasRecordedSinceTheFailureWasTakenOutOfTheLog()
    {
        var log = new FailureLog<char>();

        log.Record(0, _x);

        var note = log.Note();

        log.RaiseFromFurthestFailure(0);
        log.Record(0, _y);
        log.Relabel(in note, 0, _label);

        Assert.Equal(ErrorAt(0, new UnexpectedItem("'a'"), [new Label("value")]), Error(log));
    }

    [Fact]
    public void NamedKeepsWhatWasRecordedBeforeItsNoteWhenARestorePutsBackTheFailureThatWasTaken()
    {
        var log = new FailureLog<char>();

        log.Record(0, _x);

        var note = log.Note();
        var save = log.Save(modeLength: 0);

        log.RaiseFromFurthestFailure(0);
        log.Restore(in save);
        log.Record(0, _y);
        log.Relabel(in note, 0, _label);

        Assert.Equal(ErrorAt(0, new UnexpectedItem("'a'"), [new Label("x"), new Label("value")]), Error(log));
    }

    [Fact]
    public void WithdrawingFromAStepWithdrawsTheDiagnosticsOfThatStepAndEveryLaterOneAndGivesNothingBack()
    {
        var log = new FailureLog<char>
        {
            StepStart = 0
        };

        log.Raise(1, "first");
        log.StepStart = 4;
        log.Record(5, _x);
        log.RaiseFromFurthestFailure(5);
        log.StepStart = 8;
        log.Raise(9, "third");
        log.WithdrawFromStep(4);

        Assert.Equal(["first"], Messages(log));
        Assert.Null(Error(log));
    }

    [Fact]
    public void ClearingForgetsTheDiagnosticsAndTheStep()
    {
        var log = new FailureLog<char>
        {
            StepStart = 4
        };

        log.Raise(5, "lost");
        log.Clear();

        Assert.Empty(Diagnostics(log));
        Assert.Equal(0, log.StepStart);
    }

    [Fact]
    public void ADiagnosticKeepsTheItemsAStreamBufferReleasesAndGivesThemBackWithItsFailure()
    {
        var buffer = new StreamBuffer<char>(new ArrayUpstream<char>(Text.ToCharArray()), segmentSize: 1);
        var log = new FailureLog<char>();
        var cursor = new Cursor<char>(buffer) { Failures = log };
        var floor = cursor.Pin();

        cursor.Advance(8);
        log.Enter("in object", 1);
        log.Reject(2, 4, null, null);
        log.Leave();

        var reported = log.ToError(in cursor, "parser", buffer.SpanOf);

        log.RaiseFromFurthestFailure(cursor.Position);
        cursor.Unpin(floor);
        buffer.Release(cursor.Position, log);

        Assert.True(buffer.Start > 4);
        Assert.Equal([reported!], log.ToDiagnostics(in cursor, "parser", buffer.SpanOf));

        log.Withdraw(0);

        Assert.Equal(reported, log.ToError(in cursor, "parser", buffer.SpanOf));
    }
}
