namespace Creole;

/// <summary>
/// One thing a parse would have accepted where it failed: a named rule, an item, or the end of the input.
/// </summary>
/// <remarks>
/// A switch over <see cref="Expectation"/> that covers all three cases needs no default arm.
/// </remarks>
public union Expectation(Label, ExpectedItem, EndOfInput);
