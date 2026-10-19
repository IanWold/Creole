namespace Creole;

/// <summary>
/// Selective choice: runs <see cref="Scrutinee"/>, then the branch whose key equals its result, or <see cref="Default"/>. Backs <c>Switch</c>.
/// </summary>
internal sealed class SwitchNode<TIn, TKey, TOut> : Parser<TIn, TOut>
{
    private readonly INode[] _children;

    /// <summary>
    /// The scrutinee, then each case's branch in order, then the default branch.
    /// </summary>
    internal override IReadOnlyList<INode> Children => _children;

    internal override NodeKind Kind => NodeKind.Switch;

    /// <summary>
    /// The branch of each case, in the same order as <see cref="Keys"/>.
    /// </summary>
    public IReadOnlyList<Parser<TIn, TOut>> Branches { get; }

    public Parser<TIn, TOut> Default { get; }

    /// <summary>
    /// The key of each case, distinct under <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    public IReadOnlyList<TKey> Keys { get; }

    public Parser<TIn, TKey> Scrutinee { get; }

    /// <param name="scrutinee">The parser whose result picks the branch.</param>
    /// <param name="keys">The key of each case. The node keeps this array, so pass a new one.</param>
    /// <param name="branches">The branch of each case, in the same order as <paramref name="keys"/>. The node keeps this array, so pass a new one.</param>
    /// <param name="default">The branch for a result that matches no key.</param>
    public SwitchNode(Parser<TIn, TKey> scrutinee, TKey[] keys, Parser<TIn, TOut>[] branches, Parser<TIn, TOut> @default)
    {
        Scrutinee = scrutinee;
        Keys = keys;
        Branches = branches;
        Default = @default;
        _children = [scrutinee, .. branches, @default];
    }

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, TOut>(Kind);
}
