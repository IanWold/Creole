using static Creole.Grammar<char>;

namespace Creole.Tests;

file sealed class RecOnly : CharGrammar
{
    public static readonly Parser<char, int> A = Rec(() => B);

    public static readonly Parser<char, int> B = Rec(() => A);

    public static readonly Parser<char, int> Itself = Rec(() => Itself);

    public static readonly Parser<char, int> Through = Return(1).Or(Itself);
}

public class RecLoweringTests
{
    private static (CountingNode<int> A, CountingNode<int> B) Mutual()
    {
        CountingNode<int>? b = null;
        var a = new CountingNode<int>(Return(1).Or(Rec(() => b)));
        b = new CountingNode<int>(Return(2).Or(a));

        return (a, b);
    }

    private static CountingNode<int> Self()
    {
        CountingNode<int>? s = null;
        s = new CountingNode<int>(Return(1).Or(Rec(() => s)));

        return s;
    }

    [Fact]
    public void ALaterNodeOnTheCycleIsTheRoot()
    {
        var (_, rootB) = Mutual();

        var b = Assert.IsType<OrPlan<char, int>>(new PlanBuilder().Lower(rootB));
        var a = Assert.IsType<OrPlan<char, int>>(b.Alternatives[1]);
        var rec = Assert.IsType<RecPlan<char, int>>(a.Alternatives[1]);

        Assert.Same(b, Assert.IsType<ForwardPlan<char, int>>(rec.Target).Target);
    }

    [Fact]
    public void ANullTargetNamesTheRulePath()
    {
        Parser<char, int>? missing = null;
        var inner = Rec(() => missing);
        var outer = Rec(() => inner);

        var exception = Assert.Throws<GrammarException>(() => new PlanBuilder().Lower(outer));

        Assert.Equal("'() => missing' returned null. A Rec must refer to a rule that is initialized by the time the grammar is used. Rule path: inner → missing.", exception.Message);
        Assert.IsType<GrammarException>(exception.InnerException);
    }

    [Fact]
    public void ARecLowersToAStepOverItsTargetsPlan()
    {
        var target = new ReturnNode<char, int>(1);
        var builder = new PlanBuilder();

        Assert.Same(builder.Lower(target), Assert.IsType<RecPlan<char, int>>(builder.Lower(Rec(() => target))).Target);
    }

    [Fact]
    public void ARecOnlyCycleIsAGrammarException()
    {
        var exception = Assert.Throws<GrammarException>(() => new PlanBuilder().Lower(RecOnly.A));

        Assert.Equal("The rules B → A → B refer to each other through Rec without parsing anything, so parsing them would never terminate.", exception.Message);
    }

    [Fact]
    public void ARecThatReturnsItselfIsAGrammarExceptionWhereverItIsReached()
    {
        var exception = Assert.Throws<GrammarException>(() => new PlanBuilder().Lower(RecOnly.Through));

        Assert.Equal("The rules Itself → Itself refer to each other through Rec without parsing anything, so parsing them would never terminate.", exception.Message);
    }

    [Fact]
    public void MutualRecursionLowersEachNodeOnceAndClosesTheCycle()
    {
        var (nodeA, nodeB) = Mutual();

        var a = Assert.IsType<OrPlan<char, int>>(new PlanBuilder().Lower(nodeA));
        var b = Assert.IsType<OrPlan<char, int>>(Assert.IsType<RecPlan<char, int>>(a.Alternatives[1]).Target);
        var back = Assert.IsType<ForwardPlan<char, int>>(b.Alternatives[1]);

        Assert.Equal(1, nodeA.LowerCount);
        Assert.Equal(1, nodeB.LowerCount);
        Assert.Same(a, back.Target);
    }

    [Fact]
    public void SelfRecursionLowersOnceAndForwardsToItself()
    {
        var node = Self();
        var builder = new PlanBuilder();

        var s = Assert.IsType<OrPlan<char, int>>(builder.Lower(node));
        var back = Assert.IsType<ForwardPlan<char, int>>(Assert.IsType<RecPlan<char, int>>(s.Alternatives[1]).Target);

        Assert.Equal(1, node.LowerCount);
        Assert.Same(s, back.Target);
        Assert.Same(s, builder.Lower(node));
    }

    [Fact]
    public void TheCacheLowersACyclicGrammarOnce()
    {
        var node = Self();

        var first = PlanCache.Get(node);
        var second = PlanCache.Get(node);

        Assert.Same(first, second);
        Assert.Equal(1, node.LowerCount);
    }
}
