using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

/// <summary>
/// Diagnostics raised on paths a parse abandons are withdrawn, so each error is reported once (§ Recovery).
/// </summary>
/// <remarks>
/// The interpreter runs <see cref="ControlShape"/> grammars with <see cref="ControlShape.Raise"/> leaves, and its surviving diagnostics are compared with <see cref="ControlModel.Diagnose"/>.
/// Nothing in the library raises diagnostics yet, so the grammars use <see cref="RaiseNode"/> and <see cref="ReportNode{T}"/>, which lowering cannot see, and are lowered without <see cref="LoweringOptions.LightMarks"/>.
/// </remarks>
public class DiagnosticWithdrawalTests
{
    private const int Depth = 3;
    private const LoweringOptions FullMarks = LoweringOptions.All & ~LoweringOptions.LightMarks;
    private const int InputLength = 3;

    private static readonly Gen<ControlShape> _shapes = Shapes(5).Where(shape => !ControlModel.RepeatsNullable(shape));

    private static string? Difference(ControlShape shape, string input)
    {
        var numbered = Number(shape);
        var expected = ControlModel.InOrder.Diagnose(numbered, input);
        var actual = Run(numbered, input);

        return actual.Outcome == expected.Outcome && actual.Diagnostics.SequenceEqual(expected.Diagnostics)
            ? null
            : $"{ControlModel.Describe(numbered)} on \"{input}\": model {expected.Outcome} [{string.Join(", ", expected.Diagnostics)}], interpreter {actual.Outcome} [{string.Join(", ", actual.Diagnostics)}]";
    }

    /// <summary>
    /// Gives each <see cref="ControlShape.Raise"/> leaf its own number, in the order a walk from the left meets them.
    /// </summary>
    private static ControlShape Number(ControlShape shape)
    {
        var next = 0;

        ControlShape Renumber(ControlShape s) => s switch
        {
            ControlShape.Raise => new ControlShape.Raise(++next),
            ControlShape.AtLeastOnce t => new ControlShape.AtLeastOnce(Renumber(t.Item)),
            ControlShape.Atomic t => new ControlShape.Atomic(Renumber(t.Item)),
            ControlShape.Context t => new ControlShape.Context(Renumber(t.Item), t.Text),
            ControlShape.Lookahead t => new ControlShape.Lookahead(Renumber(t.Item)),
            ControlShape.Longest t => new ControlShape.Longest(Renumber(t.First), Renumber(t.Second)),
            ControlShape.Many t => new ControlShape.Many(Renumber(t.Item)),
            ControlShape.Named t => new ControlShape.Named(Renumber(t.Item), t.Label),
            ControlShape.Not t => new ControlShape.Not(Renumber(t.Item)),
            ControlShape.Optional t => new ControlShape.Optional(Renumber(t.Item)),
            ControlShape.Or t => new ControlShape.Or(Renumber(t.First), Renumber(t.Second)),
            ControlShape.SepBy t => new ControlShape.SepBy(Renumber(t.Item), Renumber(t.Separator)),
            ControlShape.Seq t => new ControlShape.Seq([.. t.Steps.Select(Renumber)]),
            ControlShape.Until t => new ControlShape.Until(Renumber(t.Item), Renumber(t.Terminator)),
            _ => s,
        };

        return Renumber(shape);
    }

    private static List<ParseError> Raised<T>(Parser<char, T> grammar, string input)
    {
        var log = new FailureLog<char>();
        var cursor = Cursor<char>.FromMemory(input.AsMemory()) with { Failures = log };
        var plan = new PlanBuilder(FullMarks).Lower(grammar);
        T value = default!;

        _ = plan.TryParse(ref cursor, ref value);

        return [.. log.ToDiagnostics(in cursor, "parser", SpanOf)];
    }

    private static (Outcome Outcome, IReadOnlyList<string> Diagnostics) Run(ControlShape shape, string input)
    {
        var log = new FailureLog<char>();
        var cursor = Cursor<char>.FromMemory(input.AsMemory()) with { Failures = log };
        Outcome outcome;

        try
        {
            var plan = new PlanBuilder(FullMarks, dispatchThreshold: 1).Lower(ControlTable.Build(shape));
            var value = "";
            var ok = plan.TryParse(ref cursor, ref value);

            outcome = ok ? new Outcome.Ok(value, (int)cursor.Position)
                : cursor.Position > 0 ? new Outcome.FailConsumed()
                : new Outcome.FailEmpty();
        }
        catch (GrammarException)
        {
            return (new Outcome.Throws(), []);
        }

        return (outcome, [.. log.ToDiagnostics(in cursor, "parser", SpanOf).Select(error => $"{((CustomMessage)error.Unexpected.Value!).Text} at {error.Span.Start}")]);
    }

    private static Gen<ControlShape> Shapes(int depth)
    {
        var leaf = Gen.OneOf<ControlShape>(new ControlShape.Chars("a"), new ControlShape.Chars("b"), new ControlShape.Chars("ab"), new ControlShape.Cut(), new ControlShape.Raise(0));

        if (depth <= 1)
        {
            return leaf;
        }

        var smaller = Shapes(depth - 1);

        var unary = Gen.Select(Gen.Int[0, 5], smaller, (kind, s) => kind switch
        {
            0 => (ControlShape)new ControlShape.Atomic(s),
            1 => new ControlShape.Lookahead(s),
            2 => new ControlShape.Not(s),
            3 => new ControlShape.Optional(s),
            4 => new ControlShape.Many(s),
            _ => new ControlShape.AtLeastOnce(s),
        });

        var binary = Gen.Select(Gen.Int[0, 4], smaller, smaller, (kind, first, second) => kind switch
        {
            0 => (ControlShape)new ControlShape.Seq(first, second),
            1 => new ControlShape.Or(first, second),
            2 => new ControlShape.Longest(first, second),
            3 => new ControlShape.SepBy(first, second),
            _ => new ControlShape.Until(first, second),
        });

        return Gen.Frequency((1, leaf), (2, unary), (3, binary));
    }

    private static SourceSpan SpanOf(long start, long end) =>
        new(SourceId.Unnamed, start, end);

    private static List<string> Survivors<T>(Parser<char, T> grammar, string input) =>
        [.. Raised(grammar, input).Select(error => ((CustomMessage)error.Unexpected.Value!).Text)];

    [Fact]
    public void NoDiagnosticSurvivesFromAnAbandonedPathInAnyShapeToDepthThree()
    {
        var inputs = ControlModel.AllInputs(InputLength).ToArray();
        ControlShape[] leaves = [new ControlShape.Chars("a"), new ControlShape.Chars("ab"), new ControlShape.Cut(), new ControlShape.Raise(0)];

        var difference = ControlModel.AllShapes(Depth, leaves)
            .SelectMany(shape => inputs.Select(input => Difference(shape, input)))
            .FirstOrDefault(d => d is not null);

        Assert.Null(difference);
    }

    [Fact]
    public void NoDiagnosticSurvivesFromAnAbandonedPathInRandomDeeperShapes() =>
        Gen.Select(_shapes, Gen.Char["ab"].Array[0, 6]).Sample(
            (shape, input) => Difference(shape, new string(input)) is null,
            iter: 20_000,
            print: t => $"{ControlModel.Describe(Number(t.Item1))} on \"{new string(t.Item2)}\""
        );

    [Fact]
    public void ADiagnosticRaisedOnThePathAParseKeepsSurvives() =>
        Assert.Equal(["d"], Survivors(new RaiseNode("d").Then(Char('a')).Or(Char('b')), "a"));

    [Fact]
    public void EveryAttemptTheModelLeavesOutWithdrawsTheDiagnosticsItRaised()
    {
        var raise = new RaiseNode("d");

        Assert.Empty(Survivors(Expression.Build(Digit, op => [[op.Prefix(raise.Then(Char('-')), static (_, x) => x)]]), "1"));
        Assert.Empty(Survivors(Expression.Build(Digit, op => [[op.Postfix(raise.Then(Char('!')), static (x, _) => x)]]), "1"));
        Assert.Empty(Survivors(Expression.Build(Digit, op => [[op.InfixL(raise.Then(Char('+')), static (l, _, _) => l)]]), "1"));
        Assert.Empty(Survivors(Expression.Build(raise.Then(Digit), op => [[op.Prefix(Char('-'), static (_, x) => x)]]), "x"));
        Assert.Empty(Survivors(raise.Then(Char('a')).SkipMany(), "b"));
        Assert.Empty(Survivors(raise.Then(Char('a')).Repeat(0, 2), "b"));
        Assert.Empty(Survivors(Char('a').SepBy1(raise.Then(Char(','))), "a"));
        Assert.Empty(Survivors(Char('a').SepEndBy(raise.Then(Char(','))), "a"));
    }

    [Fact]
    public void AWithdrawnDiagnosticGivesBackTheFailureItReported()
    {
        var grammar = new ReportNode<char>(Char('l')).Then(Char('i')).Or(Char('n'));
        var log = new FailureLog<char>();
        var plan = new PlanBuilder(FullMarks).Lower(grammar);

        Assert.False(Parsing.TryParse(log, plan, ";".AsMemory(), ParseOptions.Default, out _, out var error));
        Assert.Equal(new ParseError(SpanOf(0, 1), "parser", new UnexpectedItem("';'"), [new ExpectedItem("'i'"), new ExpectedItem("'l'"), new ExpectedItem("'n'")], []), error);
        Assert.Equal(0, log.DiagnosticCount);
    }

    [Fact]
    public void ALongestWinnerReportsWhatItReportsWhenItRunsAlone()
    {
        var winner = new ReportNode<char>(Char('q')).Then(String("ab")).Select(_ => 0);
        var loser = Char('a').Then(Char('z')).Select(_ => 0);
        ParseError[] alone = [new(SpanOf(0, 1), "parser", new UnexpectedItem("'a'"), [new ExpectedItem("'q'")], [])];

        Assert.Equal(alone, Raised(winner, "ab"));
        Assert.Equal(alone, Raised(Longest(winner, loser), "ab"));
        Assert.Equal(alone, Raised(Longest(loser, winner), "ab"));
    }

    [Fact]
    public void AGrammarThatCannotRaiseDiagnosticsTakesLightMarks() =>
        Assert.IsType<OrPlan<char, char, LightMark>>(new PlanBuilder().Lower(Char('a').Or(Char('b'))));

    [Fact]
    public void AGrammarWithABindTakesFullMarks() =>
        Assert.IsType<OrPlan<char, char, FullMark>>(new PlanBuilder().Lower(Char('a').Bind(c => Char(c)).Or(Char('b'))));

    [Fact]
    public void ABindReachedThroughARecGivesTheWholeGrammarFullMarks()
    {
        var bound = Char('a').Bind(c => Char(c));

        Assert.IsType<OrPlan<char, char, FullMark>>(new PlanBuilder().Lower(Char('x').Or(Rec(() => bound))));
    }

    [Fact]
    public void WithoutTheLightMarksRewriteEveryGrammarTakesFullMarks() =>
        Assert.IsType<OrPlan<char, char, FullMark>>(new PlanBuilder(FullMarks).Lower(Char('a').Or(Char('b'))));

    [Fact]
    public void AGrammarWithABindWithdrawsTheDiagnosticsOfAnAttemptThatFailsWithoutConsuming()
    {
        var grammar = new RaiseNode("d").Then(Return(0).Bind(_ => Char('b'))).Or(Char('a'));
        var log = new FailureLog<char>();
        var cursor = Cursor<char>.FromMemory("a".AsMemory()) with { Failures = log };
        var value = default(char);

        Assert.True(PlanCache.Get(grammar).TryParse(ref cursor, ref value));
        Assert.Equal(0, log.DiagnosticCount);
    }
}
