using System.Runtime.CompilerServices;

namespace Creole.Plans;

/// <summary>
/// Lowers each grammar on first use and keeps its plan for as long as the grammar is alive.
/// </summary>
/// <remarks>
/// The cache is keyed by the root node asked for, so a grammar is lowered once however many threads ask for it at the same time.
/// Children are not cached on their own: they are lowered as part of their root.
/// <see cref="ConditionalWeakTable{TKey, TValue}.GetValue"/> may run its callback more than once in a race,
/// but only the stored <see cref="Lazy{T}"/> is ever evaluated, so lowering itself runs once.
/// If lowering throws, every later call throws the same exception.
/// </remarks>
internal static class PlanCache
{
    private static readonly ConditionalWeakTable<INode, Lazy<object>> _plans = new();

    /// <summary>
    /// Returns the plan for <paramref name="parser"/>, lowering it if this is its first use.
    /// </summary>
    public static Plan<TIn, TOut> Get<TIn, TOut>(Parser<TIn, TOut> parser) =>
        (Plan<TIn, TOut>)_plans.GetValue(
            parser,
            static node => new Lazy<object>(
                () => new PlanBuilder().Lower((Parser<TIn, TOut>)node),
                LazyThreadSafetyMode.ExecutionAndPublication
            )
        ).Value;
}
