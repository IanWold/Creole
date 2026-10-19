namespace Creole.Tests;

public class PlanLoweringTests
{
    [Fact]
    public void ANodeReachedTwiceIsLoweredOnceAndItsPlanShared()
    {
        var shared = new CountingNode<int>(new ReturnNode<char, int>(1));
        var grammar = new PairNode<int, int>(shared, shared);

        var plan = Assert.IsType<PairPlan<int, int>>(new PlanBuilder().Lower(grammar));
        var cursor = new Cursor<char>("");
        (int, int) result = default;

        Assert.Equal(1, shared.LowerCount);
        Assert.Same(plan.First, plan.Second);
        Assert.True(plan.TryParse(ref cursor, ref result));
        Assert.Equal((1, 1), result);
    }

    [Fact]
    public void EachBuilderLowersAfresh()
    {
        var node = new CountingNode<int>(new ReturnNode<char, int>(1));

        var first = new PlanBuilder().Lower(node);
        var second = new PlanBuilder().Lower(node);

        Assert.Equal(2, node.LowerCount);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void FailFailsAndConsumesNothing()
    {
        var plan = new PlanBuilder().Lower(new FailNode<char, int>("never"));
        var cursor = new Cursor<char>("abc");
        var result = 7;

        Assert.False(plan.TryParse(ref cursor, ref result));
        Assert.Equal(7, result);
        Assert.Equal(0, cursor.Position);
    }

    [Fact]
    public void ReturnSucceedsWithItsValueAndConsumesNothing()
    {
        var plan = new PlanBuilder().Lower(new ReturnNode<char, int>(42));
        var cursor = new Cursor<char>("abc");
        var result = 0;

        Assert.True(plan.TryParse(ref cursor, ref result));
        Assert.Equal(42, result);
        Assert.Equal(0, cursor.Position);
    }
}
