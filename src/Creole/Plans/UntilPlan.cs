namespace Creole;

/// <summary>
/// Tries its terminator, and while that fails without consuming, runs one item. Produces the items in order. Each terminator and each item is one attempt.
/// </summary>
/// <remarks>
/// A terminator that succeeds ends the repetition, consumed.
/// An item that fails, with or without consuming, fails the repetition, as does a terminator that fails after consuming.
/// </remarks>
internal sealed class UntilPlan<TIn, T, TTerm>(Plan<TIn, T> item, Plan<TIn, TTerm> terminator) : Plan<TIn, IReadOnlyList<T>>
{
    private readonly Plan<TIn, T> _item = item;
    private readonly Plan<TIn, TTerm> _terminator = terminator;

    public override bool TryParse(ref Cursor<TIn> cursor, ref IReadOnlyList<T> result)
    {
        List<T>? items = null;
        TTerm terminator = default!;

        while (true)
        {
            var mark = LightMark.Take(ref cursor);

            if (_terminator.TryParse(ref cursor, ref terminator))
            {
                result = items ?? [];
                return true;
            }

            if (!mark.FailedEmpty(ref cursor))
            {
                return false;
            }

            T value = default!;

            if (!_item.TryParse(ref cursor, ref value))
            {
                mark.FailedEmpty(ref cursor);
                return false;
            }

            if (cursor.Position == mark.Position)
            {
                throw NullableRepetition.Item();
            }

            (items ??= []).Add(value);
        }
    }
}
