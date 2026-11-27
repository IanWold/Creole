namespace Creole;

/// <summary>
/// One channel's stack of modes in one run.
/// </summary>
/// <remarks>
/// It starts as the channel's initial mode alone. The mode nodes replace <see cref="Stack"/> as they run, and the <c>Layer.Modal</c> that reads the channel reads its top at each step.
/// </remarks>
internal sealed class ModeLink<TMode>(ModeChannel<TMode> channel) : ModeLink(channel)
{
    private readonly ModeStack<TMode> _initial = channel.InitialStack;

    /// <summary>
    /// The stack as it stands, whose top is the current mode.
    /// </summary>
    public ModeStack<TMode> Stack { get; set; } = channel.InitialStack;

    public override bool Reset()
    {
        var changed = !EqualityComparer<TMode>.Default.Equals(Stack.Top, _initial.Top);

        Stack = _initial;

        return changed;
    }

    public override void Restore(object stack) => Stack = (ModeStack<TMode>)stack;
}
