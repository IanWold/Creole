namespace Creole.Samples.Indentation;

/// <summary>
/// <c>if condition:</c> and a block.
/// </summary>
/// <param name="Condition">The condition, with every operation in parentheses.</param>
/// <param name="Body">The block to run if the condition holds.</param>
public sealed record IfStatement(string Condition, Block Body);
