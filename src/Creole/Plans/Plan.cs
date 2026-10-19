namespace Creole;

/// <summary>
/// A node of the interpreter's execution plan: the lowered, executable form of a grammar node.
/// </summary>
/// <remarks>
/// Plans are immutable and shared between threads; all parse state lives in the call.
/// Plan nodes are sealed classes, so <see cref="TryParse"/> is an ordinary virtual call.
/// </remarks>
internal abstract class Plan<TIn, TOut>
{
    /// <summary>
    /// Runs this plan at the cursor. On success, sets <paramref name="result"/> and returns <see langword="true"/>.
    /// Whether input was consumed is read from the cursor.
    /// </summary>
    public abstract bool TryParse(ref Cursor<TIn> cursor, ref TOut result);
}
