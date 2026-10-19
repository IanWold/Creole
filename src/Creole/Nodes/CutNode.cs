namespace Creole;

/// <summary>
/// Succeeds without consuming input and puts a cut in force until the parse leaves the nearest cut boundary (ADR-0008).
/// Backs <c>Cut</c>.
/// </summary>
internal sealed class CutNode<TIn> : Parser<TIn, Unit>
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Cut;

    internal override Plan<TIn, Unit> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, Unit>(Kind);
}
