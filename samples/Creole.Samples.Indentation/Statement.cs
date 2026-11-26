namespace Creole.Samples.Indentation;

/// <summary>
/// A statement: an <see cref="IfStatement"/>, a <see cref="WhileStatement"/> or an <see cref="Assignment"/>.
/// </summary>
public readonly union Statement(IfStatement, WhileStatement, Assignment);
