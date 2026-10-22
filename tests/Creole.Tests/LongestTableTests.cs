namespace Creole.Tests;

public class LongestTableTests
{
    private static readonly Dictionary<string, LongestCase> _cases = LongestTable.Cases.ToDictionary(c => c.Name);

    public static TheoryData<string> CaseNames => [.. _cases.Keys];

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void EachRowBuildsOneLongestNode(string name)
    {
        var row = _cases[name];
        var node = Assert.IsType<LongestNode<char, string>>(LongestTable.ToParser(row));

        Assert.Equal(row.Alternatives.Count, node.Alternatives.Count);
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void TheInterpreterGivesEachRowItsOutcome(string name)
    {
        var row = _cases[name];

        Assert.Equal(row.Expected, Execution.Outcome(LongestTable.ToParser(row), row.Input));
    }
}
