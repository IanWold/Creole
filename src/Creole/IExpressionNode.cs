namespace Creole;

/// <summary>
/// The untyped table view of an <c>Expression.Build</c> node, for analysis and grammar export.
/// </summary>
internal interface IExpressionNode : INode
{
    INode Atom { get; }

    /// <summary>
    /// The levels of the table, tightest first, each with its operators in declaration order.
    /// </summary>
    IReadOnlyList<IReadOnlyList<OperatorInfo>> Levels { get; }
}

/// <summary>
/// One operator in the table view: its fixity and the parser that matches it.
/// </summary>
internal readonly record struct OperatorInfo(Fixity Fixity, INode Operator);
