namespace Creole;

/// <summary>
/// Succeeds without consuming input and pushes <see cref="Mode"/> onto <see cref="Channel"/>'s stack.
/// Backs <c>PushMode</c>.
/// </summary>
internal sealed class PushModeNode<TIn, TMode>(ModeChannel<TMode> channel, TMode mode) : Parser<TIn, Unit>
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.PushMode;

    public ModeChannel<TMode> Channel { get; } = channel;

    public TMode Mode { get; } = mode;

    internal override Plan<TIn, Unit> Lower(PlanBuilder builder) =>
        PlanBuilder.PushMode<TIn, TMode>(Channel, Mode);
}
