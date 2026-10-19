namespace Creole.Tests;

public class StagingTests
{
    private static bool ReadOne(ref Cursor<char> cursor, out char result) =>
        cursor.TryRead(out result);

    [Fact]
    public void ARuleCanBeRegisteredOnlyOnce()
    {
        var rule = new ReturnNode<char, char>('r');
        Staging.Register(rule, ReadOne);

        Assert.Throws<InvalidOperationException>(() => Staging.Register(rule, ReadOne));
    }

    [Fact]
    public void FailingCodeLeavesTheResultUnchanged()
    {
        var rule = new ReturnNode<char, char>('r');
        Staging.Register(rule, ReadOne);

        var plan = PlanCache.Get(rule);
        var cursor = new Cursor<char>("");
        var result = '?';

        Assert.False(plan.TryParse(ref cursor, ref result));
        Assert.Equal('?', result);
    }

    [Fact]
    public void RegisteredCodeReplacesAChildNode()
    {
        var rule = new CountingNode<char>(new FailNode<char, char>("interpreted"));
        Staging.Register(rule, ReadOne);

        var grammar = new PairNode<char, int>(rule, new ReturnNode<char, int>(1));

        var plan = PlanCache.Get(grammar);
        var cursor = new Cursor<char>("xy");
        (char, int) result = default;

        Assert.Equal(0, rule.LowerCount);
        Assert.True(plan.TryParse(ref cursor, ref result));
        Assert.Equal(('x', 1), result);
        Assert.Equal(1, cursor.Position);
    }

    [Fact]
    public void RegisteredCodeReplacesARootNode()
    {
        var rule = new CountingNode<char>(new FailNode<char, char>("interpreted"));
        Staging.Register(rule, ReadOne);

        var plan = PlanCache.Get(rule);
        var cursor = new Cursor<char>("ab");
        var result = '?';

        Assert.Equal(0, rule.LowerCount);
        Assert.True(plan.TryParse(ref cursor, ref result));
        Assert.Equal('a', result);
        Assert.Equal(1, cursor.Position);
    }

    [Fact]
    public void RegisteringAfterFirstUseDoesNotChangeTheCachedPlan()
    {
        var rule = new ReturnNode<char, char>('r');
        var before = PlanCache.Get(rule);

        Staging.Register(rule, ReadOne);

        Assert.Same(before, PlanCache.Get(rule));
        Assert.IsType<ReturnPlan<char, char>>(before);
    }
}
