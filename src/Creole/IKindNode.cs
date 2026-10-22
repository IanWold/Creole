namespace Creole;

/// <summary>
/// A <c>Kind</c> node seen without its kind type: how dispatch reads a token's kind as a key.
/// </summary>
/// <remarks>
/// An <c>Or</c> over tokens dispatches on the next token's kind (ADR-0016).
/// A token type may implement <see cref="IKinded{TKind}"/> for more than one kind type, so dispatch reads keys only when every <c>Kind</c> node it relies on has the same <see cref="KindType"/>.
/// </remarks>
internal interface IKindNode<TToken> : IPrimitiveNode
{
    /// <summary>
    /// The enum type of the kind.
    /// </summary>
    Type KindType { get; }

    /// <summary>
    /// Reads a token's kind as the integer key that <see cref="IPrimitiveNode.First"/> uses.
    /// </summary>
    Func<TToken, long> ReadKey { get; }
}
