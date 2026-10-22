namespace Creole;

/// <summary>
/// Runs its item as one attempt. Produces <see cref="Nothing"/> if the item fails without consuming, and fails if it fails after consuming.
/// </summary>
internal sealed class OptionalPlan<TIn, T>(Plan<TIn, T> item) : Plan<TIn, Maybe<T>>
{
    private readonly Plan<TIn, T> _item = item;

    public override bool TryParse(ref Cursor<TIn> cursor, ref Maybe<T> result)
    {
        var mark = LightMark.Take(ref cursor);
        T value = default!;

        if (_item.TryParse(ref cursor, ref value))
        {
            result = value;
            return true;
        }

        if (!mark.FailedEmpty(ref cursor))
        {
            return false;
        }

        result = default;
        return true;
    }
}
