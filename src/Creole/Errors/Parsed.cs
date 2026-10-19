namespace Creole;

/// <summary>
/// A parse that succeeded with no errors.
/// </summary>
/// <typeparam name="T">The type of the parsed value.</typeparam>
/// <param name="Value">The parsed value.</param>
public sealed record Parsed<T>(T Value);
