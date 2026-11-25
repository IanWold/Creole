using System.Runtime.CompilerServices;

namespace Creole;

public abstract partial class Grammar<TIn>
{
    /// <summary>
    /// A parser that succeeds without consuming input and pops the top mode of <paramref name="channel"/>'s stack, returning to the mode below it.
    /// </summary>
    /// <typeparam name="TMode">The type of the channel's modes.</typeparam>
    /// <param name="channel">The channel whose stack to pop.</param>
    /// <param name="channelExpression">Filled in by the compiler with the expression passed as <paramref name="channel"/>.</param>
    /// <returns>A parser that pops the stack and produces <see cref="Unit"/>.</returns>
    /// <exception cref="GrammarException"><paramref name="channel"/> is null. When it runs: the stack holds only one mode.</exception>
    /// <remarks>
    /// Pair it with a <see cref="PushMode{TMode}"/> through the grammar's structure, as in <c>Seq(Kind(LBrace), PushMode(Modes, Mode.Block), Body, Kind(RBrace), PopMode(Modes))</c>.
    /// A path that pops more than it pushed is a mistake in the grammar, so popping the last mode throws rather than failing the parse.
    /// </remarks>
    public static Parser<TIn, Unit> PopMode<TMode>(ModeChannel<TMode> channel, [CallerArgumentExpression(nameof(channel))] string channelExpression = "") =>
        new PopModeNode<TIn, TMode>(Guard.NotNull(channel, channelExpression));

    /// <summary>
    /// A parser that succeeds without consuming input and pushes <paramref name="mode"/> onto <paramref name="channel"/>'s stack, so it is the current mode until a <see cref="PopMode{TMode}"/> returns to the one before.
    /// </summary>
    /// <typeparam name="TMode">The type of the channel's modes.</typeparam>
    /// <param name="channel">The channel whose stack to push onto.</param>
    /// <param name="mode">The new current mode.</param>
    /// <param name="channelExpression">Filled in by the compiler with the expression passed as <paramref name="channel"/>.</param>
    /// <returns>A parser that pushes the mode and produces <see cref="Unit"/>.</returns>
    /// <exception cref="GrammarException"><paramref name="channel"/> is null.</exception>
    public static Parser<TIn, Unit> PushMode<TMode>(ModeChannel<TMode> channel, TMode mode, [CallerArgumentExpression(nameof(channel))] string channelExpression = "") =>
        new PushModeNode<TIn, TMode>(Guard.NotNull(channel, channelExpression), mode);

    /// <summary>
    /// A parser that succeeds without consuming input and replaces the top mode of <paramref name="channel"/>'s stack with <paramref name="mode"/>, so it is the current mode.
    /// </summary>
    /// <typeparam name="TMode">The type of the channel's modes.</typeparam>
    /// <param name="channel">The channel whose current mode to replace.</param>
    /// <param name="mode">The new current mode.</param>
    /// <param name="channelExpression">Filled in by the compiler with the expression passed as <paramref name="channel"/>.</param>
    /// <returns>A parser that sets the mode and produces <see cref="Unit"/>.</returns>
    /// <exception cref="GrammarException"><paramref name="channel"/> is null.</exception>
    /// <remarks>
    /// The stack keeps its depth: a mode pushed before stays pushed, with <paramref name="mode"/> in its place.
    /// </remarks>
    public static Parser<TIn, Unit> SetMode<TMode>(ModeChannel<TMode> channel, TMode mode, [CallerArgumentExpression(nameof(channel))] string channelExpression = "") =>
        new SetModeNode<TIn, TMode>(Guard.NotNull(channel, channelExpression), mode);
}
