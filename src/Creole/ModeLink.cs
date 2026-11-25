namespace Creole;

/// <summary>
/// One channel's stack of modes in one run, whatever the type of its modes.
/// </summary>
/// <remarks>
/// <see cref="ModeWriter"/> finds a channel's link by <see cref="Channel"/>.
/// </remarks>
/// <param name="channel">The channel whose stack this is.</param>
internal abstract class ModeLink(object channel)
{
    /// <summary>
    /// The channel whose stack this is.
    /// </summary>
    public object Channel { get; } = channel;
}
