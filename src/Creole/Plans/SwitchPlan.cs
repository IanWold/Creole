namespace Creole;

/// <summary>
/// Runs its scrutinee, then the branch whose key equals the result, or the default branch if none does.
/// Fails if the scrutinee or the branch fails; the failure is committed if anything consumed input.
/// </summary>
/// <remarks>
/// Keys are compared in order with <see cref="EqualityComparer{T}.Default"/>; the node rejects duplicate keys, so the order does not change the result.
/// </remarks>
internal sealed class SwitchPlan<TIn, TKey, TOut>(Plan<TIn, TKey> scrutinee, TKey[] keys, Plan<TIn, TOut>[] branches, Plan<TIn, TOut> @default) : Plan<TIn, TOut>
{
    private readonly Plan<TIn, TOut>[] _branches = branches;
    private readonly Plan<TIn, TOut> _default = @default;
    private readonly TKey[] _keys = keys;
    private readonly Plan<TIn, TKey> _scrutinee = scrutinee;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        TKey key = default!;

        if (!_scrutinee.TryParse(ref cursor, ref key))
        {
            return false;
        }

        for (var i = 0; i < _keys.Length; i++)
        {
            if (EqualityComparer<TKey>.Default.Equals(_keys[i], key))
            {
                return _branches[i].TryParse(ref cursor, ref result);
            }
        }

        return _default.TryParse(ref cursor, ref result);
    }
}
