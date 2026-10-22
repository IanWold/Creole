namespace Creole;

/// <summary>
/// A prefix operator: runs the operator, then an operand that only tighter levels may extend, and combines them.
/// </summary>
/// <remarks>
/// The operand excludes the operator's own level, so a postfix operator at that level applies after the prefix: <c>-x!</c> is <c>(-x)!</c>.
/// The operand position tries every prefix operator again, so prefix operators repeat: <c>- -x</c>.
/// </remarks>
internal sealed class PrefixOperatorPlan<TIn, TOp, T>(Plan<TIn, TOp> op, Func<TOp, T, T> combine, int level) : OperatorPlan<TIn, T>(Fixity.Prefix, level)
{
    private readonly Func<TOp, T, T> _combine = combine;
    private readonly Plan<TIn, TOp> _op = op;

    /// <exception cref="GrammarException">The operator succeeded without consuming input, so parsing its operand would try it again forever.</exception>
    public override bool TryApply(ref Cursor<TIn> cursor, ExpressionPlan<TIn, T> expression, ref T value)
    {
        var start = cursor.Position;
        TOp operatorValue = default!;

        if (!_op.TryParse(ref cursor, ref operatorValue))
        {
            return false;
        }

        if (cursor.Position == start)
        {
            throw NullableRepetition.Operator();
        }

        T operand = default!;

        if (!expression.Parse(ref cursor, Level - 1, ref operand))
        {
            return false;
        }

        value = _combine(operatorValue, operand);
        return true;
    }

    public override bool TryMatch(ref Cursor<TIn> cursor)
    {
        TOp operatorValue = default!;
        return _op.TryParse(ref cursor, ref operatorValue);
    }
}
