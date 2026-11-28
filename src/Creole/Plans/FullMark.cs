namespace Creole;

/// <summary>
/// A mark that saves the position, the cut flag, how many diagnostics the layer has raised, the length of its mode log, and the layer's state (§ Bookmarks and retention).
/// </summary>
/// <remarks>
/// Lowering gives it to a grammar whose parsers can raise diagnostics, change a mode or change the layer's state.
/// Going back to the mark withdraws the diagnostics raised since, and each one built from the furthest failure gives that failure back.
/// It then undoes the mode changes made since, newest first, which discards the items read in the modes it undoes and forgets the failures past each change.
/// It also puts back the state from when the mark was taken, which it saves as a reference.
/// </remarks>
internal readonly struct FullMark : IMark<FullMark>
{
    private readonly bool _cut;
    private readonly int _diagnostics;
    private readonly long _modes;
    private readonly object? _state;

    /// <summary>
    /// The position the mark was taken at.
    /// </summary>
    public long Position { get; }

    /// <summary>
    /// True: a full mark saves the diagnostic count, the mode log's length and the state, and <see cref="Rewind"/> puts them back.
    /// </summary>
    public static bool Rewinds => true;

    private FullMark(long position, bool cut, int diagnostics, long modes, object? state)
    {
        Position = position;
        _cut = cut;
        _diagnostics = diagnostics;
        _modes = modes;
        _state = state;
    }

    /// <summary>
    /// Call after the attempt failed. Returns whether it failed without consuming and without starving, and if so withdraws the diagnostics raised since the mark, undoes the mode changes made since, and restores the state and the cut flag.
    /// </summary>
    public bool FailedEmpty<T>(ref Cursor<T> cursor)
    {
        if (cursor.Position != Position || cursor.Starved)
        {
            return false;
        }

        Rewind(ref cursor);
        cursor.Cut = _cut;
        return true;
    }

    /// <summary>
    /// Withdraws the diagnostics raised since the mark, undoes the mode changes made since, and restores the state, in that order.
    /// </summary>
    /// <remarks>
    /// The cursor must be back at the mark's position, so that it stands at or before every change it undoes.
    /// </remarks>
    public void Rewind<T>(ref Cursor<T> cursor)
    {
        cursor.WithdrawDiagnostics(_diagnostics);
        cursor.Modes?.UndoTo(ref cursor, _modes);
        cursor.State = _state;
    }

    /// <summary>
    /// Saves the cursor's position, cut flag, diagnostic count, mode log length and state.
    /// </summary>
    public static FullMark Take<T>(ref Cursor<T> cursor) =>
        new(cursor.Position, cursor.Cut, cursor.DiagnosticCount, cursor.Modes?.Length ?? 0, cursor.State);
}
