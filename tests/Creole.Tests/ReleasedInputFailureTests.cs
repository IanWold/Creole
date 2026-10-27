using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed class ReleaseScanner(StreamBuffer<char>? buffer, FailureLog<char>? failures, long? below) : IScanner<char, Unit>
{
    private readonly StreamBuffer<char>? _buffer = buffer;
    private readonly long? _below = below;
    private readonly FailureLog<char>? _failures = failures;

    public ScannerInfo Info { get; } = new(Nullable: true);

    public long Released { get; private set; }

    public bool Scan(ref Cursor<char> cursor, out Unit value)
    {
        if (_buffer is not null)
        {
            _buffer.Release(_below ?? cursor.Position, _failures);
            Released = Math.Max(Released, _buffer.Start);
        }

        value = default;
        return true;
    }
}

/// <summary>
/// Errors over a stream buffer that released the items they name, in the middle of the parse or after it, against the same errors over in-memory input.
/// </summary>
/// <remarks>
/// Each grammar takes a parser that releases the buffer where it stands: below the cursor, or below a fixed position where a pin would hold the horizon.
/// Segments hold one item, so a release frees everything below the horizon, and spans are read from the buffer, so a span read from a released item throws.
/// </remarks>
public class ReleasedInputFailureTests
{
    private static ParseError At(long start, long end, Unexpected unexpected, Expectation[] expected, ContextLine[]? context = null) =>
        new(new SourceSpan(SourceId.Unnamed, start, end), "parser", unexpected, expected, context ?? []);

    private static ParseError? Released<T>(Func<Parser<char, Unit>, Parser<char, T>> grammar, string text, long? below = null)
    {
        var expected = Execution.Error(grammar(Custom(new ReleaseScanner(null, null, null))), text);
        var buffer = new StreamBuffer<char>(new ArrayUpstream<char>(text.ToCharArray()), segmentSize: 1);
        var log = new FailureLog<char>();
        var releaser = new ReleaseScanner(buffer, log, below);
        var plan = new PlanBuilder(LoweringOptions.None).Lower(grammar(Custom(releaser)));
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
            Released(release => Letter.SkipAtLeastOnce().Then(release).Where(static _ => false), "abc1")
        );

        Assert.Equal(
            At(0, 5, new CustomMessage("too long"), []),
            Released(release => Letter.SkipAtLeastOnce().Then(release).Then(Digit.SkipMany()).Where(static _ => false, "too long"), "ab123;")
        );
    }

    [Fact]
    public void AChainedOperatorNamesItsItem()
    {
        Parser<char, int> Comparison(Parser<char, Unit> release) => Expression.Build(Letter.Select(static c => (int)c), op =>
        [
            [op.InfixN(Satisfy(static c => c == '<').Before(release), static (l, _, r) => l < r ? 1 : 0)],
        ]);

        Assert.Equal(
            At(3, 4, new CustomMessage("'<' cannot be chained; add parentheses"), []),
            Released(Comparison, "a<b<c")
        );
    }

    [Fact]
    public void AContextLineKeepsTheSpanOfItsStart() =>
        Assert.Equal(
            At(3, 4, new UnexpectedItem("';'"), [new Label("letter"), new ExpectedItem("']'")], [new ContextLine("in list", new SourceSpan(SourceId.Unnamed, 0, 1))]),
            Released(release => Char('[').Then(release).Then(Letter.SkipMany()).Then(Char(']')).Context("in list"), "[ab;")
        );

    [Fact]
    public void AFailureAnAtomicRewoundPastNamesItsItemAfterAFilterRestoresIt() =>
        Assert.Equal(
            At(2, 3, new UnexpectedItem("'c'"), [new ExpectedItem("'x'")]),
            Released(release => Atomic(Char('a').Then(Char('b')).Then(Char('x'))).Ignore().Or(Letter.SkipAtLeastOnce().Then(release).Where(static _ => false)), "abcd!")
        );

    [Fact]
    public void AFailureALookaheadRestoresNamesItsItem() =>
        Assert.Equal(
            At(0, 1, new UnexpectedItem("'a'"), [new ExpectedItem("'x'")]),
            Released(release => Char('x').Optional().Then(String("ab")).Then(Lookahead(Char('c').Then(Char('y').Optional()).Then(release))), "abcd", below: 2)
        );

    [Fact]
    public void TheFurthestFailureNamesItsItemOnceTheCursorHasPassedIt() =>
        Assert.Equal(
            At(0, 1, new UnexpectedItem("'a'"), [new ExpectedItem("'x'")]),
            Released(release => Char('x').Optional().Then(String("abc")).Then(release), "abc")
        );
}
