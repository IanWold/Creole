namespace Creole;

/// <summary>
/// Runs its item and produces the characters it consumed, as a view of the input where the cursor can give one (<see cref="Cursor{T}.Since"/>).
/// </summary>
internal sealed class SlicePlan<T>(Plan<char, T> item) : Plan<char, ReadOnlyMemory<char>>
{
    private readonly Plan<char, T> _item = item;

    public override bool TryParse(ref Cursor<char> cursor, ref ReadOnlyMemory<char> result)
    {
        var start = cursor.Position;
        T value = default!;

        if (!_item.TryParse(ref cursor, ref value))
        {
            return false;
        }

        result = cursor.Since(start);
        return true;
    }
}
