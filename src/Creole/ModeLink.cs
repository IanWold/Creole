namespace Creole;

/// <summary>
/// One channel's stack of modes in one run, whatever the type of its modes.
/// </summary>
/// <remarks>
/// In a pipeline, the <c>Layer.Modal</c> that reads the channel makes its link, and the layer that writes the channel changes the link's stack.
/// <see cref="ModeWriter"/> finds a channel's link by <see cref="Channel"/>.
/// </remarks>
/// <param name="channel">The channel whose stack this is.</param>
internal abstract class ModeLink(object channel)
{
    /// <summary>
    /// The channel whose stack this is.
    /// </summary>
    public object Channel { get; } = channel;

    /// <summary>
    /// The writer of the layer that writes the channel in this run: the first to change its mode, or null until one does.
    /// </summary>
    public ModeWriter? Writer { get; set; }

    /// <summary>
    /// Returns the stack to the channel's initial mode alone, as a <c>ParseMany</c> item starts.
    /// </summary>
    public abstract void Reset();
}
