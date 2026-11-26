namespace Creole.Samples.Indentation;

/// <summary>
/// <c>while condition:</c> and a block.
/// </summary>
/// <param name="Condition">The condition, with every operation in parentheses.</param>
/// <param name="Body">The block to run while the condition holds.</param>
public sealed record WhileStatement(string Condition, Block Body);
