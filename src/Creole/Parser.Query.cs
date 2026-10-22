using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Creole;

public abstract partial class Parser<TIn, TOut>
{
    /// <summary>
    /// A parser that runs this parser, passes its result to <paramref name="next"/> to choose the parser to run after it, and produces that parser's result.
    /// </summary>
    /// <typeparam name="TNext">The type of value the chosen parser produces.</typeparam>
    /// <param name="next">Chooses the parser to run from this parser's result. It runs once per match.</param>
    /// <param name="nextExpression">Filled in by the compiler with the expression passed as <paramref name="next"/>.</param>
    /// <returns>A bind node over this parser.</returns>
    /// <exception cref="GrammarException"><paramref name="next"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Use it only when the grammar genuinely depends on a parsed value, such as a length prefix followed by that many items.
    /// The parser <paramref name="next"/> returns is code, not part of the grammar, so analysis cannot see past this point.
    /// Where the value only chooses between known parsers, <c>Switch</c> or <c>Match</c> keeps every branch visible.
    /// </para>
    /// <para>
    /// A failure after this parser consumed input is committed, as in a sequence.
    /// </para>
    /// </remarks>
    public Parser<TIn, TNext> Bind<TNext>(Func<TOut, Parser<TIn, TNext>> next, [CallerArgumentExpression(nameof(next))] string nextExpression = "") =>
        new BindNode<TIn, TOut, TNext, TNext>(this, Guard.NotNull(next, nextExpression), static (_, value) => value);

    /// <summary>
    /// Runs this parser, then the parser <paramref name="selector"/> gives, and combines their results with <paramref name="resultSelector"/>.
    /// C# calls it for each <c>from</c> clause after the first in a query.
    /// </summary>
    /// <typeparam name="TNext">The type of value the clause's parser produces.</typeparam>
    /// <typeparam name="TResult">The type of value <paramref name="resultSelector"/> produces.</typeparam>
    /// <param name="selector">The clause: an expression giving the parser to run next, which may use this parser's result.</param>
    /// <param name="resultSelector">Combines the two results. It runs once per match.</param>
    /// <param name="selectorExpression">Filled in by the compiler with the expression passed as <paramref name="selector"/>; in a query, the clause's text.</param>
    /// <param name="resultSelectorExpression">Filled in by the compiler with the expression passed as <paramref name="resultSelector"/>.</param>
    /// <returns>
    /// A sequence node if the clause does not use this parser's result, so analysis sees it like <c>Seq</c>.
    /// Otherwise a bind node, as <see cref="Bind{TNext}"/> builds.
    /// </returns>
    /// <exception cref="GrammarException">
    /// <paramref name="selector"/> or <paramref name="resultSelector"/> is null, or the clause does not use this parser's result and gives a null parser.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The clause is classified once, here.
    /// If it does not mention its parameter, it is evaluated once now and its parser becomes the second step of a sequence.
    /// If it does, it is compiled once now and runs once per match, like <see cref="Bind{TNext}"/>.
    /// Parsing never touches the expression tree.
    /// </para>
    /// <para>
    /// Under Native AOT, expressions are interpreted, not compiled.
    /// That costs nothing for a clause evaluated once, but a clause that uses an earlier range variable runs on the expression interpreter at every match.
    /// </para>
    /// </remarks>
    public Parser<TIn, TResult> SelectMany<TNext, TResult>(
        Expression<Func<TOut, Parser<TIn, TNext>>> selector,
        Func<TOut, TNext, TResult> resultSelector,
        [CallerArgumentExpression(nameof(selector))] string selectorExpression = "",
        [CallerArgumentExpression(nameof(resultSelector))] string resultSelectorExpression = ""
    )
    {
        Guard.NotNull(selector, selectorExpression);
        Guard.NotNull(resultSelector, resultSelectorExpression);

        if (ParameterFinder.Uses(selector.Body, selector.Parameters[0]))
        {
            return new BindNode<TIn, TOut, TNext, TResult>(this, selector.Compile(), resultSelector);
        }

        var next = selector.Compile(preferInterpretation: true)(default!);

        return new SequenceNode<TIn, TOut, TNext, TResult>(this, Guard.NotNull(next, selectorExpression), resultSelector);
    }

    /// <summary>
    /// A parser that runs this parser and fails if <paramref name="predicate"/> rejects its result.
    /// C# calls it for the <c>where</c> clause of a query.
    /// </summary>
    /// <param name="predicate">Accepts or rejects this parser's result. It runs once per match and should not have side effects.</param>
    /// <param name="predicateExpression">Filled in by the compiler with the expression passed as <paramref name="predicate"/>.</param>
    /// <returns>A parser that matches the same input as this parser when <paramref name="predicate"/> accepts the result.</returns>
    /// <exception cref="GrammarException"><paramref name="predicate"/> is null.</exception>
    /// <remarks>
    /// If this parser consumed input before <paramref name="predicate"/> rejects its result, the failure is committed, so an enclosing choice does not try its next alternative.
    /// Wrap the filtered parser in <c>Atomic</c> to let the choice backtrack instead.
    /// </remarks>
    public Parser<TIn, TOut> Where(Func<TOut, bool> predicate, [CallerArgumentExpression(nameof(predicate))] string predicateExpression = "") =>
        new FilterNode<TIn, TOut>(this, Guard.NotNull(predicate, predicateExpression));
}

file sealed class ParameterFinder(ParameterExpression parameter) : ExpressionVisitor
{
    private bool _found;

    protected override Expression VisitParameter(ParameterExpression node)
    {
        _found |= node == parameter;

        return node;
    }

    public static bool Uses(Expression body, ParameterExpression parameter)
    {
        var finder = new ParameterFinder(parameter);
        finder.Visit(body);

        return finder._found;
    }

    public override Expression? Visit(Expression? node) =>
        _found ? node : base.Visit(node);
}
