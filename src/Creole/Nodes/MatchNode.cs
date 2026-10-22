using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Selective choice over a union: runs <see cref="Scrutinee"/>, then the first case whose type the union's value has. Backs <c>Match</c>.
/// </summary>
/// <param name="scrutinee">The parser that produces the union value.</param>
/// <param name="cases">The cases in the order they are tried. The node keeps this array, so pass a new one.</param>
internal sealed class MatchNode<TIn, TUnion, TOut>(Parser<TIn, TUnion> scrutinee, MatchCase<TIn, TOut>[] cases) : Parser<TIn, TOut>, IMatchNode
    where TUnion : IUnion
{
    private readonly INode[] _children = [scrutinee, .. cases.Select(c => c.BranchNode)];
    private readonly MatchCaseInfo[] _view = [.. cases.Select(c => new MatchCaseInfo(c.CaseType, c.BranchNode))];

    /// <summary>
    /// The scrutinee, then each case's branch in order.
    /// </summary>
    internal override IReadOnlyList<INode> Children => _children;

    internal override NodeKind Kind => NodeKind.Match;

    /// <summary>
    /// The cases, in the order they are tried.
    /// </summary>
    public IReadOnlyList<MatchCase<TIn, TOut>> Cases { get; } = cases;

    IReadOnlyList<MatchCaseInfo> IMatchNode.Cases => _view;

    public Parser<TIn, TUnion> Scrutinee { get; } = scrutinee;

    INode IMatchNode.Scrutinee => Scrutinee;

    Type IMatchNode.UnionType => typeof(TUnion);

    internal override Plan<TIn, TOut> Lower(PlanBuilder builder) =>
        builder.Match(Scrutinee, Cases);
}
