namespace Creole;

/// <summary>
/// A token with a discrete kind.
/// </summary>
/// <typeparam name="TKind">The enum of token kinds.</typeparam>
/// <remarks>
/// Kinds make the set of tokens that can start a rule precise, which sharpens analysis and lets engines dispatch on the kind with a jump table.
/// Implement it on struct tokens so tokens never box.
/// </remarks>
public interface IKinded<TKind> where TKind : struct, Enum
{
    /// <summary>
    /// The kind of this token.
    /// </summary>
    TKind Kind { get; }
}
