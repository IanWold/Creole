namespace Creole;

/// <summary>
/// A typed key for a stack of modes that a grammar changes as it parses, such as whether <c>/</c> starts a regex or divides.
/// </summary>
/// <typeparam name="TMode">The type of the modes, usually an enum, or a union whose cases are records.</typeparam>
/// <param name="initial">The mode each parse starts in.</param>
/// <remarks>
/// <para>
/// A channel is data, as a parser is: it holds its initial mode and nothing else.
/// Each parse keeps its own stack of modes for each channel, which starts as <paramref name="initial"/> alone, so one channel serves any number of parses at once.
/// <see cref="Grammar{TIn}.SetMode{TMode}"/>, <see cref="Grammar{TIn}.PushMode{TMode}"/> and <see cref="Grammar{TIn}.PopMode{TMode}"/> change the stack.
/// </para>
/// <para>
/// In a pipeline, a <c>Layer.Modal</c> reads the channel: each of its steps runs the item parser for the mode on top of the stack.
/// The grammar of one layer above it, or the parser that ends the pipeline, changes the mode.
/// </para>
/// <para>
/// A channel is known by its instance, not by its type or initial mode, so keep it in a <see langword="static"/> <see langword="readonly"/> field and use that field everywhere.
/// Modes are compared with <see cref="EqualityComparer{T}.Default"/>, so a mode that carries data should be immutable and compare by value, as a record does.
/// </para>
/// </remarks>
public sealed class ModeChannel<TMode>(TMode initial)
{
    /// <summary>
    /// The stack a parse starts with: <see cref="Initial"/> alone.
    /// </summary>
    internal ModeStack<TMode> InitialStack { get; } = new(initial, null);

    /// <summary>
    /// The mode each parse starts in.
    /// </summary>
    public TMode Initial { get; } = initial;

    /// <summary>
    /// The channel as errors name it, such as <c>ModeChannel&lt;LexMode&gt;</c>.
    /// </summary>
    public override string ToString() => $"ModeChannel<{Display.TypeName(typeof(TMode))}>";
}
