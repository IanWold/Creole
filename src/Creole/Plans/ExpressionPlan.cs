namespace Creole;

/// <summary>
/// Parses an <c>Expression.Build</c> expression by precedence climbing over its table (ADR-0004).
/// </summary>
/// <remarks>
/// <para>
/// Levels are numbered from 0, the tightest. <see cref="Parse"/> takes the loosest level whose postfix and infix operators may extend the expression.
/// Where an operand is expected, every prefix operator is tried in table order, whatever the limit, then the atom.
/// After an operand, the postfix and infix operators within the limit are tried, tighter levels first and in declaration order within a level, until none matches.
/// </para>
/// <para>
/// Each prefix attempt, each postfix or infix attempt, and the atom take a light mark, so a cut passed in one that fails without consuming is dropped (ADR-0011).
/// An infix attempt is the operator and its right operand together.
/// An attempt that fails after consuming fails the expression.
/// </para>
/// <para>
/// A second operator from a non-associative level fails the expression after consuming, reported where that operator starts with its chain message.
/// Its start is held in the failure log while it is matched, so a stream buffer that releases it first copies the item there.
/// </para>
/// </remarks>
internal sealed class ExpressionPlan<TIn, T>(Plan<TIn, T> atom, OperatorPlan<TIn, T>[] prefixes, OperatorPlan<TIn, T>[] operators, int loosest) : Plan<TIn, T>
{
    private readonly Plan<TIn, T> _atom = atom;
    private readonly int _loosest = loosest;

    /// <summary>
    /// The postfix and infix operators, tightest level first, in declaration order within a level.
    /// </summary>
    private readonly OperatorPlan<TIn, T>[] _operators = operators;

    /// <summary>
    /// The prefix operators, in table order.
    /// </summary>
    private readonly OperatorPlan<TIn, T>[] _prefixes = prefixes;

    private bool ParseOperand(ref Cursor<TIn> cursor, ref T result)
    {
        foreach (var prefix in _prefixes)
        {
            var mark = LightMark.Take(ref cursor);

            if (prefix.TryApply(ref cursor, this, ref result))
            {
                return true;
            }

            if (!mark.FailedEmpty(ref cursor))
            {
                return false;
            }
        }

        var atomMark = LightMark.Take(ref cursor);

        if (_atom.TryParse(ref cursor, ref result))
        {
            return true;
        }

        atomMark.FailedEmpty(ref cursor);
        return false;
    }

    /// <summary>
    /// Parses an operand, then extends it with the postfix and infix operators from levels 0 to <paramref name="limit"/>.
    /// </summary>
    /// <exception cref="GrammarException">A postfix operator, or an infix operator and its right operand, succeeded without consuming input.</exception>
    public bool Parse(ref Cursor<TIn> cursor, int limit, ref T result)
    {
        if (!ParseOperand(ref cursor, ref result))
        {
            return false;
        }

        var chained = -1;

        while (true)
        {
            var applied = false;

            foreach (var op in _operators)
            {
                if (op.Level > limit)
                {
                    break;
                }

                var mark = LightMark.Take(ref cursor);

                if (op.Level == chained)
                {
                    cursor.HoldStart();

                    if (op.TryMatch(ref cursor))
                    {
                        cursor.Reject(mark.Position, null, op.Chain ?? ChainMessage.Unnamed);
                        cursor.DropStart();
                        return false;
                    }

                    cursor.DropStart();

                    if (!mark.FailedEmpty(ref cursor))
                    {
                        return false;
                    }

                    continue;
                }

                if (op.TryApply(ref cursor, this, ref result))
                {
                    if (cursor.Position == mark.Position)
                    {
                        throw op.Fixity == Fixity.Postfix ? NullableRepetition.Operator() : NullableRepetition.InfixOperator();
                    }

                    chained = op.Fixity == Fixity.InfixN ? op.Level : -1;
                    applied = true;
                    break;
                }

                if (!mark.FailedEmpty(ref cursor))
                {
                    return false;
                }
            }

            if (!applied)
            {
                return true;
            }
        }
    }

    public override bool TryParse(ref Cursor<TIn> cursor, ref T result) =>
        Parse(ref cursor, _loosest, ref result);
}
