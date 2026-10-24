namespace Creole;

/// <summary>
/// Runs a <c>Rec</c>'s target one level of recursion deeper.
/// </summary>
/// <remarks>
/// Where the <c>Rec</c> closes a cycle, the target is a <see cref="ForwardPlan{TIn, TOut}"/>.
/// </remarks>
internal sealed class RecPlan<TIn, TOut>(Plan<TIn, TOut> target) : Plan<TIn, TOut>
{
    /// <summary>
    /// The plan of the <c>Rec</c>'s target.
    /// </summary>
    public Plan<TIn, TOut> Target => target;

    /// <exception cref="DepthExceededException">The parse is too deep to enter another level.</exception>
    public override bool TryParse(ref Cursor<TIn> cursor, ref TOut result)
    {
        cursor.EnterLevel();
        var ok = target.TryParse(ref cursor, ref result);
        cursor.LeaveLevel();

        return ok;
    }
}
