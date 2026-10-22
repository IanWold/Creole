namespace Creole;

/// <summary>
/// A prefix operator: <see cref="Op"/> before an operand, combined by <see cref="Combine"/>.
/// </summary>
internal sealed class PrefixOperator<TIn, TOp, T>(Parser<TIn, TOp> op, Func<TOp, T, T> combine) : ExpressionOperator<TIn, T>
{
    internal override Fixity Fixity => Fixity.Prefix;

    internal override INode Node => Op;

    public Func<TOp, T, T> Combine { get; } = combine;

    public Parser<TIn, TOp> Op { get; } = op;

    internal override OperatorPlan<TIn, T> Lower(PlanBuilder builder, int level) =>
        builder.Prefix(Op, Combine, level);
}
