namespace Creole;

/// <summary>
/// Ordered choice with commit semantics over <see cref="Alternatives"/>. Backs <c>Or</c> and <c>OneOf</c>.
/// </summary>
/// <param name="alternatives">The alternatives in the order they are tried. The node keeps this array, so pass a new one.</param>
internal sealed class OrNode<TIn, T>(Parser<TIn, T>[] alternatives) : Parser<TIn, T>
{
    /// <summary>
    /// The alternatives, in the order they are tried.
    /// </summary>
    internal override IReadOnlyList<INode> Children => Alternatives;

    internal override NodeKind Kind => NodeKind.Or;

    /// <summary>
    /// The alternatives, in the order they are tried.
    /// </summary>
    public IReadOnlyList<Parser<TIn, T>> Alternatives { get; } = alternatives;

    internal override Plan<TIn, T> Lower(PlanBuilder builder) =>
        builder.Or(Alternatives);
}
