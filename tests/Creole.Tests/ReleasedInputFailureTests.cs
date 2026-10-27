using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

/// <summary>
/// Errors over a stream buffer that released the items they name, in the middle of the parse or after it, against the same errors over in-memory input.
/// </summary>
/// <remarks>
/// Each grammar takes a <see cref="Releaser"/>, which releases the buffer where it stands: below the cursor, or below a fixed position where a pin would hold the horizon.
/// Segments hold one item, so a release frees everything below the item before the horizon, and spans are read from the buffer, so a span read from a released item throws.
/// </remarks>
public class ReleasedInputFailureTests
{
    private class Releaser(StreamBuffer<char>? buffer, FailureLog<char>? failures, long? below) : IScanner<char, Unit>
    {
        private readonly StreamBuffer<char>? _buffer = buffer;
        private readonly long? _below = below;
        private readonly FailureLog<char>? _failures = failures;

        public Parser<char, Unit> Here => Custom(this);

        public ScannerInfo Info { get; } = new(Nullable: true);

        public long Released { get; private set; }

        public void Release(long position)
        {
            if (_buffer is not null)
            {
                _buffer.Release(_below ?? position, _failures);
                Released = Math.Max(Released, _buffer.Start);
            }
        }

        public bool Scan(ref Cursor<char> cursor, out Unit value)
        {
            Release(cursor.Position);
            value = default;
            return true;
        }
    }

    private class MovingScanner(Releaser release, int distance) : IScanner<char, Unit>
    {
        private readonly int _distance = distance;
        private readonly Releaser _release = release;

        public ScannerInfo Info { get; } = new(Nullable: false, Name: "token");

        public bool Scan(ref Cursor<char> cursor, out Unit value)
        {
            cursor.Advance(_distance);
            _release.Release(cursor.Position);
            value = default;
            return false;
        }
    }

    private static ParseError At(long start, long end, Unexpected unexpected, Expectation[] expected, ContextLine[]? context = null) =>
        new(new SourceSpan(SourceId.Unnamed, start, end), "parser", unexpected, expected, context ?? []);

    private static ParseError? Released<T>(Func<Releaser, Parser<char, T>> grammar, string text, long? below = null)
    {
        var expected = Execution.Error(grammar(new Releaser(null, null, null)), text);
        var buffer = new StreamBuffer<char>(new ArrayUpstream<char>(text.ToCharArray()), segmentSize: 1);
        var log = new FailureLog<char>();
        var releaser = new Releaser(buffer, log, below);
        var plan = new PlanBuilder(LoweringOptions.None).Lower(grammar(releaser));
        var cursor = new Cursor<char>(buffer) { Failures = log };
        T value = default!;

        _ = plan.TryParse(ref cursor, ref value);
        Assert.True(releaser.Released > 0, "The grammar released nothing while it ran.");
        buffer.Release(cursor.Position, log);

        var actual = log.ToError(in cursor, "parser", buffer.SpanOf);

        Assert.Equal(expected, actual);
        return actual;
    }

    [Fact]
    public void ARejectedFilterNamesItsStartAndEnd()
    {
        Assert.Equal(
            At(0, 3, new UnexpectedItem("'a'"), []),
            Released(release => Letter.SkipAtLeastOnce().Then(release.Here).Where(static _ => false), "abc1")
        );

        Assert.Equal(
            At(0, 5, new CustomMessage("too long"), []),
            Released(release => Letter.SkipAtLeastOnce().Then(release.Here).Then(Digit.SkipMany()).Where(static _ => false, "too long"), "ab123;")
        );
    }

    [Fact]
    public void AChainedOperatorNamesItsItem()
    {
        Parser<char, int> Comparison(Releaser release) => Expression.Build(Letter.Select(static c => (int)c), op =>
        [
            [op.InfixN(Satisfy(static c => c == '<').Then(Char('=')).Before(release.Here), static (l, _, r) => l <= r ? 1 : 0)],
        ]);

        Assert.Equal(
            At(4, 6, new CustomMessage("'<' cannot be chained; add parentheses"), []),
            Released(Comparison, "a<=b<=c")
        );
    }

    [Fact]
    public void AContextLineKeepsTheSpanOfItsStart() =>
        Assert.Equal(
            At(3, 4, new UnexpectedItem("';'"), [new Label("letter"), new ExpectedItem("']'")], [new ContextLine("in list", new SourceSpan(SourceId.Unnamed, 0, 1))]),
            Released(release => Char('[').Then(Letter.SkipMany()).Then(release.Here).Then(Char(']')).Context("in list"), "[ab;")
        );

    [Fact]
    public void ACopyTakenBeforeALookaheadSavesTheFailureSurvivesItsRestore() =>
        Assert.Equal(
            At(0, 1, new UnexpectedItem("'a'"), [new ExpectedItem("'x'")]),
            Released(release => Char('x').Optional().Then(String("abc")).Then(release.Here).Then(Lookahead(Char('d').Then(Char('y').Optional()))), "abcd")
        );

    [Fact]
    public void ACustomScannerThatFailsAfterMovingNamesItsStart() =>
        Assert.Equal(
            At(0, 1, new UnexpectedItem("'a'"), [new Label("token")]),
            Released(release => Custom(new MovingScanner(release, 2)), "abc")
        );

    [Fact]
    public void AFailureAtAnotherPositionDoesNotTakeAHeldStartsCopy() =>
        Assert.Equal(
            At(3, 4, new UnexpectedItem("'1'"), [new Label("letter"), new ExpectedItem("';'")]),
            Released(release => Letter.SkipAtLeastOnce().Then(release.Here).Then(Char(';')).Where(static _ => true), "abc1")
        );

    [Fact]
    public void AFailureAtAnInnerHeldStartTakesThatStartsCopy() =>
        Assert.Equal(
            At(1, 2, new UnexpectedItem("'b'"), [new Label("token")]),
            Released(release => Letter.Then(Custom(new MovingScanner(release, 3))).Where(static _ => true), "abcdef")
        );

    [Fact]
    public void AFailureAnAtomicRewoundPastNamesItsItemAfterAFilterRestoresIt() =>
        Assert.Equal(
            At(2, 3, new UnexpectedItem("'c'"), [new ExpectedItem("'x'")]),
            Released(release => Atomic(Char('a').Then(Char('b')).Then(Char('x'))).Ignore().Or(Letter.SkipAtLeastOnce().Then(release.Here).Where(static _ => false)), "abcd!")
        );

    [Fact]
    public void AFailureALookaheadRestoresNamesItsItem() =>
        Assert.Equal(
            At(0, 1, new UnexpectedItem("'a'"), [new ExpectedItem("'x'")]),
            Released(release => Char('x').Optional().Then(String("ab")).Then(Lookahead(Char('c').Then(Char('y').Optional()).Then(release.Here))), "abcd", below: 2)
        );

    [Fact]
    public void AFailureALookaheadRestoresKeepsTheSpanOfTheValueItRejected() =>
        Assert.Equal(
            At(0, 3, new UnexpectedItem("'a'"), []),
            Released(release => Atomic(Letter.SkipAtLeastOnce().Where(static _ => false)).Optional().Then(String("abc")).Then(Char(';')).Then(Lookahead(release.Here.Then(Char('y').Optional()))), "abc;")
        );

    [Fact]
    public void AFurtherFailureDropsTheCopyOfTheOneItReplaces() =>
        Assert.Equal(
            At(4, 5, new UnexpectedItem("'e'"), [new ExpectedItem("'q'")]),
            Released(release => Char('x').Optional().Then(String("abcd")).Then(release.Here).Then(Char('q')), "abcde")
        );

    [Fact]
    public void AWiderRejectionAtTheSamePositionDropsTheEndSpanItReplaces() =>
        Assert.Equal(
            At(0, 5, new UnexpectedItem("'a'"), []),
            Released(release => Atomic(Letter.SkipAtLeastOnce().Where(static _ => false)).Optional().Then(Letter.SkipAtLeastOnce().Then(Digit.SkipMany()).Then(release.Here).Where(static _ => false)), "abc12;")
        );

    [Fact]
    public void ARejectionAnAtomicRewoundPastKeepsTheSpanOfTheValueItRejected() =>
        Assert.Equal(
            At(0, 3, new UnexpectedItem("'a'"), []),
            Released(release => Atomic(Letter.SkipAtLeastOnce().Where(static _ => false)).Or(String("abc12").Then(release.Here)), "abc12;")
        );

    [Fact]
    public void TheFurthestFailureNamesItsItemOnceTheCursorHasPassedIt() =>
        Assert.Equal(
            At(0, 1, new UnexpectedItem("'a'"), [new ExpectedItem("'x'")]),
            Released(release => Char('x').Optional().Then(String("abc")).Then(release.Here), "abc")
        );
}
