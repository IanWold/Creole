namespace Creole;

/// <summary>
/// One operator in an <c>Expression.Build</c> table: an operator parser, its fixity and the function that combines its operands.
/// </summary>
/// <typeparam name="TIn">The input item type of the grammar.</typeparam>
/// <typeparam name="T">The type every level of the expression produces.</typeparam>
/// <remarks>
/// Create operators with the methods of <see cref="OperatorFactory{TIn, T}"/>, inside the table lambda passed to <c>Expression.Build</c>.
/// An operator has no public members; it is only placed in the table.
/// </remarks>
public abstract class ExpressionOperator<TIn, T>
{
    internal ExpressionOperator() { }

    internal abstract Fixity Fixity { get; }

    /// <summary>
    /// The parser that matches the operator itself.
    /// </summary>
    internal abstract INode Node { get; }
}
