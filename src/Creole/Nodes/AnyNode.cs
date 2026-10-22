namespace Creole;

/// <summary>
/// Consumes any one item and produces it. Fails without consuming at the end of input.
/// </summary>
internal sealed class AnyNode<TIn> : Parser<TIn, TIn>, IPrimitiveNode
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Any;

    public FirstSet First => FirstSet.AnyItem;

    public bool Nullable => false;

    public bool Opaque => false;

    internal override Plan<TIn, TIn> Lower(PlanBuilder builder) =>
        PlanBuilder.Any<TIn>();
}
