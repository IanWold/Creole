namespace Creole.Nodes;

/// <summary>
/// A postfix operator: <see cref="Op"/> after an operand, combined by <see cref="Combine"/>.
/// </summary>
internal sealed class PostfixOperator<TIn, TOp, T>(Parser<TIn, TOp> op, Func<T, TOp, T> combine) : ExpressionOperator<TIn, T>
{
    public Parser<TIn, TOp> Op { get; } = op;

    public Func<T, TOp, T> Combine { get; } = combine;

    internal override Fixity Fixity => Fixity.Postfix;

    internal override INode Node => Op;
}
