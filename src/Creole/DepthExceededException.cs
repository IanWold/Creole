namespace Creole;

/// <summary>
/// Ends a parse that tried to enter a level of recursion deeper than it may go.
/// </summary>
/// <remarks>
/// No combinator catches it, so it ends the whole parse and the limit never changes what a grammar accepts.
/// </remarks>
internal sealed class DepthExceededException(long position, int depth, DepthCause cause)
    : Exception(
        cause == DepthCause.MaxDepth
        ? $"The parse reached its maximum depth of {depth} levels at position {position}."
        : $"The parse ran short of stack {depth} levels deep, at position {position}."
    )
{
    /// <summary>
    /// Whether the parse reached its maximum depth or ran short of stack.
    /// </summary>
    public DepthCause Cause { get; } = cause;

    /// <summary>
    /// The levels the parse was already inside.
    /// </summary>
    public int Depth { get; } = depth;

    /// <summary>
    /// Where the level would have started.
    /// </summary>
    public long Position { get; } = position;
}
