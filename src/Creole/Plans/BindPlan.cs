namespace Creole;

/// <summary>
/// Runs its source, asks the continuation for the parser to run next, runs that parser and combines both results. Fails as either parser does.
/// </summary>
/// <remarks>
/// The chosen parser's plan comes from <see cref="PlanCache"/>, so a parser the continuation returns again, such as a rule field, is lowered once.
/// A parser the continuation builds afresh at each match is lowered at each match.
/// </remarks>
internal sealed class BindPlan<TIn, TSource, TNext, TResult>(Plan<TIn, TSource> source, Func<TSource, Parser<TIn, TNext>> next, Func<TSource, TNext, TResult> selector, string nextExpression) : Plan<TIn, TResult>
{
    private readonly Func<TSource, Parser<TIn, TNext>> _next = next;
    private readonly string _nextExpression = nextExpression;
    private readonly Func<TSource, TNext, TResult> _selector = selector;
    private readonly Plan<TIn, TSource> _source = source;

    /// <exception cref="GrammarException">The continuation returned null, or lowering the parser it returned failed.</exception>
    public override bool TryParse(ref Cursor<TIn> cursor, ref TResult result)
    {
        TSource value = default!;

        if (!_source.TryParse(ref cursor, ref value))
        {
            return false;
        }

        var parser = _next(value) ?? throw new GrammarException($"'{_nextExpression}' returned null instead of a parser.");
        TNext nextValue = default!;

        if (!PlanCache.Get(parser).TryParse(ref cursor, ref nextValue))
        {
            return false;
        }

        result = _selector(value, nextValue);
        return true;
    }
}
