using System.Runtime.CompilerServices;

namespace Creole;

internal sealed partial class PlanBuilder
{
    public Plan<TIn, T> Longest<TIn, T>(IReadOnlyList<Parser<TIn, T>> alternatives) =>
        _options.HasFlag(LoweringOptions.Dfa) && alternatives is IReadOnlyList<Parser<char, T>> chars && DfaLongest(chars) is Plan<TIn, T> dfa
        ? dfa
        : new LongestPlan<TIn, T>([.. alternatives.Select(Lower)]);

    public Plan<TIn, TOut> Match<TIn, TUnion, TOut>(Parser<TIn, TUnion> scrutinee, IReadOnlyList<MatchCase<TIn, TOut>> cases)
        where TUnion : IUnion =>
        new MatchPlan<TIn, TUnion, TOut>(Lower(scrutinee), [.. cases.Select(c => c.Lower(this))]);

    public MatchCasePlan<TIn, TOut> MatchCase<TIn, TCase, TBranch, TOut>(Parser<TIn, TBranch> branch, Func<TCase, TBranch, TOut> combine) =>
        new TypedMatchCasePlan<TIn, TCase, TBranch, TOut>(Lower(branch), combine);

    /// <summary>
    /// Lowers an <c>Or</c>, as a <see cref="DispatchPlan{TIn, T}"/> when <see cref="LoweringOptions.Dispatch"/> is on and enough alternatives have keys (<see cref="DispatchThreshold"/>).
    /// </summary>
    public Plan<TIn, T> Or<TIn, T>(IReadOnlyList<Parser<TIn, T>> alternatives)
    {
        Plan<TIn, T>[] plans = [.. alternatives.Select(Lower)];

        return _options.HasFlag(LoweringOptions.Dispatch) && Dispatch(alternatives, plans) is { } dispatch
            ? dispatch
            : new OrPlan<TIn, T>(plans);
    }

    public Plan<TIn, TOut> Switch<TIn, TKey, TOut>(Parser<TIn, TKey> scrutinee, IReadOnlyList<TKey> keys, IReadOnlyList<Parser<TIn, TOut>> branches, Parser<TIn, TOut> @default) =>
        new SwitchPlan<TIn, TKey, TOut>(Lower(scrutinee), [.. keys], [.. branches.Select(Lower)], Lower(@default));
}
