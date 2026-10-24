namespace Creole;

/// <summary>
/// An infix operator: runs the operator, then the right operand, and combines them with the left operand.
/// </summary>
/// <remarks>
/// The right operand of a left-associative or non-associative operator extends only with tighter levels, so <c>a - b - c</c> is <c>(a - b) - c</c>.
/// A right-associative operator's right operand extends with its own level too, so <c>a ^ b ^ c</c> is <c>a ^ (b ^ c)</c>.
/// That operand runs one level of recursion deeper, because the input alone bounds how long the chain is.
/// </remarks>
internal sealed class InfixOperatorPlan<TIn, TOp, T>(Fixity fixity, Plan<TIn, TOp> op, Func<T, TOp, T, T> combine, int level) : OperatorPlan<TIn, T>(fixity, level)
{
    private readonly Func<T, TOp, T, T> _combine = combine;
    private readonly Plan<TIn, TOp> _op = op;

    private bool ParseRight(ref Cursor<TIn> cursor, ExpressionPlan<TIn, T> expression, ref T right)
    {
        if (Fixity != Fixity.InfixR)
        {
            return expression.Parse(ref cursor, Level - 1, ref right);
        }

        cursor.EnterLevel();
        var ok = expression.Parse(ref cursor, Level, ref right);
        cursor.LeaveLevel();

        return ok;
    }

    /// <exception cref="DepthExceededException">The operator is right-associative and the parse is too deep to parse its right operand.</exception>
    public override bool TryApply(ref Cursor<TIn> cursor, ExpressionPlan<TIn, T> expression, ref T value)
    {
        TOp operatorValue = default!;

        if (!_op.TryParse(ref cursor, ref operatorValue))
        {
            return false;
        }

        T right = default!;

        if (!ParseRight(ref cursor, expression, ref right))
        {
            return false;
        }

        value = _combine(value, operatorValue, right);
        return true;
    }

    public override bool TryMatch(ref Cursor<TIn> cursor)
    {
        TOp operatorValue = default!;
        return _op.TryParse(ref cursor, ref operatorValue);
    }
}
