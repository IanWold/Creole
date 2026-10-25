namespace Creole;

/// <summary>
/// Settings for a parse that do not depend on its input, so one instance can serve many parses.
/// </summary>
/// <remarks>
/// Options are immutable. Make a variant with a <see langword="with"/> expression, such as <c>ParseOptions.Default with { MaxDepth = 200 }</c>.
/// </remarks>
public sealed record ParseOptions
{
    private readonly int _maxDepth = 1000;

    /// <summary>
    /// The options a parse given none uses.
    /// </summary>
    public static ParseOptions Default { get; } = new();

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
