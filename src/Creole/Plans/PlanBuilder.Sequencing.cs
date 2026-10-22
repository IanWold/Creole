namespace Creole;

internal sealed partial class PlanBuilder
{
    public Plan<TIn, TResult> Map<TIn, TSource, TResult>(Parser<TIn, TSource> source, Func<TSource, TResult> selector) =>
        new MapPlan<TIn, TSource, TResult>(Lower(source), selector);

    public Plan<TIn, Maybe<T>> Optional<TIn, T>(Parser<TIn, T> item) => new OptionalPlan<TIn, T>(Lower(item));

    public Plan<TIn, TOut> Sequence<TIn, T1, T2, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Func<T1, T2, TOut> selector) =>
        new SequencePlan<TIn, T1, T2, TOut>(Lower(first), Lower(second), selector);

    public Plan<TIn, TOut> Sequence<TIn, T1, T2, T3, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Func<T1, T2, T3, TOut> selector) =>
        new SequencePlan<TIn, T1, T2, T3, TOut>(Lower(first), Lower(second), Lower(third), selector);

    public Plan<TIn, TOut> Sequence<TIn, T1, T2, T3, T4, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Parser<TIn, T4> fourth, Func<T1, T2, T3, T4, TOut> selector) =>
        new SequencePlan<TIn, T1, T2, T3, T4, TOut>(Lower(first), Lower(second), Lower(third), Lower(fourth), selector);

    public Plan<TIn, TOut> Sequence<TIn, T1, T2, T3, T4, T5, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Parser<TIn, T4> fourth, Parser<TIn, T5> fifth, Func<T1, T2, T3, T4, T5, TOut> selector) =>
        new SequencePlan<TIn, T1, T2, T3, T4, T5, TOut>(Lower(first), Lower(second), Lower(third), Lower(fourth), Lower(fifth), selector);

    public Plan<TIn, TOut> Sequence<TIn, T1, T2, T3, T4, T5, T6, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Parser<TIn, T4> fourth, Parser<TIn, T5> fifth, Parser<TIn, T6> sixth, Func<T1, T2, T3, T4, T5, T6, TOut> selector) =>
        new SequencePlan<TIn, T1, T2, T3, T4, T5, T6, TOut>(Lower(first), Lower(second), Lower(third), Lower(fourth), Lower(fifth), Lower(sixth), selector);

    public Plan<TIn, TOut> Sequence<TIn, T1, T2, T3, T4, T5, T6, T7, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Parser<TIn, T4> fourth, Parser<TIn, T5> fifth, Parser<TIn, T6> sixth, Parser<TIn, T7> seventh, Func<T1, T2, T3, T4, T5, T6, T7, TOut> selector) =>
        new SequencePlan<TIn, T1, T2, T3, T4, T5, T6, T7, TOut>(Lower(first), Lower(second), Lower(third), Lower(fourth), Lower(fifth), Lower(sixth), Lower(seventh), selector);

    public Plan<TIn, TOut> Sequence<TIn, T1, T2, T3, T4, T5, T6, T7, T8, TOut>(Parser<TIn, T1> first, Parser<TIn, T2> second, Parser<TIn, T3> third, Parser<TIn, T4> fourth, Parser<TIn, T5> fifth, Parser<TIn, T6> sixth, Parser<TIn, T7> seventh, Parser<TIn, T8> eighth, Func<T1, T2, T3, T4, T5, T6, T7, T8, TOut> selector) =>
        new SequencePlan<TIn, T1, T2, T3, T4, T5, T6, T7, T8, TOut>(Lower(first), Lower(second), Lower(third), Lower(fourth), Lower(fifth), Lower(sixth), Lower(seventh), Lower(eighth), selector);

    public Plan<char, ReadOnlyMemory<char>> Slice<T>(Parser<char, T> item) => new SlicePlan<T>(Lower(item));

    public Plan<char, string> Text<T>(Parser<char, T> item) => new TextPlan<T>(Lower(item));
}
