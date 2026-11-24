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

/// <summary>
/// Raises a diagnostic that reports <paramref name="message"/> where it is, and succeeds without consuming. Produces the empty string.
/// </summary>
/// <remarks>
/// It reports itself as a <c>Return</c>. Lowering cannot see that it raises diagnostics, so a grammar that holds it must be lowered without <see cref="LoweringOptions.LightMarks"/>.
/// </remarks>
internal class RaiseNode(string message) : Parser<char, string>
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Return;

    internal override Plan<char, string> Lower(PlanBuilder builder) => new RaisePlan(message);
}

internal class RaisePlan(string message) : Plan<char, string>
{
    public override bool TryParse(ref Cursor<char> cursor, ref string result)
    {
        cursor.Failures?.Raise(cursor.Position, message);
        result = "";
        return true;
    }
}

/// <summary>
/// Runs <paramref name="item"/>, and if it fails, reports the furthest failure as a diagnostic and succeeds where the item left the cursor, as a <c>Recover</c> that skips nothing would.
/// </summary>
/// <remarks>
/// Lowering cannot see that it raises diagnostics, so a grammar that holds it must be lowered without <see cref="LoweringOptions.LightMarks"/>.
/// </remarks>
internal class ReportNode<T>(Parser<char, T> item) : Parser<char, Maybe<T>>
{
    internal override IReadOnlyList<INode> Children => [item];

    internal override NodeKind Kind => NodeKind.Return;

    internal override Plan<char, Maybe<T>> Lower(PlanBuilder builder) => new ReportPlan<T>(builder.Lower(item));
}

internal class ReportPlan<T>(Plan<char, T> item) : Plan<char, Maybe<T>>
{
    public override bool TryParse(ref Cursor<char> cursor, ref Maybe<T> result)
    {
        T value = default!;

        if (item.TryParse(ref cursor, ref value))
        {
            result = value;
            return true;
        }

        cursor.Failures?.RaiseFromFurthestFailure(cursor.Position);
        result = default;
        return true;
    }
}
