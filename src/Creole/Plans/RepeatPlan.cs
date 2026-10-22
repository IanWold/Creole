namespace Creole;

/// <summary>
/// Runs its item at least <c>min</c> and at most <c>max</c> times, each as one attempt, and produces the results in order.
/// </summary>
/// <remarks>
/// An attempt that fails without consuming ends the repetition, which then succeeds if it has <c>min</c> items.
/// An attempt that fails after consuming fails the repetition.
/// </remarks>
internal sealed class RepeatPlan<TIn, T>(Plan<TIn, T> item, int min, int? max) : Plan<TIn, IReadOnlyList<T>>
{
    private readonly Plan<TIn, T> _item = item;
    private readonly int? _max = max;
    private readonly int _min = min;

    public override bool TryParse(ref Cursor<TIn> cursor, ref IReadOnlyList<T> result)
    {
        List<T>? items = null;

        for (var count = 0; _max is not { } max || count < max; count++)
        {
            var mark = LightMark.Take(ref cursor);
            T value = default!;

            if (!_item.TryParse(ref cursor, ref value))
            {
                if (!mark.FailedEmpty(ref cursor) || count < _min)
                {
                    return false;
                }

                break;
            }

            if (_max is null && cursor.Position == mark.Position)
            {
                throw NullableRepetition.Item();
            }

            (items ??= []).Add(value);
        }

        result = items ?? [];
        return true;
    }
}
