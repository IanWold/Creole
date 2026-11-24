namespace Creole;

/// <summary>
/// Succeeds without consuming input and produces the layer's state.
/// Backs <c>GetState</c>.
/// </summary>
internal sealed class GetStateNode<TIn, TState> : Parser<TIn, TState>
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.GetState;

    /// <summary>
    /// The one node for this input and state type, which every use of <c>GetState</c> shares.
    /// </summary>
    public static GetStateNode<TIn, TState> Instance { get; } = new();

    internal override Plan<TIn, TState> Lower(PlanBuilder builder) =>
        PlanBuilder.GetState<TIn, TState>();
}
