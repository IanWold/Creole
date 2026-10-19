namespace Creole;

/// <summary>
/// Succeeds without consuming input and produces a fixed value.
/// </summary>
internal sealed class ReturnNode<TIn, TOut>(TOut value) : Parser<TIn, TOut>
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Return;

    public TOut Value { get; } = value;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) => PlanBuilder.Return<TIn, TOut>(Value);
}
