using System.Diagnostics.CodeAnalysis;

namespace Creole;

/// <summary>
/// The base class for grammars over tokens that carry a kind.
/// </summary>
/// <typeparam name="TToken">The token type. Make it a struct so tokens never box.</typeparam>
/// <typeparam name="TKind">The enum of token kinds.</typeparam>
/// <remarks>
/// It adds <see cref="Kind"/> and <see cref="Case{TCase}"/> to the combinators of <see cref="Grammar{TIn}"/>.
/// </remarks>
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Static members on the generic base are the point: a derived grammar class calls them unqualified, with TToken and TKind fixed by the base.")]
public abstract class TokenGrammar<TToken, TKind> : Grammar<TToken>
    where TToken : IKinded<TKind>
    where TKind : struct, Enum
{
    /// <summary>
    /// Creates a grammar. Grammar classes hold static rules and are not usually instantiated.
    /// </summary>
    protected TokenGrammar() { }

    /// <summary>
    /// A parser that consumes one union token whose case is <typeparamref name="TCase"/>, and produces that case's value.
    /// </summary>
    /// <typeparam name="TCase">One of the case types of the union <typeparamref name="TToken"/>.</typeparam>
    /// <returns>A parser that fails without consuming input on a token of any other case, or at the end of input.</returns>
    /// <exception cref="GrammarException"><typeparamref name="TToken"/> is not a union.</exception>
    /// <remarks>
    /// <para>
    /// Use it when <typeparamref name="TToken"/> is a C# union, such as <c>union Tok(Ident, Number, Punct)</c>: <c>Case&lt;Ident&gt;()</c> produces an <c>Ident</c>, with no cast.
    /// Analysis treats each case type as a kind.
    /// </para>
    /// <para>
    /// The union must still implement <see cref="IKinded{TKind}"/> to be used with this class.
    /// Analysis cannot tell which kind a case has, so it assumes <c>Kind(k)</c> and <c>Case&lt;T&gt;()</c> may match the same token.
    /// A union boxes struct cases, so for the fastest lexers prefer struct tokens matched with <see cref="Kind"/>.
    /// </para>
    /// <para>
    /// Creole checks that <typeparamref name="TToken"/> is a union, but not that <typeparamref name="TCase"/> is one of its cases.
    /// A parser for a type that is not a case never matches.
    /// </para>
    /// </remarks>
    public static Parser<TToken, TCase> Case<TCase>() => new CaseNode<TToken, TCase>();

    /// <summary>
    /// A parser that consumes one token whose kind is <paramref name="kind"/>, and produces the token.
    /// </summary>
    /// <param name="kind">The kind to match.</param>
    /// <returns>A parser that fails without consuming input on a token of any other kind, or at the end of input.</returns>
    public static Parser<TToken, TToken> Kind(TKind kind) => new KindNode<TToken, TKind>(kind);
}
