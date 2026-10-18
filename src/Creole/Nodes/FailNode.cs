namespace Creole.Nodes;

/// <summary>
/// Fails without consuming input, reporting a custom message.
/// </summary>
internal sealed class FailNode<TIn, TOut>(string message) : Parser<TIn, TOut>
{
    public string Message { get; } = Guard.NotNull(message);

    internal override NodeKind Kind => NodeKind.Fail;

    internal override IReadOnlyList<INode> Children => [];
}
