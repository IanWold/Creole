namespace Creole;

/// <summary>
/// Consumes one token whose kind is <see cref="TokenKind"/>, and produces the token.
/// Fails without consuming on a token of any other kind.
/// </summary>
internal sealed class KindNode<TToken, TKind>(TKind kind) : Parser<TToken, TToken>, IPrimitiveNode
    where TToken : IKinded<TKind>
    where TKind : struct, Enum
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Kind;

    public FirstSet First { get; } = FirstSet.OfItem(kind);

    public bool Nullable => false;

    public bool Opaque => false;

    public TKind TokenKind { get; } = kind;

    internal override Plan<TToken, TToken> Lower(PlanBuilder builder) =>
        PlanBuilder.Kind<TToken, TKind>(TokenKind);
}
