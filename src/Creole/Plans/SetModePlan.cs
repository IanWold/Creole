namespace Creole;

/// <summary>
/// Replaces the top of a channel's stack of modes and succeeds without consuming.
/// </summary>
/// <remarks>
/// A stack of one mode becomes one built with the plan, so changing between modes with nothing pushed allocates nothing.
/// </remarks>
/// <param name="channel">The channel whose stack changes.</param>
/// <param name="mode">The new top.</param>
internal sealed class SetModePlan<TIn, TMode>(ModeChannel<TMode> channel, TMode mode) : Plan<TIn, Unit>
{
    private readonly ModeChannel<TMode> _channel = channel;
    private readonly TMode _mode = mode;
    private readonly ModeStack<TMode> _single = new(mode, null);

    public override bool TryParse(ref Cursor<TIn> cursor, ref Unit result)
    {
        var link = ModeWriter.Of(ref cursor).Link(_channel);

        link.Stack = link.Stack.Rest is null
            ? _single
            : new ModeStack<TMode>(_mode, link.Stack.Rest);

        return true;
    }
}
