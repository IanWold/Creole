namespace Creole;

/// <summary>
/// Settings for a parse that do not depend on its input, so one instance can serve many parses.
/// </summary>
/// <remarks>
/// Options are immutable. Make a variant with a <see langword="with"/> expression, such as <c>ParseOptions.Default with { MaxDepth = 200 }</c>.
/// </remarks>
public sealed record ParseOptions
{
    private readonly int _maxBufferedItems = 16_777_216;
    private readonly int _maxDepth = 1000;

    /// <summary>
    /// The options a parse given none uses.
    /// </summary>
    public static ParseOptions Default { get; } = new();

    /// <summary>
    /// The most items a layer may hold of the buffer it reads, from the oldest item it may still go back to. The default is 16,777,216.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A buffer lies between two layers, or under the first layer over a streamed source, and each is bounded on its own.
    /// The layer reading it counts from the oldest item it may still read again, or still have to produce again for the layer above, through the last item produced.
    /// It counts each time it pulls an item it does not have, and a parse that would hold more ends with one error at that oldest item.
    /// No combinator catches it, so the limit never changes what a grammar accepts.
    /// </para>
    /// <para>
    /// Input held whole in memory, such as the text a parse is given, has no buffer and is not counted.
    /// <see cref="int.MaxValue"/> lifts the limit in practice.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public int MaxBufferedItems
    {
        get => _maxBufferedItems;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, nameof(MaxBufferedItems));
            _maxBufferedItems = value;
        }
    }

    /// <summary>
    /// The deepest a parse may recurse, in levels. The default is 1,000.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One level is counted while each of these runs: a <c>Rec</c>, the parser a <c>Bind</c> continuation returned, a prefix operator's operand, and an <c>InfixR</c> right operand.
    /// A parse that would go deeper ends with one error where the level would have started, and so does a parse that runs short of the thread's stack first.
    /// No combinator catches either, so the limit never changes what a grammar accepts.
    /// </para>
    /// <para>
    /// <see cref="int.MaxValue"/> leaves only the stack check.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public int MaxDepth
    {
        get => _maxDepth;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value, nameof(MaxDepth));
            _maxDepth = value;
        }
    }
}
