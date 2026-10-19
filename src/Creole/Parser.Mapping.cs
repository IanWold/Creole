using System.Runtime.CompilerServices;

namespace Creole;

public abstract partial class Parser<TIn, TOut>
{
    /// <summary>
    /// A parser that runs this parser and produces <paramref name="value"/> in place of its result.
    /// </summary>
    /// <typeparam name="TResult">The type of the value produced. Give it explicitly to target a union or a base type: <c>Kind(J.True).As&lt;JsonValue&gt;(JsonBool.True)</c>.</typeparam>
    /// <param name="value">The value to produce.</param>
    /// <returns>A parser that matches the same input as this parser.</returns>
    public Parser<TIn, TResult> As<TResult>(TResult value) =>
        new MapNode<TIn, TOut, TResult>(this, _ => value);

    /// <summary>
    /// A parser that runs this parser and discards its result.
    /// </summary>
    /// <returns>A parser that matches the same input as this parser and produces <see cref="Unit"/>.</returns>
    public Parser<TIn, Unit> Ignore() =>
        new MapNode<TIn, TOut, Unit>(this, static _ => default);

    /// <summary>
    /// A parser that runs this parser and transforms its result with <paramref name="selector"/>.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of value <paramref name="selector"/> produces.
    /// Give it explicitly to let a union's implicit conversion apply: <c>Kind(J.String).Select&lt;JsonValue&gt;(t =&gt; new JsonString(t.Text))</c>.
    /// </typeparam>
    /// <param name="selector">Transforms this parser's result. It runs once per match and should not have side effects.</param>
    /// <param name="selectorExpression">Filled in by the compiler with the expression passed as <paramref name="selector"/>.</param>
    /// <returns>A parser that matches the same input as this parser.</returns>
    /// <exception cref="GrammarException"><paramref name="selector"/> is null.</exception>
    /// <remarks>
    /// On a tuple-valued parser such as <c>Seq(p, q)</c>, a lambda with one parameter per element calls the <see cref="Sequencing"/> overload instead.
    /// </remarks>
    public Parser<TIn, TResult> Select<TResult>(Func<TOut, TResult> selector, [CallerArgumentExpression(nameof(selector))] string selectorExpression = "") =>
        new MapNode<TIn, TOut, TResult>(this, Guard.NotNull(selector, selectorExpression));
}
