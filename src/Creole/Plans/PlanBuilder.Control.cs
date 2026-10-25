namespace Creole;

internal sealed partial class PlanBuilder
{
    public Plan<TIn, T> Atomic<TIn, T>(Parser<TIn, T> item) => new AtomicPlan<TIn, T>(Lower(item));

    public Plan<TIn, T> Context<TIn, T>(Parser<TIn, T> item, string text) =>
        new ContextPlan<TIn, T>(Lower(item), text);

    public static Plan<TIn, Unit> Cut<TIn>() => new CutPlan<TIn>();

    public Plan<TIn, T> Lookahead<TIn, T>(Parser<TIn, T> item) => new LookaheadPlan<TIn, T>(Lower(item));

    public Plan<TIn, T> Named<TIn, T>(Parser<TIn, T> item, Expected label) =>
        new NamedPlan<TIn, T>(Lower(item), label);

    public Plan<TIn, Unit> Not<TIn, T>(Parser<TIn, T> item) => new NotPlan<TIn, T>(Lower(item));
}
