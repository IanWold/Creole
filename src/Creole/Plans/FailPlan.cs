namespace Creole;

/// <summary>
/// Fails without consuming input. The node's message is reported once the error model exists (ERR-02).
/// </summary>
internal sealed class FailPlan<TIn, TOut> : Plan<TIn, TOut>
{
    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result) => false;
}
