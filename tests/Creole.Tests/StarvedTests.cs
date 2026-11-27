using System.Buffers;
using System.Text;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file class CopyScanner : IScanner<char, string>
{
    public ScannerInfo Info { get; } = new(Nullable: false, Name: "terminated word");

    public bool Scan(ref Cursor<char> cursor, out string value)
    {
        var copy = cursor;
        var letters = new StringBuilder();

        while (copy.TryRead(out var c))
        {
            if (c == ';')
            {
                cursor = copy;
                value = letters.ToString();
                return true;
            }

            letters.Append(c);
        }

        value = "";
        return false;
    }
}

file class AvailableScanner : IScanner<char, Unit>
{
    private static readonly SearchValues<char> _letters = SearchValues.Create("abcdefghijklmnopqrstuvwxyz");

    public ScannerInfo Info { get; } = new(Nullable: false, Name: "letters before a non-letter");

    public bool Scan(ref Cursor<char> cursor, out Unit value)
    {
        var copy = cursor;
        value = default;

        while (copy.Available is { IsEmpty: false } available)
        {
            var length = available.IndexOfAnyExcept(_letters);

            if (length >= 0)
            {
                copy.Advance(length);

                var matched = copy.Position > cursor.Position;

                if (matched)
                {
                    cursor = copy;
                }

                return matched;
            }

            copy.Advance(available.Length);
        }

        return false;
    }
}

file class CountingScanner : IScanner<char, char>
{
    public int Calls { get; private set; }

    public ScannerInfo Info { get; } = new(Nullable: false, Name: "probe");

    public bool Scan(ref Cursor<char> cursor, out char value)
    {
        Calls++;

        return cursor.TryRead(out value);
    }
}

file class LettersScanner : IScanner<char, Unit>
{
    private static readonly SearchValues<char> _letters = SearchValues.Create("abcdefghijklmnopqrstuvwxyz");

    public ScannerInfo Info { get; } = new(Nullable: false, Name: "letters");

    public bool Scan(ref Cursor<char> cursor, out Unit value)
    {
        var start = cursor.Position;
        value = default;

        while (cursor.Available is { IsEmpty: false } available)
        {
            var length = available.IndexOfAnyExcept(_letters);

            if (length >= 0)
            {
                cursor.Advance(length);
                break;
            }

            cursor.Advance(available.Length);
        }

        return cursor.Position > start;
    }
}

file class ReadAheadScanner : IScanner<char, char>
{
    public ScannerInfo Info { get; } = new(Nullable: false, Name: "read-ahead");

    public bool Scan(ref Cursor<char> cursor, out char value)
    {
        var copy = cursor;

        while (copy.TryRead(out _))
        {
        }

        return cursor.TryRead(out value);
    }
}

/// <summary>
/// Streamed input that has run out of what it has received: reads there starve the attempt, and every layer above passes that on, rather than taking it for the end of the input.
/// No combinator acts on a starved attempt as it would on a failure: none tries another alternative, ends a loop, rewinds or inverts it.
/// </summary>
public class StarvedTests
{
    private const int SegmentSize = 4;

    private readonly record struct Observed(bool Ok, string Value, long Position, bool Cut, bool Starved);

    /// <summary>
    /// A grammar that reaches the probe it is given only by acting on a failure, or ending a loop, where <paramref name="Received"/> runs out.
    /// </summary>
    private record ProbeCase(string Name, Func<Parser<char, char>, Parser<char, Unit>> Grammar, string Received);

    private static readonly LoweringOptions[] _loweringOptions = [LoweringOptions.None, LoweringOptions.All & ~LoweringOptions.Dfa, LoweringOptions.All];

    private static readonly ProbeCase[] _probeCases =
    [
        new("Or", static probe => String("ab").Ignore().Or(probe.Ignore()), "a"),
        new("Or, dispatched at the end of the input received", static probe => Char('a').Ignore().Or(probe.Ignore()), ""),
        new("Optional", static probe => String("ab").Optional().Then(probe).Ignore(), "a"),
        new("Many", static probe => String("ab").Many().Then(probe).Ignore(), "aba"),
        new("AtLeastOnce", static probe => String("ab").AtLeastOnce().Then(probe).Ignore(), "aba"),
        new("Repeat with a range", static probe => String("ab").Repeat(0, 3).Then(probe).Ignore(), "aba"),
        new("SkipMany", static probe => String("ab").SkipMany().Then(probe).Ignore(), "aba"),
        new("SkipMany of a character class", static probe => Letter.SkipMany().Then(probe).Ignore(), "ab"),
        new("SepBy, at a separator", static probe => Char('x').SepBy(String(", ")).Then(probe).Ignore(), "x,"),
        new("SepEndBy, at an item after a separator", static probe => Char('x').SepEndBy(Char(',')).Then(probe).Ignore(), "x,"),
        new("Until, at a terminator", static probe => probe.Until(String(";;")).Ignore(), ";"),
        new("Expression.Build, at a prefix operator", static probe => Expression.Build(Digit, static op => [[op.Prefix(String("--"), static (_, x) => x)]]).Ignore().Or(probe.Ignore()), "-"),
        new("Expression.Build, at an infix operator", static probe => Expression.Build(Digit, static op => [[op.InfixL(String("**"), static (l, _, _) => l)]]).Then(probe).Ignore(), "1*"),
        new("Longest", static probe => Longest(String("<="), probe.Select(static c => c.ToString())).Ignore(), "<"),
        new("Longest, compiled", static probe => Longest(String("<"), String("<=")).Then(probe).Ignore(), "<"),
        new("Atomic", static probe => Atomic(Char('a').Then(Char('b'))).Ignore().Or(probe.Ignore()), "a"),
        new("Lookahead", static probe => Lookahead(Custom(new LettersScanner())).Then(probe).Ignore(), "ab"),
        new("Not", static probe => Not(String("ab")).Then(probe).Ignore(), "a"),
    ];

    public static TheoryData<string> ProbeCaseNames => [.. _probeCases.Select(static @case => @case.Name)];

    private static Observed Observe<T>(Plan<char, T> plan, string received, bool starves)
    {
        var buffer = new StreamBuffer<char>(new ArrayUpstream<char>(received.ToCharArray(), starves: starves), SegmentSize);
        var cursor = new Cursor<char>(buffer) { Failures = new FailureLog<char>() };
        T value = default!;

        var ok = plan.TryParse(ref cursor, ref value);

        return new Observed(
            ok,
            ok
                ? value?.ToString() ?? ""
                : "",
            cursor.Position,
            cursor.Cut,
            cursor.Starved
        );
    }

    private static StreamBuffer<T> Open<TIn, T>(Layer<TIn, T> layer, TIn[] input, bool starves = true) =>
        new(layer.Open(new StreamBuffer<TIn>(new ArrayUpstream<TIn>(input, starves: starves), SegmentSize), new PipelineRun(ParseOptions.Default, recordFailures: true)), SegmentSize);

    private static string? PrefixDifference(ControlShape shape, string[] inputs)
    {
        var parser = ControlTable.Build(shape);

        foreach (var options in _loweringOptions)
        {
            var plan = new PlanBuilder(options, dispatchThreshold: 1).Lower(parser);
            var starving = inputs.ToDictionary(static input => input, input => Observe(plan, input, starves: true));

            foreach (var input in inputs)
            {
                var whole = Observe(plan, input, starves: false);

                for (var received = 0; received <= input.Length; received++)
                {
                    var prefix = starving[input[..received]];

                    if (!prefix.Starved && prefix != whole)
                    {
                        return $"{ControlModel.Describe(shape)} with {options} on \"{input[..received]}\" of \"{input}\": {prefix}, but the whole input gives {whole}";
                    }
                }
            }
        }

        return null;
    }

    private static (Observed Observed, int Calls) Probe(string name, LoweringOptions options, bool starves)
    {
        var probeCase = _probeCases.Single(candidate => candidate.Name == name);
        var scanner = new CountingScanner();
        var observed = Run(probeCase.Grammar(Custom(scanner)), probeCase.Received, options, starves);

        return (observed, scanner.Calls);
    }

    private static (string Items, bool Starved) Read<T>(StreamBuffer<T> buffer)
    {
        var items = new List<T>();

        for (var position = 0L; buffer.TryGetSegment(position, horizon: 0, out _, out _); position++)
        {
            items.Add(buffer.ItemAt(position));
        }

        return (string.Join(" ", items), buffer.Starved);
    }

    private static Observed Run<T>(Parser<char, T> parser, string received, LoweringOptions options = LoweringOptions.All, bool starves = true) =>
        Observe(new PlanBuilder(options, dispatchThreshold: 1).Lower(parser), received, starves);

    [Fact]
    public void ABufferWhoseUpstreamStarvesIsStarvedAndNotEnded()
    {
        var buffer = new StreamBuffer<char>(new ArrayUpstream<char>("ab".ToCharArray(), starves: true), SegmentSize);

        Assert.True(buffer.TryGetSegment(1, horizon: 0, out _, out _));
        Assert.False(buffer.Starved);
        Assert.False(buffer.TryGetSegment(2, horizon: 0, out _, out _));
        Assert.True(buffer.Starved);
    }

    [Fact]
    public void ABufferWhoseUpstreamEndsIsNotStarved()
    {
        var buffer = new StreamBuffer<char>(new ArrayUpstream<char>("ab".ToCharArray()), SegmentSize);

        Assert.False(buffer.TryGetSegment(2, horizon: 0, out _, out _));
        Assert.False(buffer.Starved);
    }

    [Fact]
    public void ReadingPastTheInputReceivedSoFarStarves() =>
        Assert.Equal(new Observed(false, "", 1, false, true), Run(Char('a').Then(Char('b')), "a"));

    [Fact]
    public void ParsingWithinTheInputReceivedSoFarDoesNotStarve() =>
        Assert.Equal(new Observed(true, "a", 1, false, false), Run(Char('a'), "ab"));

    [Fact]
    public void InputHeldWholeInMemoryNeverStarves()
    {
        var cursor = new Cursor<char>("a");

        cursor.TryRead(out _);

        Assert.True(cursor.IsAtEnd);
        Assert.False(cursor.Starved);
    }

    [Fact]
    public void AScannerThatReadsACopyToTheEndAndDiscardsItLeavesTheParseStarved() =>
        Assert.Equal(new Observed(false, "", 0, false, true), Run(Custom(new CopyScanner()), "abc"));

    [Fact]
    public void AScannerThatReadsOnlyAvailableToTheEndLeavesTheParseStarved() =>
        Assert.Equal(new Observed(false, "", 0, false, true), Run(Custom(new AvailableScanner()), "abcdef"));

    [Fact]
    public void EndFailsWhereTheInputReceivedSoFarRunsOut() =>
        Assert.Equal(new Observed(false, "", 1, false, true), Run(Char('a').Then(End), "a"));

    [Fact]
    public void EndSucceedsAtTheTrueEndOfTheInput() =>
        Assert.Equal(new Observed(true, "a", 1, false, false), Run(Char('a').Before(End), "a", starves: false));

    [Fact]
    public void ALongestWhoseWinnerEndsWhereTheInputReceivedSoFarEndsDoesNotStarve() =>
        Assert.All(
            _loweringOptions,
            static options => Assert.Equal(new Observed(true, "abcdefgh", 8, false, false), Run(Longest(String("abcdefgh"), String("abc")), "abcdefgh", options))
        );

    [Fact]
    public void ALexerProducesNothingFromAStepThatStarves() =>
        Assert.Equal(
            ("", true),
            Read(Open(Layer.Tokens(Longest(String("<"), String("<="))), "<".ToCharArray()))
        );

    [Fact]
    public void ALexerProducesTheItemsBeforeTheStepThatStarves() =>
        Assert.Equal(
            ("ab cd", true),
            Read(Open(Layer.Tokens(Letter.AtLeastOnce().Text(), Char(' ').SkipMany()), "ab cd ".ToCharArray()))
        );

    [Fact]
    public void ALexerItemThatFailsWhereItsInputStarvedReportsNoFailure() =>
        Assert.Equal(("", true), Read(Open(Layer.Tokens(String("ab")), "a".ToCharArray())));

    [Fact]
    public void ALexerSkipThatFailsWhereItsInputStarvedReportsNoFailure() =>
        Assert.Equal(
            ("a", true),
            Read(Open(Layer.Tokens(Letter, String("--").Select(static _ => default(Unit))), "--a-".ToCharArray()))
        );

    [Fact]
    public void ALexerItemThatFailsAtTheTrueEndOfItsInputStillFails() =>
        Assert.Throws<LayerFailedException>(() => Read(Open(Layer.Tokens(String("ab")), "a".ToCharArray(), starves: false)));

    [Fact]
    public void ATransducerDoesNotRunAtEndWhereItsInputStarves() =>
        Assert.Equal(
            ("a b", true),
            Read(Open(Layer.Transduce(0, Any.Select(static c => Emit.One(c)), atEnd: static _ => Emit.One('$')), "ab".ToCharArray()))
        );

    [Fact]
    public void ATransducerRunsAtEndAtTheTrueEndOfItsInput() =>
        Assert.Equal(
            ("a b $", false),
            Read(Open(Layer.Transduce(0, Any.Select(static c => Emit.One(c)), atEnd: static _ => Emit.One('$')), "ab".ToCharArray(), starves: false))
        );

    [Fact]
    public void ATransducerStepThatFailsWhereItsInputStarvedReportsNoFailure() =>
        Assert.Equal(("", true), Read(Open(Layer.Transduce(0, String("ab").Select(static s => Emit.One(s[0]))), "a".ToCharArray())));

    [Fact]
    public void TheDecoderStarvesWhereItsInputStarvesInsideASequence() =>
        Assert.Equal(("a", true), Read(Open(Layer.Utf8(), new byte[] { 0x61, 0xE2, 0x82 })));

    [Fact]
    public void TheDecoderReportsASequenceCutOffAtTheTrueEndOfItsInput() =>
        Assert.Throws<LayerFailedException>(() => Read(Open(Layer.Utf8(), new byte[] { 0x61, 0xE2, 0x82 }, starves: false)));

    [Fact]
    public void SelectStarvesWhereItsInputStarves() =>
        Assert.Equal(("A B", true), Read(Open(Layer.Select<char, char>(char.ToUpperInvariant), "ab".ToCharArray())));

    [Fact]
    public void WhereStarvesWhereItsInputStarves() =>
        Assert.Equal(("a", true), Read(Open(Layer.Where<char>(static c => c != ' '), "a ".ToCharArray())));

    [Fact]
    public void StarvationAtTheRootReachesEveryLayerAbove() =>
        Assert.Equal(
            ("ab", true),
            Read(Open(Layer.Utf8().Then(Layer.Tokens(Letter.AtLeastOnce().Text(), Char(' ').SkipMany())), "ab cd"u8.ToArray()))
        );

    [Fact]
    public void EveryControlShapeToDepthThreeThatDoesNotStarveOnAPrefixDoesWhatItDoesOnTheWholeInput()
    {
        var inputs = ControlModel.AllInputs(3).ToArray();

        var difference = ControlModel.AllShapes(3)
            .Select(shape => PrefixDifference(shape, inputs))
            .FirstOrDefault(static difference => difference is not null);

        Assert.Null(difference);
    }

    [Fact]
    public void AnAtomicItemThatStarvedIsNotRewound() =>
        Assert.All(
            _loweringOptions,
            static options => Assert.Equal(new Observed(false, "", 1, false, true), Run(Atomic(Char('a').Then(Char('b'))), "a", options))
        );

    [Theory]
    [MemberData(nameof(ProbeCaseNames))]
    public void TheProbeRunsAtTheTrueEndOfTheInput(string name) =>
        Assert.All(_loweringOptions, options => Assert.NotEqual(0, Probe(name, options, starves: false).Calls));

    [Theory]
    [MemberData(nameof(ProbeCaseNames))]
    public void NoCombinatorTriesAnotherAlternativeOrEndsALoopAfterAStarvedAttempt(string name) =>
        Assert.All(
            _loweringOptions,
            options =>
            {
                var (observed, calls) = Probe(name, options, starves: true);

                Assert.Equal((false, true, 0), (observed.Ok, observed.Starved, calls));
            }
        );

    [Fact]
    public void ALongestDoesNotRunItsWinnerAgainAfterAnEarlierReadStarved() =>
        Assert.All(
            _loweringOptions,
            static options =>
            {
                var observed = Run(Custom(new ReadAheadScanner()).Then(Longest(Char('a').Then(Char('b').Optional()), Char('x').Then(Char('y').Optional()))), "?ac", options);

                Assert.Equal((false, true), (observed.Ok, observed.Starved));
            }
        );
}
