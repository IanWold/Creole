using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

public class RepetitionExecutionTests
{
    private static readonly Parser<char, Unit> _cutThenSucceed = new CutThenNode<Unit>(Return(default(Unit)));

    private static (bool Ok, bool Cut, long Position) Run<T>(Parser<char, T> parser, string input)
    {
        var cursor = new Cursor<char>(input);
        var (ok, _) = Execution.Run(parser, ref cursor);

        return (ok, cursor.Cut, cursor.Position);
    }

    [Fact]
    public void ABoundedRepeatRunsAnItemThatConsumesNothingUpToItsMaximum() =>
        Assert.Equal(new Outcome.Ok("3", 0), Execution.Outcome(Return('x').Repeat(1, 3).Select(items => items.Count), "a"));

    [Fact]
    public void AnItemThatFailsWithoutConsumingDropsItsCut()
    {
        Assert.Equal((true, false, 0L), Run(new CutThenNode<char>(Char('a')).Many(), "b"));
        Assert.Equal((true, false, 0L), Run(new CutThenNode<char>(Char('a')).SkipMany(), "b"));
    }

    [Fact]
    public void AMandatoryItemThatFailsWithoutConsumingDropsItsCut() =>
        Assert.Equal((false, false, 0L), Run(new CutThenNode<char>(Char('a')).AtLeastOnce(), "b"));

    [Fact]
    public void AnItemThatSucceededKeepsItsCut() =>
        Assert.Equal((true, true, 1L), Run(new CutThenNode<char>(Char('a')).Many(), "ab"));

    [Fact]
    public void ASeparatorThatMatchesWithoutConsumingKeepsItsCutWhenTheListEnds() =>
        Assert.Equal((true, true, 1L), Run(Char('a').SepBy(_cutThenSucceed), "ab"));

    [Fact]
    public void ASeparatorThatFailsWithoutConsumingDropsItsCut() =>
        Assert.Equal((true, false, 1L), Run(Char('a').SepBy(new CutThenNode<char>(Char(','))), "ab"));

    [Fact]
    public void AnUntilTerminatorThatFailsWithoutConsumingDropsItsCut() =>
        Assert.Equal((false, false, 1L), Run(Char('a').Until(new CutThenNode<char>(Char(';'))), "ab"));

    [Fact]
    public void AnUntilItemThatFailsWithoutConsumingDropsItsCut() =>
        Assert.Equal((false, false, 0L), Run(new CutThenNode<char>(Char('a')).Until(Char(';')), "b"));

    [Fact]
    public void ManyOverAnItemThatConsumesNothingThrowsAGrammarException()
    {
        var exception = Assert.Throws<GrammarException>(() => Run(Return('x').Many(), "a"));

        Assert.Contains("would never end", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RepetitionResultsAreInOrder() =>
        Assert.Equal(new Outcome.Ok("abc", 3), Execution.Outcome(Letter.Many().Select(string.Concat), "abc1"));

    [Fact]
    public void TheGuardFiresOnlyWhenARoundConsumesNothing() =>
        Assert.Equal(new Outcome.Ok("2", 2), Execution.Outcome(Char('a').Optional().SepBy(Char(',')).Select(items => items.Count), "a,;"));
}
