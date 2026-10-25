namespace Creole;

/// <summary>
/// Fails without consuming input, recording its message.
/// </summary>
internal sealed class FailPlan<TIn, TOut>(string message) : Plan<TIn, TOut>
{
    private readonly string _message = message;

    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        cursor.FailWith(_message);
        return false;
    }
}
