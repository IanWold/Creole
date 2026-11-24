namespace Creole;

/// <summary>
/// A mark that saves only the position and the cut flag (§ Bookmarks and retention).
/// </summary>
/// <remarks>
/// An attempt is each alternative of a choice, the item of <c>Optional</c>, each repetition item, separator and terminator.
/// When the attempt fails without consuming, the mark restores the cut flag, so a cut the attempt passed is dropped with it (ADR-0011).
/// </remarks>
internal readonly struct LightMark : IMark<LightMark>
{
    private readonly bool _cut;

    /// <summary>
    /// The position the attempt started at.
    /// </summary>
    public long Position { get; }

    /// <summary>
    /// False: a light mark saves only the position and the cut flag.
    /// </summary>
    public static bool Rewinds => false;

    private LightMark(long position, bool cut)
    {
        Position = position;
        _cut = cut;
    }

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

    /// <summary>
    /// Does nothing: a light mark saves no part other than the position and the cut flag.
    /// </summary>
    public void Rewind<T>(ref Cursor<T> cursor) { }

    /// <summary>
    /// Saves the cursor's position and cut flag before an attempt.
    /// </summary>
    public static LightMark Take<T>(ref Cursor<T> cursor) =>
        new(cursor.Position, cursor.Cut);
}
