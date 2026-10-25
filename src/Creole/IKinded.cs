namespace Creole;

/// <summary>
/// A token with a discrete kind.
/// </summary>
/// <typeparam name="TKind">The enum of token kinds.</typeparam>
/// <remarks>
/// <para>
/// Kinds make the set of tokens that can start a rule precise, which sharpens analysis and lets engines dispatch on the kind with a jump table.
/// Implement it on struct tokens so tokens never box.
/// </para>
/// <para>
/// A token type also says how error messages show it: <see cref="Describe"/> names each kind, and <see cref="Detail"/> adds something of the token's own when it is unexpected.
/// Both have defaults, so a token type need only implement <see cref="Kind"/>.
/// </para>
/// </remarks>
public interface IKinded<TKind> where TKind : struct, Enum
{
    /// <summary>
    /// What an error message adds after this token's kind when the token is unexpected, such as its text, or <see langword="null"/> to add nothing.
    /// </summary>
    /// <remarks>
    /// A string token that returns its text reads <c>unexpected string "host"</c>. A comma that returns <see langword="null"/> reads <c>unexpected ','</c>.
    /// Engines read it only when they build an error, never while parsing. A struct token that keeps this default is boxed for the call.
    /// </remarks>
    string? Detail => null;

    /// <summary>
    /// The kind of this token.
    /// </summary>
    TKind Kind { get; }

    /// <summary>
    /// How error messages name <paramref name="kind"/>, such as <c>','</c> for a comma or <c>string</c> for a string literal.
    /// </summary>
    /// <param name="kind">A kind of this token type.</param>
    /// <returns>The name to show. The default is the kind's enum name.</returns>
    /// <remarks>
    /// <para>
    /// Override it on the token type with an explicit implementation, such as <c>static string IKinded&lt;J&gt;.Describe(J kind) =&gt; kind switch { J.Comma =&gt; "','", … };</c>.
    /// An expected kind reads <c>expected ','</c>, and an unexpected token starts with its kind's name.
    /// </para>
    /// <para>
    /// Engines and tools call it only when they build an error or a report, never while parsing, so it may read the current UI culture.
    /// It must not throw. A null or empty result is replaced by the kind's enum name.
    /// </para>
    /// </remarks>
    static virtual string Describe(TKind kind) => kind.ToString();
}
