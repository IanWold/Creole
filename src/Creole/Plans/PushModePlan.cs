namespace Creole;

/// <summary>
/// Pushes a mode onto a channel's stack of modes and succeeds without consuming.
/// </summary>
/// <param name="channel">The channel whose stack changes.</param>
/// <param name="mode">The new top.</param>
internal sealed class PushModePlan<TIn, TMode>(ModeChannel<TMode> channel, TMode mode) : Plan<TIn, Unit>
{
    private readonly ModeChannel<TMode> _channel = channel;
    private readonly TMode _mode = mode;

    public override bool TryParse(ref Cursor<TIn> cursor, ref Unit result)
    {
        var link = ModeWriter.Of(ref cursor).Link(_channel);

        link.Stack = new ModeStack<TMode>(_mode, link.Stack);

        return true;
    }
}
