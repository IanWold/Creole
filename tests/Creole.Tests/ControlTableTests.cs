namespace Creole.Tests;

public class ControlTableTests
{
    private static readonly Dictionary<string, ControlCase> _cases = ControlTable.Cases.ToDictionary(c => c.Name);

    public static TheoryData<string> CaseNames => [.. _cases.Keys];

    private static IEnumerable<INode> Walk(INode node) =>
        node.Children.SelectMany(Walk).Prepend(node);

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void EachCaseBuildsAGrammar(string name)
    {
        var parser = ControlTable.Build(_cases[name].Shape);

        Assert.NotNull(parser);
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void TheInterpreterGivesEachRowItsOutcome(string name)
    {
        var row = _cases[name];

        Assert.Equal(row.Expected, Execution.Outcome(ControlTable.Build(row.Shape), row.Input));
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void TheInterpreterReportsEachRowsFailure(string name)
    {
        var row = _cases[name];

        Assert.Equal(
            row.Reported is null ? null : $"error[parser] at {row.Reported}",
            Execution.Error(ControlTable.Build(row.Shape), row.Input)?.ToString()
        );
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void EveryRewriteGivesEachRowTheSameOutcomeAndFailure(string name)
    {
        var row = _cases[name];
        var (without, with) = Rewrites.Compare(ControlTable.Build(row.Shape), LoweringOptions.All, row.Input);

        Assert.Equal(without, with);
    }

    [Fact]
    public void TheTableCoversEveryControlNode()
    {
        var kinds = ControlTable.Cases.SelectMany(c => Walk(ControlTable.Build(c.Shape))).Select(n => n.Kind).ToHashSet();

        Assert.Superset(new HashSet<NodeKind> { NodeKind.Atomic, NodeKind.Lookahead, NodeKind.Not, NodeKind.Cut, NodeKind.Named, NodeKind.Context }, kinds);
    }

    [Fact]
    public void TheTableHasOneCasePerName() =>
        Assert.Equal(ControlTable.Cases.Count, _cases.Count);
}
