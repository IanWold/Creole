namespace Creole;

/// <summary>
/// Succeeds at the end of the input, and otherwise fails, recording that it expected the end. Never consumes.
/// </summary>
internal sealed class EndPlan<TIn> : Plan<TIn, Unit>
{
    public override bool TryParse(ref Cursor<TIn> cursor, ref Unit result)
    {
        if (cursor.IsAtEnd)
        {
            return true;
        }

        cursor.Fail(Expected.End);
        return false;
    }
}
