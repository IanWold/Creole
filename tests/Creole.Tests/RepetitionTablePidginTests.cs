using Pidgin;
using PidginParser = Pidgin.Parser;
using PidginCharParser = Pidgin.Parser<char>;

namespace Creole.Tests;

public class RepetitionTablePidginTests
{
    private static readonly Dictionary<string, RepetitionCase> _cases = RepetitionTable.Cases.ToDictionary(c => c.Name);

    public static TheoryData<string> CaseNames => [.. _cases.Keys];

    private static IEnumerable<Shape> Flatten(Shape shape)
    {
        yield return shape;

        var children = shape switch
        {
            Shape.Many s => [s.Item],
            Shape.AtLeastOnce s => [s.Item],
            Shape.Repeat s => [s.Item],
            Shape.RepeatRange s => [s.Item],
            Shape.SepBy s => [s.Item, s.Separator],
            Shape.SepBy1 s => [s.Item, s.Separator],
            Shape.SepEndBy s => [s.Item, s.Separator],
            Shape.Until s => [s.Item, s.Terminator],
            Shape.SkipMany s => [s.Item],
            Shape.SkipAtLeastOnce s => [s.Item],
            _ => Array.Empty<Shape>(),
        };

        foreach (var child in children.SelectMany(Flatten))
        {
            yield return child;
        }
    }

    private static Pidgin.Parser<char, IEnumerable<string>> Optional(Pidgin.Parser<char, string> item, int count) =>
        count == 0
        ? PidginCharParser.Return(Enumerable.Empty<string>())
        : PidginParser.Map((first, rest) => rest.Prepend(first), item, Optional(item, count - 1))
            .Or(PidginCharParser.Return(Enumerable.Empty<string>()));

    private static string Render(IEnumerable<string> items) =>
        $"[{string.Join(",", items)}]";

    private static Pidgin.Parser<char, IEnumerable<string>> RepeatRange(Pidgin.Parser<char, string> item, int min, int max) =>
        PidginParser.Map((required, optional) => required.Concat(optional), item.Repeat(min), Optional(item, max - min));

    private static Outcome Run(Shape shape, string input)
    {
        var parser = PidginParser.Map((value, offset) => (Outcome)new Outcome.Ok(value, offset), ToPidgin(shape), PidginCharParser.CurrentOffset)
            .Or(PidginCharParser.Return<Outcome>(new Outcome.FailEmpty()));

        try
        {
            var result = parser.Parse(input);
            return result.Success
                ? result.Value
                : new Outcome.FailConsumed();
        }
        catch (InvalidOperationException)
        {
            return new Outcome.Throws();
        }
    }

    private static Pidgin.Parser<char, string> ToPidgin(Shape shape) => shape switch
    {
        Shape.Character s => PidginParser.Char(s.Value).Select(c => c.ToString()),
        Shape.TwoCharacters s => PidginParser.Map((a, b) => $"{a}{b}", PidginParser.Char(s.First), PidginParser.Char(s.Second)),
        Shape.Return s => PidginCharParser.Return(s.Value),
        Shape.Many s => ToPidgin(s.Item).Many().Select(Render),
        Shape.AtLeastOnce s => ToPidgin(s.Item).AtLeastOnce().Select(Render),
        Shape.Repeat s => ToPidgin(s.Item).Repeat(s.Count).Select(Render),
        Shape.RepeatRange s => RepeatRange(ToPidgin(s.Item), s.Min, s.Max).Select(Render),
        Shape.SepBy s => ToPidgin(s.Item).Separated(ToPidgin(s.Separator)).Select(Render),
        Shape.SepBy1 s => ToPidgin(s.Item).SeparatedAtLeastOnce(ToPidgin(s.Separator)).Select(Render),
        Shape.SepEndBy s => ToPidgin(s.Item).SeparatedAndOptionallyTerminated(ToPidgin(s.Separator)).Select(Render),
        Shape.Until s => ToPidgin(s.Item).Until(ToPidgin(s.Terminator)).Select(Render),
        Shape.SkipMany s => ToPidgin(s.Item).SkipMany().Select(_ => "()"),
        Shape.SkipAtLeastOnce s => ToPidgin(s.Item).SkipAtLeastOnce().Select(_ => "()"),
        _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, null),
    };

    [Fact]
    public void CaseNamesAreUnique() =>
        Assert.Equal(RepetitionTable.Cases.Count, _cases.Count);

    [Fact]
    public void EveryShapeAppearsInTheTable()
    {
        var used = RepetitionTable.Cases.SelectMany(c => Flatten(c.Shape)).Select(s => s.GetType()).ToHashSet();
        var all = typeof(Shape).GetNestedTypes().Where(t => t.IsSubclassOf(typeof(Shape)));

        Assert.All(all, type => Assert.Contains(type, used));
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void PidginAgreesWithTheTable(string name)
    {
        var row = _cases[name];

        Assert.Equal(row.Expected, Run(row.Shape, row.Input));
    }
}
