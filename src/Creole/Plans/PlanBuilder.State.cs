namespace Creole;

internal partial class PlanBuilder
{
    public static Plan<TIn, TState> GetState<TIn, TState>() => new GetStatePlan<TIn, TState>();

    public Plan<TIn, TResult> Scan<TIn, TSource, TState, TResult>(Parser<TIn, TSource> source, Func<TSource, TState, SourceSpan, (TResult Result, TState State)> function) =>
        new StateScanPlan<TIn, TSource, TState, TResult>(Lower(source), function);

    public static Plan<TIn, Unit> SetState<TIn>(object held) => new SetStatePlan<TIn>(held);
}
