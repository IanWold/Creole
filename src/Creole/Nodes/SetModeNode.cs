namespace Creole;

/// <summary>
/// Succeeds without consuming input and replaces the top of <see cref="Channel"/>'s stack with <see cref="Mode"/>.
/// Backs <c>SetMode</c>.
/// </summary>
internal class SetModeNode<TIn, TMode>(ModeChannel<TMode> channel, TMode mode) : Parser<TIn, Unit>
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.SetMode;

    public ModeChannel<TMode> Channel { get; } = channel;

    public TMode Mode { get; } = mode;

    internal override Plan<TIn, Unit> Lower(PlanBuilder builder) =>
        PlanBuilder.SetMode<TIn, TMode>(Channel, Mode);
}
