namespace Creole;

/// <summary>
/// A postfix operator: runs the operator and combines it with the operand before it.
/// </summary>
internal sealed class PostfixOperatorPlan<TIn, TOp, T>(Plan<TIn, TOp> op, Func<T, TOp, T> combine, int level) : OperatorPlan<TIn, T>(Fixity.Postfix, level)
{
    private readonly Func<T, TOp, T> _combine = combine;
    private readonly Plan<TIn, TOp> _op = op;

    public override bool TryApply(ref Cursor<TIn> cursor, ExpressionPlan<TIn, T> expression, ref T value)
    {
        TOp operatorValue = default!;

        if (!_op.TryParse(ref cursor, ref operatorValue))
        {
            return false;
        }

        value = _combine(value, operatorValue);
        return true;
    }

    public override bool TryMatch(ref Cursor<TIn> cursor)
    {
        TOp operatorValue = default!;
        return _op.TryParse(ref cursor, ref operatorValue);
    }
}
