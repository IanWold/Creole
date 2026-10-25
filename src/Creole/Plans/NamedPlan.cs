namespace Creole;

/// <summary>
/// Runs its item and, when it consumed nothing, replaces the expectations it recorded where it started with its label.
/// </summary>
/// <remarks>
/// Expectations recorded there before the item ran stay, ahead of the label. A failure that expected nothing in particular counts as recorded.
/// If the item consumed input, or what it recorded there has been superseded, nothing changes.
/// </remarks>
internal sealed class NamedPlan<TIn, T>(Plan<TIn, T> item, Expected label) : Plan<TIn, T>
{
    private readonly Expected _label = label;

    /// <summary>
    /// The plan of the parser the label names.
    /// </summary>
    public Plan<TIn, T> Item { get; } = item;

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        var start = cursor.Position;
        var note = cursor.NoteFailures();
        var ok = Item.TryParse(ref cursor, ref result);

        if (cursor.Position == start)
        {
            cursor.Relabel(in note, start, _label);
        }

        return ok;
    }
}
