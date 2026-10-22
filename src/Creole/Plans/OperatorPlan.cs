namespace Creole;

/// <summary>
/// The plan for one operator of an <see cref="ExpressionPlan{TIn, T}"/>, which hides the type its parser produces.
/// </summary>
/// <remarks>
/// <see cref="TryApply"/> sets the value only on success, so after an attempt that fails without consuming, the expression still holds its left operand.
/// </remarks>
internal abstract class OperatorPlan<TIn, T>(Fixity fixity, int level)
{
    public Fixity Fixity { get; } = fixity;

    /// <summary>
    /// The operator's level in the table. 0 is the tightest.
    /// </summary>
    public int Level { get; } = level;

    /// <summary>
    /// Runs the operator, and for a prefix or infix operator the operand after it, then combines them into <paramref name="value"/>.
    /// For a postfix or infix operator, <paramref name="value"/> holds the left operand on entry.
    /// </summary>
    public abstract bool TryApply(ref Cursor<TIn> cursor, ExpressionPlan<TIn, T> expression, ref T value);

    /// <summary>
    /// Runs only the operator's parser and reports whether it matched.
    /// The expression uses it to detect a chained non-associative operator.
    /// </summary>
    public abstract bool TryMatch(ref Cursor<TIn> cursor);
}
