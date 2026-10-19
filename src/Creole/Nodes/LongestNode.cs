namespace Creole;

/// <summary>
/// Longest match over <see cref="Alternatives"/>: every alternative runs atomically from the same position, the one that consumed the most input wins, and a tie goes to the earliest. Backs <c>Longest</c>.
/// </summary>
/// <param name="alternatives">The alternatives, in order of priority for ties. The node keeps this array, so pass a new one.</param>
/// <remarks>
/// Engines compile the alternatives to a single DFA when all of them are regular, and run them one by one otherwise.
/// </remarks>
internal sealed class LongestNode<TIn, T>(Parser<TIn, T>[] alternatives) : Parser<TIn, T>
{
    /// <summary>
    /// The alternatives, in order of priority for ties.
    /// </summary>
    internal override IReadOnlyList<INode> Children => Alternatives;

    internal override NodeKind Kind => NodeKind.Longest;

    /// <summary>
    /// The alternatives, in order of priority for ties.
    /// </summary>
    public IReadOnlyList<Parser<TIn, T>> Alternatives { get; } = alternatives;

    internal override Plan<TIn, T> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, T>(Kind);
}
