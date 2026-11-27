namespace Creole;

/// <summary>
/// Runs <see cref="First"/>, then <see cref="Second"/>, and produces both results, if the second's span starts where the first's ends.
/// Backs <c>Adjacent</c>.
/// </summary>
/// <remarks>
/// When the spans do not touch, it fails as a rejected filter over the pair: committed if either consumed input, and reported where the first started.
/// </remarks>
internal class AdjacentNode<TIn, T1, T2>(Parser<TIn, T1> first, Parser<TIn, T2> second) : Parser<TIn, (T1, T2)>
{
    internal override IReadOnlyList<INode> Children => [First, Second];

    internal override NodeKind Kind => NodeKind.Adjacent;

    public Parser<TIn, T1> First { get; } = first;

    public Parser<TIn, T2> Second { get; } = second;

    internal override Plan<TIn, (T1, T2)> Lower(PlanBuilder builder) =>
        builder.Adjacent(First, Second);
}
