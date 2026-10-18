namespace Creole.Plans;

/// <summary>
/// Lowers a grammar tree to an execution plan. One builder lowers one grammar, then is discarded.
/// </summary>
/// <remarks>
/// Each node lowers itself through <see cref="Parser{TIn, TOut}.Lower"/>,
/// which forwards the node's typed fields to a method here, so lowering logic stays in the engine (ADR-0001).
/// A node reached more than once in the same grammar is lowered once and its plan shared.
/// </remarks>
internal sealed class PlanBuilder
{
    private readonly Dictionary<INode, object> _plans = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// Returns the plan for <paramref name="parser"/>, lowering it if this builder has not seen it yet.
    /// </summary>
    public Plan<TIn, TOut> Lower<TIn, TOut>(Parser<TIn, TOut> parser)
    {
        if (_plans.TryGetValue(parser, out var existing))
        {
            return (Plan<TIn, TOut>)existing;
        }

        var plan = parser.Lower(this);
        _plans.Add(parser, plan);
        return plan;
    }

    public static Plan<TIn, TOut> Return<TIn, TOut>(TOut value) => new ReturnPlan<TIn, TOut>(value);

    public static Plan<TIn, TOut> Fail<TIn, TOut>() => new FailPlan<TIn, TOut>();
}
