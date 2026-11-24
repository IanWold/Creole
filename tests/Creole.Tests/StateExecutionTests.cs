using System.Collections.Immutable;
using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file readonly record struct Depth(int Open, int Deepest);

/// <summary>
/// The code sample in docs/concepts/layer-state.md § An example. Keep the two in step.
/// </summary>
file class Nesting : CharGrammar
{
    private static readonly Parser<char, char> _close =
        GetState<Depth>().Where(static depth => depth.Open > 0, "no '(' to close")
            .Then(Char(')').Scan<Depth, char>(static (paren, depth, _) => (paren, depth with { Open = depth.Open - 1 })));

    private static readonly Parser<char, char> _open =
        Char('(').Scan<Depth, char>(static (paren, depth, _) => (paren, new Depth(depth.Open + 1, Math.Max(depth.Deepest, depth.Open + 1))));

    public static readonly Parser<char, int> Deepest =
        SetState(new Depth(0, 0)).Then(_open.Or(_close).SkipMany()).Before(End).Then(GetState<Depth>()).Select(static depth => depth.Deepest);
}

/// <summary>
/// A parser's state: <c>GetState</c>, <c>SetState</c> and <c>Scan</c>, and how backtracking restores it (§ 2. Layer-local state).
/// </summary>
/// <remarks>
/// The model-based tests run <see cref="ControlShape"/> grammars whose <see cref="ControlShape.Raise"/> leaves add an entry to an immutable list held as the state.
/// Backtracking puts back the state wherever it withdraws a diagnostic, so the list the parse leaves is the diagnostics <see cref="ControlModel.Diagnose"/> says survive.
/// </remarks>
public class StateExecutionTests
{
    private const int Depth = 3;
    private const int InputLength = 3;

    private static readonly Parser<char, char> _countA = Char('a').Scan<int, char>(static (a, count, _) => (a, count + 1));
    private static readonly Gen<ControlShape> _shapes = DiagnosticWithdrawalTests.Shapes(5).Where(shape => !ControlModel.RepeatsNullable(shape));

    private static string? Difference(ControlShape shape, string input, bool recordFailures)
    {
        var numbered = DiagnosticWithdrawalTests.Number(shape);
        var expected = ControlModel.InOrder.Diagnose(numbered, input);
        var actual = Run(numbered, input, recordFailures);

        return actual.Outcome == expected.Outcome && actual.State.SequenceEqual(expected.Diagnostics)
            ? null
            : $"{ControlModel.Describe(numbered)} on \"{input}\" {(recordFailures ? "with" : "without")} a failure log: model {expected.Outcome} [{string.Join(", ", expected.Diagnostics)}], interpreter {actual.Outcome} [{string.Join(", ", actual.State)}]";
    }

    private static Parser<char, string> Raise(int id) =>
        Return("").Scan<ImmutableList<string>, string>((_, raised, span) => ("", raised.Add($"d{id} at {span.Start}")));

    private static (Outcome Outcome, IReadOnlyList<string> State) Run(ControlShape shape, string input, bool recordFailures)
    {
        var cursor = Cursor<char>.FromMemory(input.AsMemory()) with
        {
            Failures = recordFailures
                ? new FailureLog<char>()
                : null,
            State = StateBox.Hold(ImmutableList<string>.Empty),
        };

        Outcome outcome;

        try
        {
            var plan = new PlanBuilder(dispatchThreshold: 1).Lower(ControlTable.Build(shape, Raise));
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

        return (outcome, StateBox.Read<ImmutableList<string>>(cursor.State));
    }

    private static int StateAfter<T>(Parser<char, T> parser, string text)
    {
        var grammar = SetState(0).Then(parser).Then(GetState<int>());

        Assert.Null(Execution.Disagreement(grammar, text.AsMemory(), ParseOptions.Default));

        return Value(grammar, text);
    }

    private static T Value<T>(Parser<char, T> parser, string text) =>
        Assert.IsType<Parsed<T>>(parser.Parse(text).Value).Value;

    [Fact]
    public void ABindWhoseParserSetsTheStateIsUndoneByTheMarksAroundIt() =>
        Assert.Equal(0, StateAfter(Atomic(Char('a').Bind(_ => SetState(1)).Then(Char('b'))).Or(Char('a')), "ac"));

    [Fact]
    public void ALongestLeavesTheStateItsWinnerLeavesWhenItRunsAlone()
    {
        var setsSeven = Char('a').Then(SetState(7)).As('l');

        Assert.Equal(0, StateAfter(Longest(String("ab").As('w'), _countA), "ab"));
        Assert.Equal(1, StateAfter(Longest(_countA.Then(Char('b')), setsSeven), "ab"));
        Assert.Equal(1, StateAfter(Longest(setsSeven, _countA.Then(Char('b'))), "ab"));
        Assert.Equal(7, StateAfter(Longest(String("ab").As('w'), setsSeven), "ac"));
    }

    [Fact]
    public void ALookaheadPutsBackTheState() =>
        Assert.Equal(0, StateAfter(Lookahead(_countA), "a"));

    [Fact]
    public void AnAtomicThatRewindsPutsBackTheState() =>
        Assert.Equal(0, StateAfter(Atomic(_countA.Then(Char('b'))).Or(Char('a')), "ac"));

    [Fact]
    public void AnAttemptThatFailsWithoutConsumingPutsBackTheState()
    {
        Assert.Equal(0, StateAfter(SetState(1).Then(Char('x')).Or(Return('y')), "a"));
        Assert.Equal(0, StateAfter(SetState(1).Then(Char('x')).Optional(), "a"));
        Assert.Equal(0, StateAfter(SetState(1).Then(Char('x')).Many(), "a"));
        Assert.Equal(2, StateAfter(_countA.Many(), "aab"));
    }

    [Fact]
    public void ANotPutsBackTheState() =>
        Assert.Equal(0, StateAfter(Not(_countA.Then(Char('b'))), "ac"));

    [Fact]
    public void TheDocumentedExampleFindsTheDeepestNestingAndRejectsAnUnopenedParenthesis()
    {
        Assert.Equal(3, Value(Nesting.Deepest, "(()(()))"));
        Assert.Equal(0, Value(Nesting.Deepest, ""));
        Assert.Equal(
            """
            error[parser]: expected '(' or end of input
             --> 1:3
              |
            1 | ())
              |   ^ no '(' to close
            """,
            Assert.IsType<Failed>(Nesting.Deepest.Parse("())").Value).Diagnostics.Render()
        );
    }

    [Fact]
    public void ANullStateReadsAsNullOnlyAsATypeThatAdmitsNull()
    {
        Assert.Null(Value(SetState<string?>(null).Then(GetState<string?>()), ""));
        Assert.Null(Value(SetState<int?>(null).Then(GetState<int?>()), ""));
        Assert.Equal(
            "The parser layer's state is null, but its grammar reads it as Int32, which cannot be null.",
            Assert.Throws<GrammarException>(static () => SetState<int?>(null).Then(GetState<int>()).Parse("")).Message
        );
    }

    [Fact]
    public void AParserSetsItsStateAtItsRootAndReadsItBack() =>
        Assert.Equal(3, Value(SetState(0).Then(_countA.Many()).Then(GetState<int>()), "aaa"));

    [Fact]
    public void ReadingStateAParserNeverSetThrows() =>
        Assert.Equal(
            "The parser layer has no state, but its grammar reads it as Int32. Give the layer an initialState, or set the state with SetState before reading it.",
            Assert.Throws<GrammarException>(static () => GetState<int>().Parse("")).Message
        );

    [Fact]
    public void ReadingStateAsAnotherTypeThrows()
    {
        const string Message = "The parser layer's state is of type String, but its grammar reads it as Int32.";

        Assert.Equal(Message, Assert.Throws<GrammarException>(static () => SetState("x").Then(GetState<int>()).Parse("")).Message);
        Assert.Equal(Message, Assert.Throws<GrammarException>(() => SetState("x").Then(_countA).Parse("a")).Message);
    }

    [Fact]
    public void ScanOverAParserThatConsumesNothingGetsAZeroWidthSpan() =>
        Assert.Equal(
            new SourceSpan(SourceId.Unnamed, 1, 1),
            Value(SetState(0).Then(Char('a')).Then(Return(1).Scan<int, SourceSpan>(static (_, count, span) => (span, count))), "a")
        );

    [Fact]
    public void ScanReadsTheStateItsSourceLeftAndTheSpanItConsumed() =>
        Assert.Equal(
            (11, new SourceSpan(SourceId.Unnamed, 0, 2)),
            Value(SetState(10).Then(Char('b').Then(_countA).Scan<int, (int, SourceSpan)>(static (_, count, span) => ((count, span), count))), "ba")
        );

    [Fact]
    public void SetStateAllocatesNothingWhenItRuns()
    {
        var plan = PlanCache.Get(SetState(5).Then(GetState<int>()));

        Assert.Equal(
            0,
            Allocation.Fewest(() =>
            {
                var cursor = new Cursor<char>("");
                var value = 0;

                _ = plan.TryParse(ref cursor, ref value);
            })
        );
    }

    [Fact]
    public void StateReadsAsATypeItDerivesFrom()
    {
        Assert.Equal("x", Value(SetState("x").Then(GetState<object>()), ""));
        Assert.Equal("x", Value(SetState("x").Then(GetState<IComparable>()), ""));
    }

    [Fact]
    public void TheStateAfterAnyShapeToDepthThreeIsWhatThePathTheParseKeptLeft()
    {
        var inputs = ControlModel.AllInputs(InputLength).ToArray();
        ControlShape[] leaves = [new ControlShape.Chars("a"), new ControlShape.Chars("ab"), new ControlShape.Cut(), new ControlShape.Raise(0)];

        var difference = ControlModel.AllShapes(Depth, leaves)
            .SelectMany(shape => inputs.SelectMany(input => new[] { Difference(shape, input, recordFailures: false), Difference(shape, input, recordFailures: true) }))
            .FirstOrDefault(d => d is not null);

        Assert.Null(difference);
    }

    [Fact]
    public void TheStateAfterRandomDeeperShapesIsWhatThePathTheParseKeptLeft() =>
        Gen.Select(_shapes, Gen.Char["ab"].Array[0, 6], Gen.Bool).Sample(
            (shape, input, recordFailures) => Difference(shape, new string(input), recordFailures) is null,
            iter: 20_000,
            print: t => $"{ControlModel.Describe(DiagnosticWithdrawalTests.Number(t.Item1))} on \"{new string(t.Item2)}\" {(t.Item3 ? "with" : "without")} a failure log"
        );
}
