namespace Creole;

/// <summary>
/// Runs its item and produces the characters it consumed: a view of in-memory input, and a copy of streamed input (<see cref="Cursor{T}.Since"/>, ADR-0013).
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
