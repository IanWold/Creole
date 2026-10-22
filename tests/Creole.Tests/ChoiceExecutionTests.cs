using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed record Small(char Digit);

file sealed record Large(char Digit);

file union Size(Small, Large);

file static class Sizes
{
    public static readonly Parser<char, Size> Size = Digit.Select(static d => d < '5' ? (Size)new Small(d) : new Large(d));

    public static readonly Parser<char, string> Sized = Size.Match(
        Case(Char('!'), static (Small s, char _) => $"small {s.Digit}"),
        Case(Return(0), static (Large l, int _) => $"large {l.Digit}")
    );

    public static readonly Parser<char, char> SmallOnly = Size.Match(Case(Return(0), static (Small s, int _) => s.Digit));
}

public class ChoiceExecutionTests
{
    private static readonly Outcome _failConsumed = new Outcome.FailConsumed();
    private static readonly Outcome _failEmpty = new Outcome.FailEmpty();

    private static readonly Parser<char, string> _versioned = Digit.Switch(
        ('1', String("one")),
        ('2', String("two")),
        @default: Return("other")
    );

    private static Outcome.Ok Ok(string value, int consumed) => new(value, consumed);

    [Fact]
    public void MatchFailsWhenNoCaseApplies() =>
        Assert.Equal(_failConsumed, Execution.Outcome(Sizes.SmallOnly, "7"));

    [Fact]
    public void MatchFailsWithoutConsumingWhenItsScrutineeDoes() =>
        Assert.Equal(_failEmpty, Execution.Outcome(Sizes.Sized, "x"));

    [Fact]
    public void MatchRunsTheBranchOfTheValuesCase()
    {
        Assert.Equal(Ok("small 3", 2), Execution.Outcome(Sizes.Sized, "3!"));
        Assert.Equal(Ok("large 7", 1), Execution.Outcome(Sizes.Sized, "7!"));
    }

    [Fact]
    public void MatchWhoseBranchFailsFailsAfterConsuming() =>
        Assert.Equal(_failConsumed, Execution.Outcome(Sizes.Sized, "3?"));

    [Fact]
    public void OneOfTriesEachAlternativeInOrder()
    {
        var keyword = OneOf(String("if"), String("in"), String("is"));

        Assert.Equal(Ok("is", 2), Execution.Outcome(keyword, "is"));
        Assert.Equal(_failEmpty, Execution.Outcome(keyword, "it"));
    }

    [Fact]
    public void OrDoesNotTryTheNextAlternativeAfterACommittedFailure() =>
        Assert.Equal(_failConsumed, Execution.Outcome(Seq(Char('a'), Char('b')).Select((a, b) => b).Or(Char('a')), "ac"));

    [Fact]
    public void OrTakesTheFirstAlternativeThatMatches() =>
        Assert.Equal(Ok("a", 1), Execution.Outcome(Char('a').Or(Letter), "ab"));

    [Fact]
    public void SwitchFailsAfterConsumingWhenTheBranchFails() =>
        Assert.Equal(_failConsumed, Execution.Outcome(_versioned, "1two"));

    [Fact]
    public void SwitchFailsWithoutConsumingWhenItsScrutineeDoes() =>
        Assert.Equal(_failEmpty, Execution.Outcome(_versioned, "x"));

    [Fact]
    public void SwitchRunsTheBranchWhoseKeyMatches()
    {
        Assert.Equal(Ok("one", 4), Execution.Outcome(_versioned, "1one"));
        Assert.Equal(Ok("two", 4), Execution.Outcome(_versioned, "2two"));
    }

    [Fact]
    public void SwitchRunsTheDefaultWhenNoKeyMatches() =>
        Assert.Equal(Ok("other", 1), Execution.Outcome(_versioned, "9"));

    [Fact]
    public void SwitchWithACollectionOfCasesRunsTheMatchingBranch()
    {
        var opcode = Any.Switch([.. Enumerable.Range(0, 20).Select(i => ((char)('a' + i), Return(i)))], @default: Fail<int>("unknown opcode"));

        Assert.Equal(Ok("19", 1), Execution.Outcome(opcode, "t"));
        Assert.Equal(_failConsumed, Execution.Outcome(opcode, "z"));
    }
}
