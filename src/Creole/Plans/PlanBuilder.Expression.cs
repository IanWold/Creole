namespace Creole;

internal sealed partial class PlanBuilder
{
    public Plan<TIn, T> Expression<TIn, T>(Parser<TIn, T> atom, IReadOnlyList<IReadOnlyList<ExpressionOperator<TIn, T>>> levels)
    {
        var atomPlan = Lower(atom);
        var operators = levels.SelectMany((level, index) => level.Select(op => op.Lower(this, index))).ToArray();

        return new ExpressionPlan<TIn, T>(
            atomPlan,
            [.. operators.Where(op => op.Fixity == Fixity.Prefix)],
            [.. operators.Where(op => op.Fixity != Fixity.Prefix)],
            levels.Count - 1
        );
    }

    public OperatorPlan<TIn, T> Infix<TIn, TOp, T>(Fixity fixity, Parser<TIn, TOp> op, Func<T, TOp, T, T> combine, int level) =>
        new InfixOperatorPlan<TIn, TOp, T>(fixity, Lower(op), combine, level);

    public OperatorPlan<TIn, T> Postfix<TIn, TOp, T>(Parser<TIn, TOp> op, Func<T, TOp, T> combine, int level) =>
        new PostfixOperatorPlan<TIn, TOp, T>(Lower(op), combine, level);

    public OperatorPlan<TIn, T> Prefix<TIn, TOp, T>(Parser<TIn, TOp> op, Func<TOp, T, T> combine, int level) =>
        new PrefixOperatorPlan<TIn, TOp, T>(Lower(op), combine, level);
}
