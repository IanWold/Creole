namespace Creole;

/// <summary>
/// A channel's stack of modes in one run: <see cref="Top"/> is the current mode.
/// </summary>
/// <remarks>
/// It is immutable, so a change makes a new stack and leaves the one before it whole.
/// </remarks>
internal class ModeStack<TMode>(TMode top, ModeStack<TMode>? rest)
{
    /// <summary>
    /// The modes below the top, or null when the top is the only mode.
    /// </summary>
    public ModeStack<TMode>? Rest { get; } = rest;

    /// <summary>
    /// The current mode.
    /// </summary>
    public TMode Top { get; } = top;
}
