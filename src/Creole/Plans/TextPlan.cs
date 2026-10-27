namespace Creole;

/// <summary>
/// Runs its item and produces the characters it consumed as a new string.
/// </summary>
/// <remarks>
/// Its start is pinned while it runs, so a stream buffer keeps the characters it reads back.
/// </remarks>
internal sealed class TextPlan<T>(Plan<char, T> item) : Plan<char, string>
{
    private readonly Plan<char, T> _item = item;

    public override bool TryParse(ref Cursor<char> cursor, ref string result)
    {
        var start = cursor.Position;
        var floor = cursor.Pin();
        T value = default!;

        if (!_item.TryParse(ref cursor, ref value))
        {
            cursor.Unpin(floor);
            return false;
        }

        result = cursor.Since(start).ToString();
        cursor.Unpin(floor);
        return true;
    }
}
