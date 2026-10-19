namespace Creole;

/// <summary>
/// One input item that the parse would have accepted.
/// </summary>
/// <param name="Display">How to show the item to a user, such as <c>','</c>.</param>
public sealed record ExpectedItem(string Display);
