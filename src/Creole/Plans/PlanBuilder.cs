namespace Creole;

/// <summary>
/// Lowers a grammar tree to an execution plan. One builder lowers one grammar, then is discarded.
/// </summary>
/// <remarks>
/// Each node lowers itself through <see cref="Parser{TIn, TOut}.Lower"/>, which forwards the node's typed fields to a method here, so lowering logic stays in the engine (ADR-0001).
/// A node reached more than once in the same grammar is lowered once and its plan shared.
/// A node with compiled code registered in <see cref="Staging"/> uses that code instead, wherever it appears.
/// </remarks>
internal sealed class PlanBuilder
{
    private readonly Dictionary<INode, object> _plans = new(ReferenceEqualityComparer.Instance);

    public static Plan<TIn, TOut> Fail<TIn, TOut>() => new FailPlan<TIn, TOut>();

    /// <summary>
    /// Returns the plan for <paramref name="parser"/>, lowering it if this builder has not seen it yet.
    /// Compiled code registered for the node takes precedence over lowering it.
    /// </summary>
    public Plan<TIn, TOut> Lower<TIn, TOut>(Parser<TIn, TOut> parser)
    {
        if (_plans.TryGetValue(parser, out var existing))
        {
            return (Plan<TIn, TOut>)existing;
        }

        var plan = Staging.TryGetPlan(parser, out var staged)
            ? staged
            : parser.Lower(this);

        _plans.Add(parser, plan);

        return plan;
    }

    /// <summary>
    /// Stands in for the lowering of a node kind the interpreter cannot run yet. INT-03 replaces every call.
    /// </summary>
    public static Plan<TIn, TOut> NotYetExecutable<TIn, TOut>(NodeKind kind) =>
        throw new NotSupportedException($"The interpreter cannot run {kind} nodes yet.");

    public static Plan<TIn, TOut> Return<TIn, TOut>(TOut value) => new ReturnPlan<TIn, TOut>(value);
}
