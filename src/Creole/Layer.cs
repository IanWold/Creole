using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Makes layers: <c>Layer.Tokens(Token, skip: Whitespace.SkipMany())</c>, <c>Layer.Where&lt;Tok&gt;(t =&gt; t.Kind != K.Comment)</c>.
/// </summary>
public static class Layer
{
    /// <summary>
    /// The layer that passes its items through unchanged: the unit of <see cref="Layer{TIn, TOut}.Then{TNext}"/>.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <returns>A layer whose items, and their spans, are its input's.</returns>
    /// <remarks>
    /// Chaining a layer with it returns that layer, so it adds no buffer to a chain.
    /// </remarks>
    public static Layer<T, T> Identity<T>() => IdentityLayer<T>.Instance;

    /// <summary>
    /// A layer that applies <paramref name="selector"/> to each item.
    /// </summary>
    /// <typeparam name="TIn">The type of item the layer reads.</typeparam>
    /// <typeparam name="TOut">The type of item the layer produces.</typeparam>
    /// <param name="selector">The function from each input item to the item produced in its place.</param>
    /// <param name="selectorExpression">Filled in by the compiler with the expression for <paramref name="selector"/>.</param>
    /// <returns>A layer that produces one item for each input item, with that input item's span.</returns>
    /// <exception cref="GrammarException"><paramref name="selector"/> is null.</exception>
    /// <remarks>
    /// It runs <paramref name="selector"/> once for each input item, when the layer above first needs the result.
    /// </remarks>
    public static Layer<TIn, TOut> Select<TIn, TOut>(Func<TIn, TOut> selector, [CallerArgumentExpression(nameof(selector))] string selectorExpression = "") =>
        new SelectLayer<TIn, TOut>(Guard.NotNull(selector, selectorExpression));

    /// <summary>
    /// A layer that runs <paramref name="item"/> repeatedly to the end of its input and produces each value, such as a lexer producing tokens.
    /// </summary>
    /// <typeparam name="TIn">The type of item the layer reads, such as <see cref="char"/>.</typeparam>
    /// <typeparam name="TOut">The type of item the layer produces, such as a token type.</typeparam>
    /// <param name="item">The parser for one item, usually a <c>Longest</c> over the token rules.</param>
    /// <param name="name">The layer's name in errors, which <see cref="ParseError.Layer"/> holds.</param>
    /// <param name="itemExpression">Filled in by the compiler with the expression for <paramref name="item"/>.</param>
    /// <returns>A layer that produces the value of each run of <paramref name="item"/>.</returns>
    /// <exception cref="GrammarException"><paramref name="item"/> is null, or <paramref name="name"/> is null or empty.</exception>
    /// <remarks>
    /// <para>
    /// Each step stops if the input has ended, and otherwise runs <paramref name="item"/> and produces its value.
    /// The item's span runs from the start of the first input item it consumed to the end of the last.
    /// </para>
    /// <para>
    /// If <paramref name="item"/> fails, the parse ends with an error from this layer: the furthest failure its steps recorded.
    /// If <paramref name="item"/> succeeds without consuming input, the layer would never reach the end of its input, so it throws <see cref="GrammarException"/>.
    /// </para>
    /// </remarks>
    public static Layer<TIn, TOut> Tokens<TIn, TOut>(Parser<TIn, TOut> item, string name = "lexer", [CallerArgumentExpression(nameof(item))] string itemExpression = "") =>
        new TokensLayer<TIn, TOut>(Guard.NotNull(item, itemExpression), skip: null, Guard.NotNullOrEmpty(name));

    /// <summary>
    /// A layer that runs <paramref name="skip"/> and then <paramref name="item"/> repeatedly to the end of its input and produces each value of <paramref name="item"/>, such as a lexer that skips whitespace between tokens.
    /// </summary>
    /// <typeparam name="TIn">The type of item the layer reads, such as <see cref="char"/>.</typeparam>
    /// <typeparam name="TOut">The type of item the layer produces, such as a token type.</typeparam>
    /// <param name="item">The parser for one item, usually a <c>Longest</c> over the token rules.</param>
    /// <param name="skip">The parser for the input to skip before each item, such as <c>Whitespace.SkipMany()</c>. It must succeed, so it usually repeats.</param>
    /// <param name="name">The layer's name in errors, which <see cref="ParseError.Layer"/> holds.</param>
    /// <param name="itemExpression">Filled in by the compiler with the expression for <paramref name="item"/>.</param>
    /// <param name="skipExpression">Filled in by the compiler with the expression for <paramref name="skip"/>.</param>
    /// <returns>A layer that produces the value of each run of <paramref name="item"/>.</returns>
    /// <exception cref="GrammarException"><paramref name="item"/> or <paramref name="skip"/> is null, or <paramref name="name"/> is null or empty.</exception>
    /// <remarks>
    /// <para>
    /// Each step runs <paramref name="skip"/>, stops if the input has ended, and otherwise runs <paramref name="item"/> and produces its value.
    /// The item's span runs from the start of the first input item <paramref name="item"/> consumed to the end of the last, so it never includes the skipped input before it.
    /// </para>
    /// <para>
    /// If <paramref name="skip"/> or <paramref name="item"/> fails, the parse ends with an error from this layer: the furthest failure its steps recorded.
    /// If <paramref name="item"/> succeeds without consuming input, the layer would never reach the end of its input, so it throws <see cref="GrammarException"/>.
    /// </para>
    /// </remarks>
    public static Layer<TIn, TOut> Tokens<TIn, TOut>(
        Parser<TIn, TOut> item,
        Parser<TIn, Unit> skip,
        string name = "lexer",
        [CallerArgumentExpression(nameof(item))] string itemExpression = "",
        [CallerArgumentExpression(nameof(skip))] string skipExpression = ""
    ) =>
        new TokensLayer<TIn, TOut>(Guard.NotNull(item, itemExpression), Guard.NotNull(skip, skipExpression), Guard.NotNullOrEmpty(name));

    /// <summary>
    /// A layer that keeps the items for which <paramref name="predicate"/> is true and drops the rest.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <param name="predicate">The test each item must pass to be kept.</param>
    /// <param name="predicateExpression">Filled in by the compiler with the expression for <paramref name="predicate"/>.</param>
    /// <returns>A layer that produces the kept items, each with its span.</returns>
    /// <exception cref="GrammarException"><paramref name="predicate"/> is null.</exception>
    /// <remarks>
    /// It runs <paramref name="predicate"/> once for each input item, when the layer above first needs an item it might keep.
    /// </remarks>
    public static Layer<T, T> Where<T>(Func<T, bool> predicate, [CallerArgumentExpression(nameof(predicate))] string predicateExpression = "") =>
        new WhereLayer<T>(Guard.NotNull(predicate, predicateExpression));
}
