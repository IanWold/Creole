namespace Creole;

/// <summary>
/// Ends a parse whose layer would hold more items of the buffer it reads than its limit allows.
/// </summary>
/// <remarks>
/// No combinator catches it, so it ends the whole parse and the limit never changes what a grammar accepts.
/// </remarks>
internal class BufferLimitExceededException(long position, long horizon, int limit)
    : Exception($"The layer would hold more than {limit} items of its input from position {horizon}, reading at position {position}.")
{
    /// <summary>
    /// The oldest position the layer could still go back to, where the items it held begin.
    /// </summary>
    public long Horizon { get; } = horizon;

    /// <summary>
    /// The most items the layer may hold.
    /// </summary>
    public int Limit { get; } = limit;

    /// <summary>
    /// The position the layer was reading when it passed the limit.
    /// </summary>
    public long Position { get; } = position;
}
