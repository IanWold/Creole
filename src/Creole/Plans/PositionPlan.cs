namespace Creole;

/// <summary>
/// Succeeds without consuming and produces a zero-width span where the next item starts, or at the end of the last item at the end of the input.
/// </summary>
internal class PositionPlan<TIn> : Plan<TIn, SourceSpan>
{
    public override bool TryParse(ref Cursor<TIn> cursor, ref SourceSpan result)
    {
        result = cursor.SpanSince(cursor.Position);
        return true;
    }
}
