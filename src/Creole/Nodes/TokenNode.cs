namespace Creole;

/// <summary>
/// Consumes one item equal to <see cref="Value"/>, by <see cref="EqualityComparer{T}.Default"/>, and produces the item read.
/// Fails without consuming on any other item.
/// </summary>
internal sealed class TokenNode<TIn>(TIn value) : Parser<TIn, TIn>, IPrimitiveNode
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Token;

    public FirstSet First { get; } = FirstSet.OfItem(value);

    public bool Nullable => false;

    public bool Opaque => false;

    public TIn Value { get; } = value;

    internal override Plan<TIn, TIn> Lower(PlanBuilder builder) =>
        PlanBuilder.Token(Value);
}
