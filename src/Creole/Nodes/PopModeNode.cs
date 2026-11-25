namespace Creole;

/// <summary>
/// Succeeds without consuming input and pops the top of <see cref="Channel"/>'s stack.
/// Backs <c>PopMode</c>.
/// </summary>
internal sealed class PopModeNode<TIn, TMode>(ModeChannel<TMode> channel) : Parser<TIn, Unit>
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.PopMode;

    public ModeChannel<TMode> Channel { get; } = channel;

    internal override Plan<TIn, Unit> Lower(PlanBuilder builder) =>
        PlanBuilder.PopMode<TIn, TMode>(Channel);
}
