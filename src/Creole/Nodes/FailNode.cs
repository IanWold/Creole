namespace Creole;

/// <summary>
/// Fails without consuming input, reporting a custom message.
/// </summary>
internal sealed class FailNode<TIn, TOut>(string message) : Parser<TIn, TOut>
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Fail;

    public string Message { get; } = Guard.NotNull(message);

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.Fail<TIn, TOut>(Message);
}
