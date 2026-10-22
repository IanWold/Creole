namespace Creole;

/// <summary>
/// Selective choice: runs <see cref="Scrutinee"/>, then the branch whose key equals its result, or <see cref="Default"/>. Backs <c>Switch</c>.
/// </summary>
/// <param name="scrutinee">The parser whose result picks the branch.</param>
/// <param name="keys">The key of each case. The node keeps this array, so pass a new one.</param>
/// <param name="branches">The branch of each case, in the same order as <paramref name="keys"/>. The node keeps this array, so pass a new one.</param>
/// <param name="default">The branch for a result that matches no key.</param>
internal sealed class SwitchNode<TIn, TKey, TOut>(Parser<TIn, TKey> scrutinee, TKey[] keys, Parser<TIn, TOut>[] branches, Parser<TIn, TOut> @default) : Parser<TIn, TOut>
{
    private readonly INode[] _children = [scrutinee, .. branches, @default];

    /// <summary>
    /// The scrutinee, then each case's branch in order, then the default branch.
    /// </summary>
    internal override IReadOnlyList<INode> Children => _children;

    internal override NodeKind Kind => NodeKind.Switch;

    /// <summary>
    /// The branch of each case, in the same order as <see cref="Keys"/>.
    /// </summary>
    public IReadOnlyList<Parser<TIn, TOut>> Branches { get; } = branches;

    public Parser<TIn, TOut> Default { get; } = @default;

    /// <summary>
    /// The key of each case, distinct under <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    public IReadOnlyList<TKey> Keys { get; } = keys;

    public Parser<TIn, TKey> Scrutinee { get; } = scrutinee;

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        builder.Switch(Scrutinee, Keys, Branches, Default);
}
