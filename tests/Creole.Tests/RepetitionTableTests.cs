using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

public class RepetitionTableTests
{
    private static readonly Dictionary<string, RepetitionCase> _cases = RepetitionTable.Cases.ToDictionary(c => c.Name);

    public static TheoryData<string> CaseNames => [.. _cases.Keys];

    private static string Render(IReadOnlyList<string> items) =>
        $"[{string.Join(",", items)}]";

    private static Parser<char, string> ToCreole(Shape shape) => shape switch
    {
        Shape.Character s => Char(s.Value).Select(c => c.ToString()),
        Shape.TwoCharacters s => Seq(Char(s.First), Char(s.Second)).Select((a, b) => $"{a}{b}"),
        Shape.Return s => Return(s.Value),
        Shape.Many s => ToCreole(s.Item).Many().Select(Render),
        Shape.AtLeastOnce s => ToCreole(s.Item).AtLeastOnce().Select(Render),
        Shape.Repeat s => ToCreole(s.Item).Repeat(s.Count).Select(Render),
        Shape.RepeatRange s => ToCreole(s.Item).Repeat(s.Min, s.Max).Select(Render),
        Shape.SepBy s => ToCreole(s.Item).SepBy(ToCreole(s.Separator)).Select(Render),
        Shape.SepBy1 s => ToCreole(s.Item).SepBy1(ToCreole(s.Separator)).Select(Render),
        Shape.SepEndBy s => ToCreole(s.Item).SepEndBy(ToCreole(s.Separator)).Select(Render),
        Shape.Until s => ToCreole(s.Item).Until(ToCreole(s.Terminator)).Select(Render),
        Shape.SkipMany s => ToCreole(s.Item).SkipMany().As("()"),
        Shape.SkipAtLeastOnce s => ToCreole(s.Item).SkipAtLeastOnce().As("()"),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void TheInterpreterAgreesWithTheTable(string name)
    {
        var row = _cases[name];

        Assert.Equal(row.Expected, Execution.Outcome(ToCreole(row.Shape), row.Input));
    }
}
