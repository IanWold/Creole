namespace Creole;

/// <summary>
/// What a parse found where it failed: an item, the end of the input, or a message from the grammar.
/// </summary>
/// <remarks>
/// A switch over <see cref="Unexpected"/> that covers all three cases needs no default arm.
/// </remarks>
public union Unexpected(UnexpectedItem, EndOfInput, CustomMessage);
