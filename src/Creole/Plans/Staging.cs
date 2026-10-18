using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Creole.Plans;

/// <summary>
/// The shape of a rule compiled by the source generator:
/// the same contract as <see cref="Plan{TIn, TOut}.TryParse"/>.
/// </summary>
internal delegate bool StagedParse<TIn, TOut>(ref Cursor<TIn> cursor, out TOut result);

/// <summary>
/// Compiled code registered for grammar nodes.
/// Lowering uses a node's registered code in place of the node's own plan.
/// </summary>
/// <remarks>
/// Generated grammar classes register each rule from their static constructor, which runs before the rule
/// can be used, so registration comes before lowering. A registration made after a grammar was lowered does
/// not change that grammar's cached plan.
/// </remarks>
internal static class Staging
{
    private static readonly ConditionalWeakTable<INode, object> _plans = [];

    /// <summary>
    /// Registers <paramref name="parse"/> as the compiled code for <paramref name="rule"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The rule already has compiled code.</exception>
    public static void Register<TIn, TOut>(Parser<TIn, TOut> rule, StagedParse<TIn, TOut> parse)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(parse);

        if (!_plans.TryAdd(rule, new StagedPlan<TIn, TOut>(parse)))
        {
            throw new InvalidOperationException("This rule already has compiled code registered.");
        }
    }

    /// <summary>
    /// Finds the compiled code registered for <paramref name="rule"/>, as a plan.
    /// </summary>
    public static bool TryGetPlan<TIn, TOut>(Parser<TIn, TOut> rule, [NotNullWhen(true)] out Plan<TIn, TOut>? plan)
    {
        plan = _plans.TryGetValue(rule, out var staged)
            ? (Plan<TIn, TOut>)staged
            : null;

        return plan is not null;
    }
}
