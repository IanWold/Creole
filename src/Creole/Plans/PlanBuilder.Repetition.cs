namespace Creole;

internal sealed partial class PlanBuilder
{
    public Plan<TIn, IReadOnlyList<T>> Repeat<TIn, T>(Parser<TIn, T> item, int min, int? max) =>
        new RepeatPlan<TIn, T>(Lower(item), min, max);

    public Plan<TIn, IReadOnlyList<T>> Separated<TIn, T, TSep>(Parser<TIn, T> item, Parser<TIn, TSep> separator, int min, bool allowTrailing) =>
        new SeparatedPlan<TIn, T, TSep>(Lower(item), Lower(separator), min, allowTrailing);

    /// <summary>
    /// Lowers a skipped repetition, as a <see cref="ScanPlan"/> when the item is a character class and <see cref="LoweringOptions.Scan"/> is on.
    /// </summary>
    public Plan<TIn, Unit> SkipRepeat<TIn, T>(Parser<TIn, T> item, int min, int? max) =>
        _options.HasFlag(LoweringOptions.Scan) && item is CharClassNode run && ScanPlan.Of(run.Set, min, max) is Plan<TIn, Unit> scan
            ? scan
            : new SkipRepeatPlan<TIn, T>(Lower(item), min, max);

    public Plan<TIn, IReadOnlyList<T>> Until<TIn, T, TTerm>(Parser<TIn, T> item, Parser<TIn, TTerm> terminator) =>
        new UntilPlan<TIn, T, TTerm>(Lower(item), Lower(terminator));
}
