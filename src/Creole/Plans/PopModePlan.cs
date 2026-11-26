namespace Creole;

/// <summary>
/// Pops the top of a channel's stack of modes and succeeds without consuming.
/// </summary>
/// <remarks>
/// Popping the only mode throws <see cref="GrammarException"/>: pushes and pops pair up through the grammar's structure, so a path that pops more than it pushed is a mistake in the grammar.
/// </remarks>
/// <param name="channel">The channel whose stack changes.</param>
internal sealed class PopModePlan<TIn, TMode>(ModeChannel<TMode> channel) : Plan<TIn, Unit>
{
    private readonly ModeChannel<TMode> _channel = channel;

    public override bool TryParse(ref Cursor<TIn> cursor, ref Unit result)
    {
        var writer = ModeWriter.Of(ref cursor);
        var link = writer.Link(_channel);

        link.Stack = link.Stack.Rest ?? throw writer.PoppedLast(_channel);

        return true;
    }
}
