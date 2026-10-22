namespace Creole;

/// <summary>
/// An infix operator: <see cref="Op"/> between two operands, combined by <see cref="Combine"/>.
/// <see cref="Fixity"/> is <see cref="Fixity.InfixL"/>, <see cref="Fixity.InfixR"/> or <see cref="Fixity.InfixN"/>.
/// </summary>
internal sealed class InfixOperator<TIn, TOp, T>(Fixity fixity, Parser<TIn, TOp> op, Func<T, TOp, T, T> combine) : ExpressionOperator<TIn, T>
{
    internal override Fixity Fixity { get; } = fixity;

    internal override INode Node => Op;

    public Func<T, TOp, T, T> Combine { get; } = combine;

    public Parser<TIn, TOp> Op { get; } = op;

    internal override OperatorPlan<TIn, T> Lower(PlanBuilder builder, int level) =>
        builder.Infix(Fixity, Op, Combine, level);
}
