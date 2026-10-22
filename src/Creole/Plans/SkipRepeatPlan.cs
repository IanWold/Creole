namespace Creole;

/// <summary>
/// Runs its item as <see cref="RepeatPlan{TIn, T}"/> does, but keeps none of the results, so it allocates nothing.
/// </summary>
internal sealed class SkipRepeatPlan<TIn, T>(Plan<TIn, T> item, int min, int? max) : Plan<TIn, Unit>
{
    private readonly Plan<TIn, T> _item = item;
    private readonly int? _max = max;
    private readonly int _min = min;

    public override bool TryParse(ref Cursor<TIn> cursor, ref Unit result)
    {
        T value = default!;

        for (var count = 0; _max is not { } max || count < max; count++)
        {
            var mark = LightMark.Take(ref cursor);

            if (!_item.TryParse(ref cursor, ref value))
            {
                return mark.FailedEmpty(ref cursor) && count >= _min;
            }

            if (_max is null && cursor.Position == mark.Position)
            {
                throw NullableRepetition.Item();
            }
        }

        return true;
    }
}
