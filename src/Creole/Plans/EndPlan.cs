namespace Creole;

/// <summary>
/// Succeeds at the end of the input, and otherwise fails. Never consumes.
/// </summary>
internal sealed class EndPlan<TIn> : Plan<TIn, Unit>
{
    public override bool TryParse(ref Cursor<TIn> cursor, ref Unit result) => cursor.IsAtEnd;
}
