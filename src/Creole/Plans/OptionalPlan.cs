namespace Creole;

/// <summary>
/// Runs its item as one attempt. Produces <see cref="Nothing"/> if the item fails without consuming, and fails if it fails after consuming.
/// </summary>
internal class OptionalPlan<TIn, T, TMark>(Plan<TIn, T> item) : Plan<TIn, Maybe<T>>
    where TMark : struct, IMark<TMark>
{
    private readonly Plan<TIn, T> _item = item;

    public override bool TryParse(ref Cursor<TIn> cursor, ref Maybe<T> result)
    {
        var mark = TMark.Take(ref cursor);
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
