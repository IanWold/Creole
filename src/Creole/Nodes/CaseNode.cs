using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Consumes one union token whose case is <typeparamref name="TCase"/>, and produces that case's value.
/// Fails without consuming on a token of any other case.
/// </summary>
/// <remarks>
/// Analysis treats the case type as a kind: the FIRST set holds <typeparamref name="TCase"/>.
/// </remarks>
internal sealed class CaseNode<TToken, TCase> : Parser<TToken, TCase>, IPrimitiveNode
{
    internal override IReadOnlyList<INode> Children => [];

    internal override NodeKind Kind => NodeKind.Case;

    public Expected Expected { get; } = Expected.UnionCase<TCase>();

    public FirstSet First { get; } = FirstSet.OfCase(typeof(TCase));

    public bool Nullable => false;

    public bool Opaque => false;

    /// <exception cref="GrammarException"><typeparamref name="TToken"/> is not a union.</exception>
    public CaseNode()
    {
        if (!typeof(TToken).IsAssignableTo(typeof(IUnion)))
        {
            throw new GrammarException($"Case<{typeof(TCase).Name}>() needs a union token type, but {typeof(TToken).Name} is not a union. Use Kind(k) to match tokens by kind.");
        }
    }

    internal override Plan<TToken, TCase> Lower(PlanBuilder builder) =>
        PlanBuilder.Case<TToken, TCase>(Expected);
}
