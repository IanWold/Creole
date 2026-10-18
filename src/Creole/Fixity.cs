namespace Creole;

/// <summary>
/// Where an operator in an <c>Expression.Build</c> table appears relative to its operands, and how it associates.
/// </summary>
internal enum Fixity
{
    Prefix,
    Postfix,
    InfixL,
    InfixR,
    InfixN,
}
