namespace Creole;

/// <summary>
/// Consumes one item for which <see cref="Predicate"/> returns <see langword="true"/>, and produces it.
/// Fails without consuming on any other item.
/// </summary>
/// <remarks>
/// The predicate is code, so analysis cannot see which items it accepts. The node is opaque and its FIRST set is any item.
/// </remarks>
internal sealed class SatisfyNode<TIn>(Func<TIn, bool> predicate) : Parser<TIn, TIn>, IPrimitiveNode
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Satisfy;

    public FirstSet First => FirstSet.AnyItem;

    public bool Nullable => false;

    public bool Opaque => true;

    public Func<TIn, bool> Predicate { get; } = Guard.NotNull(predicate);

    internal override Plan<TIn, TIn> Lower(PlanBuilder builder) =>
        PlanBuilder.Satisfy(Predicate);
}
