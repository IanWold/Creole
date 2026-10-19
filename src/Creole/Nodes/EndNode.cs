namespace Creole;

/// <summary>
/// Succeeds without consuming input at the end of input, and fails without consuming anywhere else.
/// </summary>
internal sealed class EndNode<TIn> : Parser<TIn, Unit>, IPrimitiveNode
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.End;

    public FirstSet First => FirstSet.EndOfInput;

    public bool Nullable => true;

    public bool Opaque => false;

    internal override Plan<TIn, Unit> Lower(PlanBuilder builder) => PlanBuilder.NotYetExecutable<TIn, Unit>(Kind);
}
