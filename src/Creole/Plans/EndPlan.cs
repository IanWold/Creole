namespace Creole;

/// <summary>
/// Succeeds at the end of the input, and otherwise fails, recording that it expected the end. Never consumes.
/// </summary>
/// <remarks>
/// Where the input has run out of what it has received, it has not ended, so the attempt is starved and this fails, recording nothing.
/// </remarks>
internal sealed class EndPlan<TIn> : Plan<TIn, Unit>
{
    public override bool TryParse(ref Cursor<TIn> cursor, ref Unit result)
    {
        if (cursor.IsAtEnd)
        {
            return !cursor.Starved;
        }

        cursor.Fail(Expected.End);
        return false;
    }
}
