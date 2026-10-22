namespace Creole;

/// <summary>
/// Runs its item and produces the characters it consumed as a new string.
/// </summary>
internal sealed class TextPlan<T>(Plan<char, T> item) : Plan<char, string>
{
    private readonly Plan<char, T> _item = item;

    public override bool TryParse(ref Cursor<char> cursor, ref string result)
    {
        var start = cursor.Position;
        T value = default!;

        if (!_item.TryParse(ref cursor, ref value))
        {
            return false;
        }

        result = cursor.Since(start).ToString();
        return true;
    }
}
