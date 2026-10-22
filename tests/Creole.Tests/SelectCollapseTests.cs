using System.Globalization;
using CsCheck;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed class Depths : CharGrammar
{
    public static readonly Parser<char, int> Group = Rec(() => Inner).Between(Char('('), Char(')'));

    public static readonly Parser<char, int> Inner = OneOf(Group.Select(static d => d + 1).Select(static d => d * 2), Char('x').As(1));
}

public class SelectCollapseTests
{
    private static readonly Parser<char, string>[] _sources =
    [
        String("ab"),
        String("ab").Or(Return("r")),
        Char('a').Then(String("bc")),
        new CutThenNode<string>(String("a")),
        Char('a').Select(static c => c.ToString()),
    ];

    private static Parser<char, string> Chain(Parser<char, string> source, int length)
    {
        for (var i = 1; i <= length; i++)
        {
            var suffix = i.ToString(CultureInfo.InvariantCulture);
            source = source.Select(s => s + suffix);
        }

        return source;
    }

    private static void AssertAgrees<T>(Parser<char, T> parser, string input, bool cut = false)
    {
        var (without, with) = Rewrites.Compare(parser, LoweringOptions.CollapseSelect, input, cut: cut);

        Assert.Equal(without, with);
    }

    [Fact]
    public void AChainAgreesWithRunningEachSelectAsItsOwnPlan() =>
        Gen
        .Select(Gen.Int[0, _sources.Length - 1], Gen.Int[1, 4], Gen.Char["abcr"].Array[0, 4], Gen.Bool)
        .Sample((source, length, input, cut) => AssertAgrees(Chain(_sources[source], length), new string(input), cut));

    [Fact]
    public void AChainOfSelectsLowersToOnePlanOverTheFirstSource()
    {
        var chain = Digit.Select(static c => c - '0').Select(static d => d.ToString(CultureInfo.InvariantCulture));

        Assert.IsType<MapPlan<char, char, int, string>>(new PlanBuilder(LoweringOptions.CollapseSelect).Lower(chain));
        Assert.IsType<MapPlan<char, int, string>>(new PlanBuilder(LoweringOptions.None).Lower(chain));
    }

    [Fact]
    public void AChainOverARuleStillBeingLoweredAgrees()
    {
        AssertAgrees(Depths.Inner, "((x))");
        AssertAgrees(Depths.Inner, "((x)");
        Assert.Equal("10", Rewrites.Compare(Depths.Inner, LoweringOptions.CollapseSelect, "((x))").With.Value);
    }

    [Fact]
    public void ASharedSelectKeepsItsOwnValueWhereItIsUsedAlone()
    {
        var inner = Char('a').Select(static _ => "i");
        var parser = Seq(inner.Select(static s => s + "o"), inner);

        AssertAgrees(parser, "aa");
        Assert.Equal("(io, i)", Rewrites.Compare(parser, LoweringOptions.CollapseSelect, "aa").With.Value);
    }

    [Theory]
    [InlineData("7", "f g")]
    [InlineData("x", "")]
    public void TheFunctionsRunInOrderOnceEachAndOnlyOnSuccess(string input, string expected)
    {
        var calls = new List<string>();
        var parser = Digit
            .Select(c => { calls.Add("f"); return c; })
            .Select(c => { calls.Add("g"); return c; });

        var cursor = new Cursor<char>(input);
        char value = default;
        var ok = new PlanBuilder(LoweringOptions.CollapseSelect).Lower(parser).TryParse(ref cursor, ref value);

        Assert.Equal((expected.Length > 0, expected), (ok, string.Join(' ', calls)));
    }
}
