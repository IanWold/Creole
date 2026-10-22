namespace Creole;

internal sealed partial class PlanBuilder
{
    public Plan<TIn, T> Atomic<TIn, T>(Parser<TIn, T> item) => new AtomicPlan<TIn, T>(Lower(item));

    /// <summary>
    /// Lowers <c>Context</c> to its item's plan: it does not change how the item parses, only what a failure reports, which arrives with the error model (ERR-02).
    /// </summary>
    public Plan<TIn, T> Context<TIn, T>(Parser<TIn, T> item) => Lower(item);

    public static Plan<TIn, Unit> Cut<TIn>() => new CutPlan<TIn>();

    public Plan<TIn, T> Lookahead<TIn, T>(Parser<TIn, T> item) => new LookaheadPlan<TIn, T>(Lower(item));

    /// <summary>
    /// Lowers <c>Named</c> to its item's plan: it does not change how the item parses, only what a failure reports, which arrives with the error model (ERR-02).
    /// </summary>
    public Plan<TIn, T> Named<TIn, T>(Parser<TIn, T> item) => Lower(item);

    public Plan<TIn, Unit> Not<TIn, T>(Parser<TIn, T> item) => new NotPlan<TIn, T>(Lower(item));
}
