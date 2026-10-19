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
