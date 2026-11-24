namespace Creole;

/// <summary>
/// Succeeds without consuming input and produces a zero-width span where the next item starts, or at the end of the last item at the end of the input.
/// Backs <c>Position</c>.
/// </summary>
internal sealed class PositionNode<TIn> : Parser<TIn, SourceSpan>
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Position;

    internal override Plan<TIn, SourceSpan> Lower(PlanBuilder builder) =>
        PlanBuilder.Position<TIn>();
}
