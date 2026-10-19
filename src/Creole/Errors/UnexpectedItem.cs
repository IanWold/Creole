namespace Creole;

/// <summary>
/// An input item that the parse did not expect.
/// </summary>
/// <param name="Display">How to show the item to a user, such as <c>string "host"</c>.</param>
public sealed record UnexpectedItem(string Display);
