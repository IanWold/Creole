namespace Creole;

/// <summary>
/// Runs its item, then pairs of separator and item, and produces the items in order. Each item and each separator is one attempt.
/// </summary>
/// <remarks>
/// <para>
/// A first item that fails without consuming gives an empty list, or a failure if <c>min</c> is 1.
/// A separator that fails without consuming ends the list.
/// An item after a separator that fails without consuming ends the list if trailing separators are allowed or the separator consumed nothing, and otherwise fails it.
/// Any attempt that fails after consuming fails the list.
/// </para>
/// <para>
/// When the list ends after a separator, only the item's failed attempt is undone: the separator stays consumed, and a cut it passed stays in force (ADR-0011).
/// </para>
/// </remarks>
internal sealed class SeparatedPlan<TIn, T, TSep>(Plan<TIn, T> item, Plan<TIn, TSep> separator, int min, bool allowTrailing) : Plan<TIn, IReadOnlyList<T>>
{
    private readonly bool _allowTrailing = allowTrailing;
    private readonly Plan<TIn, T> _item = item;
    private readonly int _min = min;
    private readonly Plan<TIn, TSep> _separator = separator;

    public override bool TryParse(ref Cursor<TIn> cursor, ref IReadOnlyList<T> result)
    {
        var mark = LightMark.Take(ref cursor);
        T value = default!;

        if (!_item.TryParse(ref cursor, ref value))
        {
            if (!mark.FailedEmpty(ref cursor) || _min > 0)
            {
                return false;
            }

            result = [];
            return true;
        }

        List<T> items = [];
        items.Add(value);
        TSep separator = default!;

        while (true)
        {
            var round = LightMark.Take(ref cursor);

            if (!_separator.TryParse(ref cursor, ref separator))
            {
                if (!round.FailedEmpty(ref cursor))
                {
                    return false;
                }

                break;
            }

            var itemMark = LightMark.Take(ref cursor);

            if (!_item.TryParse(ref cursor, ref value))
            {
                if (!itemMark.FailedEmpty(ref cursor) || !(_allowTrailing || cursor.Position == round.Position))
                {
                    return false;
                }

                break;
            }

            if (cursor.Position == round.Position)
            {
                throw NullableRepetition.ItemAndSeparator();
            }

            items.Add(value);
        }

        result = items;
        return true;
    }
}
