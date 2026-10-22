namespace Creole;

/// <summary>
/// A postfix operator: <see cref="Op"/> after an operand, combined by <see cref="Combine"/>.
/// </summary>
internal sealed class PostfixOperator<TIn, TOp, T>(Parser<TIn, TOp> op, Func<T, TOp, T> combine) : ExpressionOperator<TIn, T>
{
    internal override Fixity Fixity => Fixity.Postfix;

    internal override INode Node => Op;

    public Func<T, TOp, T> Combine { get; } = combine;

    public Parser<TIn, TOp> Op { get; } = op;

    internal override OperatorPlan<TIn, T> Lower(PlanBuilder builder, int level) =>
        builder.Postfix(Op, Combine, level);
}
