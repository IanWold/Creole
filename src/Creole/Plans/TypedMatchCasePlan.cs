namespace Creole;

/// <summary>
/// The lowered form of a <see cref="TypedMatchCase{TIn, TCase, TBranch, TOut}"/>.
/// </summary>
internal sealed class TypedMatchCasePlan<TIn, TCase, TBranch, TOut>(Plan<TIn, TBranch> branch, Func<TCase, TBranch, TOut> combine) : MatchCasePlan<TIn, TOut>
{
    private readonly Plan<TIn, TBranch> _branch = branch;
    private readonly Func<TCase, TBranch, TOut> _combine = combine;

    public override bool Applies(object? value) => value is TCase;

    public override bool TryParse(object? value, ref Cursor<TIn> cursor, ref TOut result)
    {
        TBranch branch = default!;

        if (!_branch.TryParse(ref cursor, ref branch))
        {
            return false;
        }

        result = _combine((TCase)value!, branch);
        return true;
    }
}
