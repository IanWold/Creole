using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Runs its scrutinee, then the first case whose type the union's value has.
/// Fails if the scrutinee fails, if no case applies, or if the case's branch fails; the failure is committed if anything consumed input.
/// When no case applies, it records a failure that expects nothing in particular, where the branch would have started.
/// </summary>
internal sealed class MatchPlan<TIn, TUnion, TOut>(Plan<TIn, TUnion> scrutinee, MatchCasePlan<TIn, TOut>[] cases) : Plan<TIn, TOut>
    where TUnion : IUnion
{
    private readonly MatchCasePlan<TIn, TOut>[] _cases = cases;
    private readonly Plan<TIn, TUnion> _scrutinee = scrutinee;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        TUnion union = default!;

        if (!_scrutinee.TryParse(ref cursor, ref union))
        {
            return false;
        }

        var value = union.Value;

        foreach (var @case in _cases)
        {
            if (@case.Applies(value))
            {
                return @case.TryParse(value, ref cursor, ref result);
            }
        }

        cursor.Fail(null);
        return false;
    }
}
