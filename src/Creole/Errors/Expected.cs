namespace Creole;

/// <summary>
/// What a failure expected, as a primitive or a label records it: built into an <see cref="Expectation"/> only when the error is reported.
/// </summary>
/// <remarks>
/// Each node creates its descriptors once, so a failure records a reference and allocates nothing.
/// Recording drops a descriptor already recorded at the same position; building the error drops expectations that turn out equal.
/// </remarks>
internal sealed class Expected(Func<Expectation> build)
{
    private readonly Func<Expectation> _build = build;

    /// <summary>
    /// The end of the input, which <c>End</c> expects.
    /// </summary>
    public static Expected End { get; } = new(static () => new EndOfInput());

    /// <summary>
    /// Builds the expectation, calling any user code that names it.
    /// </summary>
    public Expectation Build() => _build();

    /// <summary>
    /// A character class without a name, as one expectation such as <c>[a-z]</c>.
    /// </summary>
    public static Expected CharacterSet(CharSet set) => new(() => new ExpectedItem(Display.CharacterSet(set)));

    /// <summary>
    /// One item, such as the argument of <c>Token(x)</c>.
    /// </summary>
    public static Expected Item<T>(T value) => new(() => new ExpectedItem(Display<T>.Item(value)));

    /// <summary>
    /// A label, such as a named class's <c>digit</c>, a scanner's name or the label of <c>Named</c>.
    /// </summary>
    public static Expected Labelled(string name) => new(() => new Label(name));

    /// <summary>
    /// A string, such as <c>"let"</c>.
    /// </summary>
    public static Expected Text(string text) => new(() => new ExpectedItem(Display.Text(text)));

    /// <summary>
    /// A token kind, named as the token type describes it.
    /// </summary>
    public static Expected TokenKind<TToken, TKind>(TKind kind)
        where TToken : IKinded<TKind>
        where TKind : struct, Enum =>
        new(() => new ExpectedItem(Display.Kind<TToken, TKind>(kind)));

    /// <summary>
    /// A union token's case, named by its type.
    /// </summary>
    public static Expected UnionCase<TCase>() => new(static () => new ExpectedItem(Display.TypeName(typeof(TCase))));
}
