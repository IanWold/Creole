namespace Creole;

internal sealed partial class PlanBuilder
{
    /// <summary>
    /// Lowers <c>Context</c> to its item's plan: it does not change how the item parses, only what a failure reports, which arrives with the error model (ERR-02).
    /// </summary>
    public Plan<TIn, T> Context<TIn, T>(Parser<TIn, T> item) => Lower(item);

    /// <summary>
    /// Lowers <c>Named</c> to its item's plan: it does not change how the item parses, only what a failure reports, which arrives with the error model (ERR-02).
    /// </summary>
    public Plan<TIn, T> Named<TIn, T>(Parser<TIn, T> item) => Lower(item);
}
