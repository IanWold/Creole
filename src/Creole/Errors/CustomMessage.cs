namespace Creole;

/// <summary>
/// A message that a grammar gives in place of an unexpected item, such as one raised by a failing check.
/// </summary>
/// <param name="Text">The message to show to a user.</param>
public sealed record CustomMessage(string Text);
