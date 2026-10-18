using Creole.Plans;

namespace Creole.Nodes;

/// <summary>
/// Succeeds without consuming input and produces a fixed value.
/// </summary>
internal sealed class ReturnNode<TIn, TOut>(TOut value) : Parser<TIn, TOut>
{
    public TOut Value { get; } = value;

    internal override NodeKind Kind => NodeKind.Return;

    internal override IReadOnlyList<INode> Children => [];

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) => PlanBuilder.Return<TIn, TOut>(Value);
}
