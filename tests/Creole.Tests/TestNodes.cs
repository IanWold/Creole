namespace Creole.Tests;

internal class CountingNode<TOut>(Parser<char, TOut> inner) : Parser<char, TOut>
{
    private int _lowerCount;

    internal override IReadOnlyList<INode> Children => [inner];

    internal override NodeKind Kind => NodeKind.Return;

    public int LowerCount => Volatile.Read(ref _lowerCount);

    internal override Plan<char, TOut> Lower(PlanBuilder builder)
    {
        Interlocked.Increment(ref _lowerCount);
        return builder.Lower(inner);
    }
}

internal class PairNode<TA, TB>(Parser<char, TA> first, Parser<char, TB> second) : Parser<char, (TA, TB)>
{
    internal override IReadOnlyList<INode> Children => [first, second];

    internal override NodeKind Kind => NodeKind.Return;

    internal override Plan<char, (TA, TB)> Lower(PlanBuilder builder) =>
        new PairPlan<TA, TB>(builder.Lower(first), builder.Lower(second));
}

internal class PairPlan<TA, TB>(Plan<char, TA> first, Plan<char, TB> second) : Plan<char, (TA, TB)>
{
    public Plan<char, TA> First { get; } = first;

    public Plan<char, TB> Second { get; } = second;

    public override bool TryParse(ref Cursor<char> cursor, ref (TA, TB) result)
    {
        TA a = default!;
        TB b = default!;

        if (!First.TryParse(ref cursor, ref a) || !Second.TryParse(ref cursor, ref b))
        {
            return false;
        }

        result = (a, b);
        return true;
    }
}

internal class CutThenNode<T>(Parser<char, T> inner) : Parser<char, T>
{
    internal override IReadOnlyList<INode> Children => [inner];

    internal override NodeKind Kind => NodeKind.Cut;

    internal override Plan<char, T> Lower(PlanBuilder builder) => new CutThenPlan<T>(builder.Lower(inner));
}

internal class CutThenPlan<T>(Plan<char, T> inner) : Plan<char, T>
{
    public override bool TryParse(ref Cursor<char> cursor, ref T result)
    {
        cursor.Cut = true;
        return inner.TryParse(ref cursor, ref result);
    }
}
