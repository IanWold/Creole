namespace Creole;

/// <summary>
/// Runs its item inside a context, so failures recorded past where it started carry the context's text.
/// </summary>
internal sealed class ContextPlan<TIn, T>(Plan<TIn, T> item, string text) : Plan<TIn, T>
{
    private readonly Plan<TIn, T> _item = item;
    private readonly string _text = text;

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result)
    {
        cursor.EnterContext(_text);

        var ok = _item.TryParse(ref cursor, ref result);

        cursor.LeaveContext();
        return ok;
    }
}
