namespace Creole;

internal sealed partial class PlanBuilder
{
    public Plan<TIn, T> Atomic<TIn, T>(Parser<TIn, T> item) =>
        FullMarks
            ? new AtomicPlan<TIn, T, FullMark>(Lower(item))
            : new AtomicPlan<TIn, T, LightMark>(Lower(item));

    public Plan<TIn, T> Context<TIn, T>(Parser<TIn, T> item, string text) =>
        new ContextPlan<TIn, T>(Lower(item), text);

    public static Plan<TIn, Unit> Cut<TIn>() => new CutPlan<TIn>();

    public Plan<TIn, T> Lookahead<TIn, T>(Parser<TIn, T> item) =>
        FullMarks
            ? new LookaheadPlan<TIn, T, FullMark>(Lower(item))
            : new LookaheadPlan<TIn, T, LightMark>(Lower(item));

    public Plan<TIn, T> Named<TIn, T>(Parser<TIn, T> item, Expected label) =>
        new NamedPlan<TIn, T>(Lower(item), label);

    public Plan<TIn, Unit> Not<TIn, T>(Parser<TIn, T> item) =>
        FullMarks
            ? new NotPlan<TIn, T, FullMark>(Lower(item))
            : new NotPlan<TIn, T, LightMark>(Lower(item));
}
