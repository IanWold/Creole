namespace Creole;

/// <summary>
/// What a combinator saves before one attempt: the position and the cut flag (§ Bookmarks and retention).
/// </summary>
/// <remarks>
/// An attempt is each alternative of a choice, the item of <c>Optional</c>, each repetition item, separator and terminator.
/// When the attempt fails without consuming, the mark restores the cut flag, so a cut the attempt passed is dropped with it (ADR-0011).
/// </remarks>
internal readonly struct LightMark
{
    private readonly bool _cut;

    /// <summary>
    /// The position the attempt started at.
    /// </summary>
    public long Position { get; }

    private LightMark(long position, bool cut)
    {
        Position = position;
        _cut = cut;
    }

    /// <summary>
    /// Saves the cursor's position and cut flag before an attempt.
    /// </summary>
    public static LightMark Take<T>(ref Cursor<T> cursor) =>
        new(cursor.Position, cursor.Cut);

    /// <summary>
    /// Call after the attempt failed. Returns whether it failed without consuming, and if so restores the cut flag.
    /// </summary>
    public bool FailedEmpty<T>(ref Cursor<T> cursor)
    {
        if (cursor.Position != Position)
        {
            return false;
        }

        cursor.Cut = _cut;
        return true;
    }
}
