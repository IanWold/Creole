namespace Creole;

/// <summary>
/// Succeeds without consuming input and replaces the layer's state with <see cref="State"/>.
/// Backs <c>SetState</c>.
/// </summary>
internal class SetStateNode<TIn, TState>(TState state) : Parser<TIn, Unit>
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.SetState;

    public TState State { get; } = state;

    internal override Plan<TIn, Unit> Lower(PlanBuilder builder) =>
        PlanBuilder.SetState<TIn>(StateBox.Hold(State));
}
