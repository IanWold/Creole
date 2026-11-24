namespace Creole;

/// <summary>
/// A mark that saves the position, the cut flag and how many diagnostics the layer has raised (§ Bookmarks and retention).
/// </summary>
/// <remarks>
/// Lowering gives it to a grammar whose parsers can raise diagnostics.
/// Going back to the mark withdraws the diagnostics raised since, and each one built from the furthest failure gives that failure back.
/// </remarks>
internal readonly struct FullMark : IMark<FullMark>
{
    private readonly bool _cut;
    private readonly int _diagnostics;

    /// <summary>
    /// The position the mark was taken at.
    /// </summary>
    public long Position { get; }

    /// <summary>
    /// True: a full mark saves the diagnostic count, and <see cref="Rewind"/> withdraws the diagnostics raised since.
    /// </summary>
    public static bool Rewinds => true;

    private FullMark(long position, bool cut, int diagnostics)
    {
        Position = position;
        _cut = cut;
        _diagnostics = diagnostics;
    }

    /// <summary>
    /// Call after the attempt failed. Returns whether it failed without consuming, and if so withdraws the diagnostics raised since the mark and restores the cut flag.
    /// </summary>
    public bool FailedEmpty<T>(ref Cursor<T> cursor)
    {
        if (cursor.Position != Position)
        {
            return false;
        }

        cursor.WithdrawDiagnostics(_diagnostics);
        cursor.Cut = _cut;
        return true;
    }

    /// <summary>
    /// Withdraws the diagnostics raised since the mark.
    /// </summary>
    public void Rewind<T>(ref Cursor<T> cursor) =>
        cursor.WithdrawDiagnostics(_diagnostics);

    /// <summary>
    /// Saves the cursor's position, cut flag and diagnostic count.
    /// </summary>
    public static FullMark Take<T>(ref Cursor<T> cursor) =>
        new(cursor.Position, cursor.Cut, cursor.DiagnosticCount);
}
