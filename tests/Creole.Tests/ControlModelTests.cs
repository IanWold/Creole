namespace Creole.Tests;

public class ControlModelTests
{
    private const int Depth = 3;
    private const int InputLength = 3;

    private static readonly Dictionary<string, ControlCase> _cases = ControlTable.Cases.ToDictionary(c => c.Name);
    private static readonly string[] _inputs = [.. ControlModel.AllInputs(InputLength)];
    private static readonly ControlShape[] _shapes = [.. ControlModel.AllShapes(Depth)];

    public static TheoryData<string> CaseNames => [.. _cases.Keys];

    private static string? FirstDifference(Func<ControlShape, string, string?> compare) =>
        _shapes.SelectMany(shape => _inputs.Select(input => compare(shape, input))).FirstOrDefault(difference => difference is not null);

    [Fact]
    public void DispatchingGivesTheSameResultsAsRunningInOrder()
    {
        var difference = FirstDifference((shape, input) =>
            ControlModel.InOrder.Run(shape, input) is var inOrder
                && ControlModel.Dispatching.Run(shape, input) is var dispatching
                && inOrder != dispatching
                ? $"{ControlModel.Describe(shape)} on \"{input}\": in order {inOrder}, dispatching {dispatching}"
                : null
        );

        Assert.Null(difference);
    }

    [Fact]
    public void FirstAndNullableAreSound()
    {
        var difference = FirstDifference((shape, input) => ControlModel.InOrder.Run(shape, input).Outcome switch
        {
            Outcome.Ok { Consumed: 0 } when !ControlModel.Nullable(shape) =>
                $"{ControlModel.Describe(shape)} on \"{input}\" succeeds without consuming, but is not nullable",
            Outcome.Ok { Consumed: > 0 } or Outcome.FailConsumed when !ControlModel.First(shape, input[0]) =>
                $"{ControlModel.Describe(shape)} on \"{input}\" consumes '{input[0]}' first, which is not in its FIRST set",
            _ => null,
        });

        Assert.Null(difference);
    }

    [Fact]
    public void NestingSequencesChangesNoResult()
    {
        var difference = FirstDifference((shape, input) =>
            ControlModel.InOrder.Run(shape, input) is var nested
                && ControlModel.InOrder.Run(ControlModel.Flatten(shape), input) is var flat
                && nested != flat
                ? $"{ControlModel.Describe(shape)} on \"{input}\": nested {nested}, flat {flat}"
                : null
        );

        Assert.Null(difference);
    }

    [Fact]
    public void TheGrammarsExerciseCutsAndDispatch()
    {
        var results = _shapes.SelectMany(shape => _inputs.Select(input => ControlModel.InOrder.Run(shape, input))).ToArray();

        var skipped = _shapes.Sum(shape => _inputs.Sum(input => ControlModel.Dispatching.SkippedAttemptsWithCuts(shape, input)));

        Assert.True(_shapes.Length > 30_000, $"{_shapes.Length} grammars");
        Assert.True(skipped > 10_000, $"Dispatch skipped {skipped} attempts containing a cut.");
        Assert.Contains(results, r => r.Outcome is Outcome.FailConsumed && r.CutInForce);
        Assert.Contains(results, r => r.Outcome is Outcome.FailEmpty && r.CutInForce);
        Assert.Contains(results, r => r.Outcome is Outcome.Ok && r.CutInForce);
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void TheModelGivesEachTableRowItsOutcome(string name)
    {
        var row = _cases[name];

        Assert.Equal(row.Expected, ControlModel.InOrder.Run(row.Shape, row.Input).Outcome);
        Assert.Equal(row.Expected, ControlModel.Dispatching.Run(row.Shape, row.Input).Outcome);
    }
}
