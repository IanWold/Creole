namespace Creole;

/// <summary>
/// The base class for grammars over tokens that carry a kind.
/// </summary>
/// <typeparam name="TToken">The token type. Make it a struct so tokens never box.</typeparam>
/// <typeparam name="TKind">The enum of token kinds.</typeparam>
public abstract class TokenGrammar<TToken, TKind> : Grammar<TToken>
    where TToken : IKinded<TKind>
    where TKind : struct, Enum
{
    /// <summary>
    /// Creates a grammar. Grammar classes hold static rules and are not usually instantiated.
    /// </summary>
    protected TokenGrammar() { }
}
