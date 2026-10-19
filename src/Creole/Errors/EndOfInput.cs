namespace Creole;

/// <summary>
/// The end of the input, as something found (<see cref="Unexpected"/>) or something expected (<see cref="Expectation"/>).
/// </summary>
/// <remarks>
/// It carries no data, so every instance is equal to every other.
/// </remarks>
public sealed record EndOfInput;
