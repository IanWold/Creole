namespace Creole;

/// <summary>
/// A <c>Match</c> case for union values of type <typeparamref name="TCase"/>: runs <see cref="Branch"/>, then <see cref="Combine"/> on the case value and the branch's result.
/// </summary>
internal sealed class TypedMatchCase<TIn, TCase, TBranch, TOut>(Parser<TIn, TBranch> branch, Func<TCase, TBranch, TOut> combine) : MatchCase<TIn, TOut>
{
    internal override INode BranchNode => Branch;

    internal override Type CaseType => typeof(TCase);

    public Parser<TIn, TBranch> Branch { get; } = branch;

    public Func<TCase, TBranch, TOut> Combine { get; } = combine;
}
