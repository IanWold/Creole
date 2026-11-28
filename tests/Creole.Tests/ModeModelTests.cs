using CsCheck;

namespace Creole.Tests;

/// <summary>
/// One way of running a parser over <see cref="ModeShapes.Lexer"/>'s tokens.
/// </summary>
/// <param name="Name">What the run varies, for a failure's message.</param>
/// <param name="SegmentSize">The items to a segment in both buffers, whose pools overwrite each array given back, so a read of a released or discarded item shows.</param>
/// <param name="RecordFailures">Whether the parser has a failure log, as a parse's second run does.</param>
/// <param name="Builder">Makes the builder that lowers the parser, or null for the plan a pipeline runs.</param>
file record Configuration(string Name, int SegmentSize, bool RecordFailures, Func<PlanBuilder>? Builder);

/// <summary>
/// Runs a grammar over <see cref="ModeShapes.Lexer"/> in each <see cref="Configuration"/>, and through <c>Pipeline.Parse</c>, and compares every run with <see cref="ModeModel"/>.
/// </summary>
file static class ModeRuns
{
    private static readonly Configuration[] _configurations =
    [
        new("in one segment", 1024, RecordFailures: true, Builder: null),
        new("in one-item segments", 1, RecordFailures: true, Builder: null),
        new("in one-item segments without a failure log", 1, RecordFailures: false, Builder: null),
        new("without rewrites or light marks", 1, RecordFailures: true, static () => new PlanBuilder(LoweringOptions.None)),
        new("with every choice dispatched", 1, RecordFailures: true, static () => new PlanBuilder(LoweringOptions.All, dispatchThreshold: 1)),
    ];

    /// <summary>
    /// The outcome of <paramref name="parser"/> on <paramref name="input"/>, run as a pipeline's parser runs, and how many entries its mode log dropped.
    /// </summary>
    private static (Outcome Outcome, long Dropped) Run(Parser<RunToken, string> parser, string input, Configuration configuration)
    {
        var run = new PipelineRun(ParseOptions.Default, configuration.RecordFailures);
        var characters = new StreamBuffer<char>(new ArrayUpstream<char>(input.ToCharArray()), configuration.SegmentSize, new OverwritingPool<char>('\0'));

        using var items = new StreamBuffer<RunToken>(ModeShapes.Lexer.Open(characters, run), configuration.SegmentSize, new OverwritingPool<RunToken>(default));

        var writer = run.Writer("parser");
        var cursor = new Cursor<RunToken>(items)
        {
            Failures = configuration.RecordFailures
                ? new FailureLog<RunToken>()
                : null,
            Modes = writer,
        };
        var plan = configuration.Builder?.Invoke().Lower(parser) ?? PlanCache.Get(parser);
        var value = "";

        try
        {
            Outcome outcome = plan.TryParse(ref cursor, ref value)
                ? new Outcome.Ok(value, (int)cursor.Position)
                : cursor.Position > 0
                    ? new Outcome.FailConsumed()
                    : new Outcome.FailEmpty();

            return (outcome, writer.Length - writer.Count);
        }
        catch (GrammarException)
        {
            return (new Outcome.Throws(), 0);
        }
    }

    /// <summary>
    /// Null if every run of <paramref name="shape"/> on <paramref name="input"/> agrees with the model, and otherwise the first difference. Adds one to <paramref name="dropping"/> if a run's mode log dropped entries.
    /// </summary>
    public static string? Difference(ModeShape shape, string input, ref int dropping)
    {
        var (expected, atEnd) = ModeModel.Run(shape, input);
        var parser = ModeShapes.Build(shape);
        var dropped = false;

        foreach (var configuration in _configurations)
        {
            var (actual, entries) = Run(parser, input, configuration);

            dropped |= entries > 0;

            if (actual != expected)
            {
                return $"{ModeShapes.Describe(shape)} on \"{input}\" {configuration.Name}: model {expected}, pipeline {actual}";
            }
        }

        if (dropped)
        {
            Interlocked.Increment(ref dropping);
        }

        var parsed = Pipeline.From(ModeShapes.Lexer).Then(parser.Before(Grammar<RunToken>.End)).Parse(input).Value;

        var value = expected is Outcome.Ok ok && atEnd
            ? ok.Value
            : null;

        return (parsed as Parsed<string>)?.Value == value
            ? null
            : $"{ModeShapes.Describe(shape)} on \"{input}\" through Parse: model {value ?? "a failure"}, pipeline {parsed}";
    }
}

/// <summary>
/// The pipeline against <see cref="ModeModel"/>, which lexes every token from scratch in the mode the grammar left on top, so that a token read in an abandoned mode, or a change a rewind failed to undo, shows.
/// </summary>
/// <remarks>
/// Each grammar runs over buffers in several configurations, and through <c>Pipeline.Parse</c> to the end of its text, and each run must agree with the model.
/// The inputs are short, so a log fills its first array of four entries in some runs and drops the entries below the cursor and the floor.
/// A rewind to before a dropped entry throws, so a drop that removed an entry a rewind needs fails the run.
/// </remarks>
public class ModeModelTests
{
    private const int Iterations = 20_000;

    private static readonly Gen<ModeShape> _shapes = Shapes(4);

    /// <summary>
    /// A repetition whose item tries a shape and a token inside an <c>Atomic</c>, and otherwise reads any token, so it runs to the end of the text and its log fills and drops entries while marks are open.
    /// </summary>
    private static readonly Gen<ModeShape> _loops = _shapes.Select(static shape =>
        (ModeShape)new ModeShape.Many(
            new ModeShape.Or(
                new ModeShape.Atomic(new ModeShape.Seq(shape, new ModeShape.Tok(RunKind.A))),
                new ModeShape.Or(new ModeShape.Tok(RunKind.A), new ModeShape.Tok(RunKind.B))
            )
        )
    );

    private static Gen<ModeShape> Shapes(int depth)
    {
        var leaf = Gen.OneOf<ModeShape>(
            new ModeShape.Tok(RunKind.A),
            new ModeShape.Tok(RunKind.B),
            new ModeShape.Set(RunMode.Single),
            new ModeShape.Set(RunMode.Runs)
        );

        if (depth <= 1)
        {
            return leaf;
        }

        var smaller = Shapes(depth - 1);

        var unary = Gen.Select(Gen.Int[0, 6], Gen.Enum<RunMode>(), smaller, static (which, mode, shape) => which switch
        {
            0 => (ModeShape)new ModeShape.Atomic(shape),
            1 => new ModeShape.Lookahead(shape),
            2 => new ModeShape.Not(shape),
            3 => new ModeShape.Optional(shape),
            4 => new ModeShape.Many(
                ModeShapes.Nullable(shape)
                    ? new ModeShape.Seq(shape, new ModeShape.Tok(RunKind.A))
                    : shape
            ),
            5 => new ModeShape.Push(mode, shape),
            _ => new ModeShape.Atomic(new ModeShape.Seq(new ModeShape.Set(mode), shape)),
        });

        var binary = Gen.Select(Gen.Int[0, 2], smaller, smaller, static (which, first, second) => which switch
        {
            0 => (ModeShape)new ModeShape.Seq(first, second),
            1 => new ModeShape.Or(first, second),
            _ => new ModeShape.Longest(first, second),
        });

        return Gen.Frequency((1, leaf), (2, unary), (3, binary));
    }

    [Fact]
    public void TheModelKeepsTheModeTheWinnerOfALongestLeft() =>
        Assert.Equal(
            (new Outcome.Ok("[a][a][b]", 3), true),
            ModeModel.Run(
                new ModeShape.Seq(
                    new ModeShape.Longest(
                        new ModeShape.Seq(new ModeShape.Tok(RunKind.A), new ModeShape.Tok(RunKind.A)),
                        new ModeShape.Seq(new ModeShape.Set(RunMode.Runs), new ModeShape.Tok(RunKind.A))
                    ),
                    new ModeShape.Tok(RunKind.B)
                ),
                "aab"
            )
        );

    [Fact]
    public void TheModelPopsWhatAPushPushed() =>
        Assert.Equal(
            (new Outcome.Ok("[aa][b][b]", 3), true),
            ModeModel.Run(
                new ModeShape.Seq(
                    new ModeShape.Push(RunMode.Runs, new ModeShape.Tok(RunKind.A)),
                    new ModeShape.Seq(new ModeShape.Tok(RunKind.B), new ModeShape.Tok(RunKind.B))
                ),
                "aabb"
            )
        );

    [Theory]
    [InlineData("aab", "[aa]", 1)]
    [InlineData("aba", "[a]", 1)]
    public void TheModelReadsATokenInTheModeOnTop(string input, string value, int consumed) =>
        Assert.Equal(
            (new Outcome.Ok(value, consumed), false),
            ModeModel.Run(new ModeShape.Seq(new ModeShape.Set(RunMode.Runs), new ModeShape.Tok(RunKind.A)), input)
        );

    [Fact]
    public void TheModelUndoesTheChangesOfAnAlternativeThatFailedWithoutConsuming() =>
        Assert.Equal(
            (new Outcome.Ok("[a][a][b]", 3), true),
            ModeModel.Run(
                new ModeShape.Seq(
                    new ModeShape.Or(new ModeShape.Seq(new ModeShape.Set(RunMode.Runs), new ModeShape.Tok(RunKind.B)), new ModeShape.Tok(RunKind.A)),
                    new ModeShape.Seq(new ModeShape.Tok(RunKind.A), new ModeShape.Tok(RunKind.B))
                ),
                "aab"
            )
        );

    [Fact]
    public void ThePipelineAgreesWithAFromScratchLexOnRandomGrammars()
    {
        var dropping = 0;

        Gen.Select(_shapes, Gen.Char["ab"].Array[0, 10]).Sample(
            (shape, input) => ModeRuns.Difference(shape, new string(input), ref dropping) is null,
            iter: Iterations,
            print: sample => ModeRuns.Difference(sample.Item1, new string(sample.Item2), ref dropping) ?? ""
        );
    }

    [Fact]
    public void ThePipelineAgreesWithAFromScratchLexWhileTheModeLogDropsEntries()
    {
        var dropping = 0;

        Gen.Select(_loops, Gen.Char["ab"].Array[10, 40]).Sample(
            (shape, input) => ModeRuns.Difference(shape, new string(input), ref dropping) is null,
            iter: Iterations / 4,
            print: sample => ModeRuns.Difference(sample.Item1, new string(sample.Item2), ref dropping) ?? ""
        );

        Assert.InRange(dropping, Iterations / 16, Iterations / 4);
    }
}
