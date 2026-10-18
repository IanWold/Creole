using Creole.Plans;

namespace Creole.Nodes;

/// <summary>
/// An operator-precedence expression over <see cref="Atom"/>, built by <c>Expression.Build</c> (ADR-0004).
/// </summary>
/// <param name="atom">The operand parser.</param>
/// <param name="levels">The checked table, tightest level first. The node keeps these arrays, so pass copies.</param>
internal sealed class ExpressionNode<TIn, T>(Parser<TIn, T> atom, ExpressionOperator<TIn, T>[][] levels) : Parser<TIn, T>, IExpressionNode
{
    private readonly INode[] _children = [atom, .. levels.SelectMany(level => level.Select(op => op.Node))];
    private readonly OperatorInfo[][] _view = [.. levels.Select(level => level.Select(op => new OperatorInfo(op.Fixity, op.Node)).ToArray())];

    public Parser<TIn, T> Atom { get; } = atom;

    /// <summary>
    /// The levels of the table, tightest first, each with its operators in declaration order.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<ExpressionOperator<TIn, T>>> Levels { get; } = levels;

    internal override NodeKind Kind => NodeKind.Expression;

    /// <summary>
    /// The atom, then every operator parser in table order.
    /// </summary>
    internal override IReadOnlyList<INode> Children => _children;

    INode IExpressionNode.Atom => Atom;

    IReadOnlyList<IReadOnlyList<OperatorInfo>> IExpressionNode.Levels => _view;

    internal override Plan<TIn, T> Lower(PlanBuilder builder) =>
        PlanBuilder.NotYetExecutable<TIn, T>(Kind);

    /// <summary>
    /// Copies <paramref name="table"/> and applies the construction-time checks of ADR-0004.
    /// </summary>
    /// <exception cref="GrammarException">The table, a level or an operator is null; the table or a level is empty; or a level mixes infix associativities.</exception>
    public static ExpressionOperator<TIn, T>[][] CheckTable(IReadOnlyList<IReadOnlyList<ExpressionOperator<TIn, T>>>? table)
    {
        if (table is null)
        {
            throw new GrammarException("The operator table is null.");
        }

        if (table.Count == 0)
        {
            throw new GrammarException("The operator table has no levels.");
        }

        var levels = new ExpressionOperator<TIn, T>[table.Count][];

        for (var i = 0; i < table.Count; i++)
        {
            var level = table[i] ?? throw new GrammarException($"Level {i} of the operator table is null.");

            if (level.Count == 0)
            {
                throw new GrammarException($"Level {i} of the operator table has no operators.");
            }

            levels[i] = new ExpressionOperator<TIn, T>[level.Count];

            for (var j = 0; j < level.Count; j++)
            {
                levels[i][j] = level[j] ?? throw new GrammarException($"Operator {j} in level {i} of the operator table is null.");
            }

            var infix = levels[i].Select(op => op.Fixity).Where(fixity => fixity is Fixity.InfixL or Fixity.InfixR or Fixity.InfixN).Distinct().ToArray();

            if (infix.Length > 1)
            {
                throw new GrammarException($"Level {i} of the operator table mixes {string.Join(" and ", infix)} operators. Put infix operators with different associativity on different levels.");
            }
        }

        return levels;
    }
}
