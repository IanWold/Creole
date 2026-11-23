namespace Creole;

internal sealed partial class PlanBuilder
{
    public Plan<TIn, T> Expression<TIn, T>(Parser<TIn, T> atom, IReadOnlyList<IReadOnlyList<ExpressionOperator<TIn, T>>> levels)
    {
        var atomPlan = Lower(atom);
        var operators = levels.SelectMany((level, index) => level.Select(op => op.Lower<LightMark>(this, index))).ToArray();

        return new ExpressionPlan<TIn, T, LightMark>(
            atomPlan,
            [.. operators.Where(op => op.Fixity == Fixity.Prefix)],
            [.. operators.Where(op => op.Fixity != Fixity.Prefix)],
            levels.Count - 1
        );
    }

    public OperatorPlan<TIn, T, TMark> Infix<TIn, TOp, T, TMark>(Fixity fixity, Parser<TIn, TOp> op, Func<T, TOp, T, T> combine, int level)
        where TMark : struct, IMark<TMark> =>
        new InfixOperatorPlan<TIn, TOp, T, TMark>(fixity, Lower(op), combine, level)
        {
            Chain = fixity == Fixity.InfixN
                ? Chained(op)
                : null,
        };

    /// <summary>
    /// The message for <paramref name="op"/> when it follows another operator of its non-associative level, naming it as dispatch would name it if skipped.
    /// </summary>
    private static ChainMessage Chained(INode op)
    {
        var (primitive, label) = Keyed(op);

        return new ChainMessage(label ?? primitive?.Expected);
    }

    public OperatorPlan<TIn, T, TMark> Postfix<TIn, TOp, T, TMark>(Parser<TIn, TOp> op, Func<T, TOp, T> combine, int level)
        where TMark : struct, IMark<TMark> =>
        new PostfixOperatorPlan<TIn, TOp, T, TMark>(Lower(op), combine, level);

    public OperatorPlan<TIn, T, TMark> Prefix<TIn, TOp, T, TMark>(Parser<TIn, TOp> op, Func<TOp, T, T> combine, int level)
        where TMark : struct, IMark<TMark> =>
        new PrefixOperatorPlan<TIn, TOp, T, TMark>(Lower(op), combine, level);
}
