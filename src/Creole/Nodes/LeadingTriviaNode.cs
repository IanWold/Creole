namespace Creole;

/// <summary>
/// Succeeds without consuming input and produces the source between the end of the item before the cursor and the start of the next item, or the end of the input.
/// Backs <c>LeadingTrivia</c>.
/// </summary>
internal sealed class LeadingTriviaNode<TIn> : Parser<TIn, SourceSpan>
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.LeadingTrivia;

    internal override Plan<TIn, SourceSpan> Lower(PlanBuilder builder) =>
        PlanBuilder.LeadingTrivia<TIn>();
}
