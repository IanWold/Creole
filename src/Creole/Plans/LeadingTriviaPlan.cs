namespace Creole;

/// <summary>
/// Succeeds without consuming and produces the source between the end of the item before the cursor and the start of the next item, or the end of the input.
/// </summary>
internal class LeadingTriviaPlan<TIn> : Plan<TIn, SourceSpan>
{
    public override bool TryParse(ref Cursor<TIn> cursor, ref SourceSpan result)
    {
        result = cursor.LeadingTrivia();
        return true;
    }
}
