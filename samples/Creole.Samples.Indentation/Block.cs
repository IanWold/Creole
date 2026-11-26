namespace Creole.Samples.Indentation;

/// <summary>
/// The indented statements that follow the header of an <c>if</c> or a <c>while</c>.
/// </summary>
/// <param name="Body">The statements, at least one.</param>
public sealed record Block(IReadOnlyList<Statement> Body);
