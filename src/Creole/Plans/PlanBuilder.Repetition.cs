namespace Creole;

internal sealed partial class PlanBuilder
{
    public Plan<TIn, IReadOnlyList<T>> Repeat<TIn, T>(Parser<TIn, T> item, int min, int? max) =>
        new RepeatPlan<TIn, T>(Lower(item), min, max);

    public Plan<TIn, IReadOnlyList<T>> Separated<TIn, T, TSep>(Parser<TIn, T> item, Parser<TIn, TSep> separator, int min, bool allowTrailing) =>
        new SeparatedPlan<TIn, T, TSep>(Lower(item), Lower(separator), min, allowTrailing);

    public Plan<TIn, Unit> SkipRepeat<TIn, T>(Parser<TIn, T> item, int min, int? max) =>
        new SkipRepeatPlan<TIn, T>(Lower(item), min, max);

    public Plan<TIn, IReadOnlyList<T>> Until<TIn, T, TTerm>(Parser<TIn, T> item, Parser<TIn, TTerm> terminator) =>
        new UntilPlan<TIn, T, TTerm>(Lower(item), Lower(terminator));
}
