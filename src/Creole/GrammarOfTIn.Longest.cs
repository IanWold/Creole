using System.Runtime.CompilerServices;

namespace Creole;

public abstract partial class Grammar<TIn>
{
    /// <summary>
    /// A parser that runs 2 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
        ]);

    /// <summary>
    /// A parser that runs 3 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
        ]);

    /// <summary>
    /// A parser that runs 4 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
        ]);

    /// <summary>
    /// A parser that runs 5 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
        ]);

    /// <summary>
    /// A parser that runs 6 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative6">The sixth alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative6Expression">
    /// The caller's expression for <paramref name="alternative6"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        Parser<TIn, TOut> alternative6,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = "",
        [CallerArgumentExpression(nameof(alternative6))] string alternative6Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
            Guard.NotNull(alternative6, alternative6Expression),
        ]);

    /// <summary>
    /// A parser that runs 7 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative6">The sixth alternative.</param>
    /// <param name="alternative7">The seventh alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative6Expression">
    /// The caller's expression for <paramref name="alternative6"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative7Expression">
    /// The caller's expression for <paramref name="alternative7"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        Parser<TIn, TOut> alternative6,
        Parser<TIn, TOut> alternative7,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = "",
        [CallerArgumentExpression(nameof(alternative6))] string alternative6Expression = "",
        [CallerArgumentExpression(nameof(alternative7))] string alternative7Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
            Guard.NotNull(alternative6, alternative6Expression),
            Guard.NotNull(alternative7, alternative7Expression),
        ]);

    /// <summary>
    /// A parser that runs 8 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative6">The sixth alternative.</param>
    /// <param name="alternative7">The seventh alternative.</param>
    /// <param name="alternative8">The eighth alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative6Expression">
    /// The caller's expression for <paramref name="alternative6"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative7Expression">
    /// The caller's expression for <paramref name="alternative7"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative8Expression">
    /// The caller's expression for <paramref name="alternative8"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        Parser<TIn, TOut> alternative6,
        Parser<TIn, TOut> alternative7,
        Parser<TIn, TOut> alternative8,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = "",
        [CallerArgumentExpression(nameof(alternative6))] string alternative6Expression = "",
        [CallerArgumentExpression(nameof(alternative7))] string alternative7Expression = "",
        [CallerArgumentExpression(nameof(alternative8))] string alternative8Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
            Guard.NotNull(alternative6, alternative6Expression),
            Guard.NotNull(alternative7, alternative7Expression),
            Guard.NotNull(alternative8, alternative8Expression),
        ]);

    /// <summary>
    /// A parser that runs 9 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative6">The sixth alternative.</param>
    /// <param name="alternative7">The seventh alternative.</param>
    /// <param name="alternative8">The eighth alternative.</param>
    /// <param name="alternative9">The ninth alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative6Expression">
    /// The caller's expression for <paramref name="alternative6"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative7Expression">
    /// The caller's expression for <paramref name="alternative7"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative8Expression">
    /// The caller's expression for <paramref name="alternative8"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative9Expression">
    /// The caller's expression for <paramref name="alternative9"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        Parser<TIn, TOut> alternative6,
        Parser<TIn, TOut> alternative7,
        Parser<TIn, TOut> alternative8,
        Parser<TIn, TOut> alternative9,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = "",
        [CallerArgumentExpression(nameof(alternative6))] string alternative6Expression = "",
        [CallerArgumentExpression(nameof(alternative7))] string alternative7Expression = "",
        [CallerArgumentExpression(nameof(alternative8))] string alternative8Expression = "",
        [CallerArgumentExpression(nameof(alternative9))] string alternative9Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
            Guard.NotNull(alternative6, alternative6Expression),
            Guard.NotNull(alternative7, alternative7Expression),
            Guard.NotNull(alternative8, alternative8Expression),
            Guard.NotNull(alternative9, alternative9Expression),
        ]);

    /// <summary>
    /// A parser that runs 10 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative6">The sixth alternative.</param>
    /// <param name="alternative7">The seventh alternative.</param>
    /// <param name="alternative8">The eighth alternative.</param>
    /// <param name="alternative9">The ninth alternative.</param>
    /// <param name="alternative10">The tenth alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative6Expression">
    /// The caller's expression for <paramref name="alternative6"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative7Expression">
    /// The caller's expression for <paramref name="alternative7"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative8Expression">
    /// The caller's expression for <paramref name="alternative8"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative9Expression">
    /// The caller's expression for <paramref name="alternative9"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative10Expression">
    /// The caller's expression for <paramref name="alternative10"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        Parser<TIn, TOut> alternative6,
        Parser<TIn, TOut> alternative7,
        Parser<TIn, TOut> alternative8,
        Parser<TIn, TOut> alternative9,
        Parser<TIn, TOut> alternative10,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = "",
        [CallerArgumentExpression(nameof(alternative6))] string alternative6Expression = "",
        [CallerArgumentExpression(nameof(alternative7))] string alternative7Expression = "",
        [CallerArgumentExpression(nameof(alternative8))] string alternative8Expression = "",
        [CallerArgumentExpression(nameof(alternative9))] string alternative9Expression = "",
        [CallerArgumentExpression(nameof(alternative10))] string alternative10Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
            Guard.NotNull(alternative6, alternative6Expression),
            Guard.NotNull(alternative7, alternative7Expression),
            Guard.NotNull(alternative8, alternative8Expression),
            Guard.NotNull(alternative9, alternative9Expression),
            Guard.NotNull(alternative10, alternative10Expression),
        ]);

    /// <summary>
    /// A parser that runs 11 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative6">The sixth alternative.</param>
    /// <param name="alternative7">The seventh alternative.</param>
    /// <param name="alternative8">The eighth alternative.</param>
    /// <param name="alternative9">The ninth alternative.</param>
    /// <param name="alternative10">The tenth alternative.</param>
    /// <param name="alternative11">The eleventh alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative6Expression">
    /// The caller's expression for <paramref name="alternative6"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative7Expression">
    /// The caller's expression for <paramref name="alternative7"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative8Expression">
    /// The caller's expression for <paramref name="alternative8"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative9Expression">
    /// The caller's expression for <paramref name="alternative9"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative10Expression">
    /// The caller's expression for <paramref name="alternative10"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative11Expression">
    /// The caller's expression for <paramref name="alternative11"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        Parser<TIn, TOut> alternative6,
        Parser<TIn, TOut> alternative7,
        Parser<TIn, TOut> alternative8,
        Parser<TIn, TOut> alternative9,
        Parser<TIn, TOut> alternative10,
        Parser<TIn, TOut> alternative11,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = "",
        [CallerArgumentExpression(nameof(alternative6))] string alternative6Expression = "",
        [CallerArgumentExpression(nameof(alternative7))] string alternative7Expression = "",
        [CallerArgumentExpression(nameof(alternative8))] string alternative8Expression = "",
        [CallerArgumentExpression(nameof(alternative9))] string alternative9Expression = "",
        [CallerArgumentExpression(nameof(alternative10))] string alternative10Expression = "",
        [CallerArgumentExpression(nameof(alternative11))] string alternative11Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
            Guard.NotNull(alternative6, alternative6Expression),
            Guard.NotNull(alternative7, alternative7Expression),
            Guard.NotNull(alternative8, alternative8Expression),
            Guard.NotNull(alternative9, alternative9Expression),
            Guard.NotNull(alternative10, alternative10Expression),
            Guard.NotNull(alternative11, alternative11Expression),
        ]);

    /// <summary>
    /// A parser that runs 12 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative6">The sixth alternative.</param>
    /// <param name="alternative7">The seventh alternative.</param>
    /// <param name="alternative8">The eighth alternative.</param>
    /// <param name="alternative9">The ninth alternative.</param>
    /// <param name="alternative10">The tenth alternative.</param>
    /// <param name="alternative11">The eleventh alternative.</param>
    /// <param name="alternative12">The twelfth alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative6Expression">
    /// The caller's expression for <paramref name="alternative6"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative7Expression">
    /// The caller's expression for <paramref name="alternative7"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative8Expression">
    /// The caller's expression for <paramref name="alternative8"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative9Expression">
    /// The caller's expression for <paramref name="alternative9"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative10Expression">
    /// The caller's expression for <paramref name="alternative10"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative11Expression">
    /// The caller's expression for <paramref name="alternative11"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative12Expression">
    /// The caller's expression for <paramref name="alternative12"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        Parser<TIn, TOut> alternative6,
        Parser<TIn, TOut> alternative7,
        Parser<TIn, TOut> alternative8,
        Parser<TIn, TOut> alternative9,
        Parser<TIn, TOut> alternative10,
        Parser<TIn, TOut> alternative11,
        Parser<TIn, TOut> alternative12,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = "",
        [CallerArgumentExpression(nameof(alternative6))] string alternative6Expression = "",
        [CallerArgumentExpression(nameof(alternative7))] string alternative7Expression = "",
        [CallerArgumentExpression(nameof(alternative8))] string alternative8Expression = "",
        [CallerArgumentExpression(nameof(alternative9))] string alternative9Expression = "",
        [CallerArgumentExpression(nameof(alternative10))] string alternative10Expression = "",
        [CallerArgumentExpression(nameof(alternative11))] string alternative11Expression = "",
        [CallerArgumentExpression(nameof(alternative12))] string alternative12Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
            Guard.NotNull(alternative6, alternative6Expression),
            Guard.NotNull(alternative7, alternative7Expression),
            Guard.NotNull(alternative8, alternative8Expression),
            Guard.NotNull(alternative9, alternative9Expression),
            Guard.NotNull(alternative10, alternative10Expression),
            Guard.NotNull(alternative11, alternative11Expression),
            Guard.NotNull(alternative12, alternative12Expression),
        ]);

    /// <summary>
    /// A parser that runs 13 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative6">The sixth alternative.</param>
    /// <param name="alternative7">The seventh alternative.</param>
    /// <param name="alternative8">The eighth alternative.</param>
    /// <param name="alternative9">The ninth alternative.</param>
    /// <param name="alternative10">The tenth alternative.</param>
    /// <param name="alternative11">The eleventh alternative.</param>
    /// <param name="alternative12">The twelfth alternative.</param>
    /// <param name="alternative13">The thirteenth alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative6Expression">
    /// The caller's expression for <paramref name="alternative6"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative7Expression">
    /// The caller's expression for <paramref name="alternative7"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative8Expression">
    /// The caller's expression for <paramref name="alternative8"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative9Expression">
    /// The caller's expression for <paramref name="alternative9"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative10Expression">
    /// The caller's expression for <paramref name="alternative10"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative11Expression">
    /// The caller's expression for <paramref name="alternative11"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative12Expression">
    /// The caller's expression for <paramref name="alternative12"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative13Expression">
    /// The caller's expression for <paramref name="alternative13"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        Parser<TIn, TOut> alternative6,
        Parser<TIn, TOut> alternative7,
        Parser<TIn, TOut> alternative8,
        Parser<TIn, TOut> alternative9,
        Parser<TIn, TOut> alternative10,
        Parser<TIn, TOut> alternative11,
        Parser<TIn, TOut> alternative12,
        Parser<TIn, TOut> alternative13,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = "",
        [CallerArgumentExpression(nameof(alternative6))] string alternative6Expression = "",
        [CallerArgumentExpression(nameof(alternative7))] string alternative7Expression = "",
        [CallerArgumentExpression(nameof(alternative8))] string alternative8Expression = "",
        [CallerArgumentExpression(nameof(alternative9))] string alternative9Expression = "",
        [CallerArgumentExpression(nameof(alternative10))] string alternative10Expression = "",
        [CallerArgumentExpression(nameof(alternative11))] string alternative11Expression = "",
        [CallerArgumentExpression(nameof(alternative12))] string alternative12Expression = "",
        [CallerArgumentExpression(nameof(alternative13))] string alternative13Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
            Guard.NotNull(alternative6, alternative6Expression),
            Guard.NotNull(alternative7, alternative7Expression),
            Guard.NotNull(alternative8, alternative8Expression),
            Guard.NotNull(alternative9, alternative9Expression),
            Guard.NotNull(alternative10, alternative10Expression),
            Guard.NotNull(alternative11, alternative11Expression),
            Guard.NotNull(alternative12, alternative12Expression),
            Guard.NotNull(alternative13, alternative13Expression),
        ]);

    /// <summary>
    /// A parser that runs 14 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative6">The sixth alternative.</param>
    /// <param name="alternative7">The seventh alternative.</param>
    /// <param name="alternative8">The eighth alternative.</param>
    /// <param name="alternative9">The ninth alternative.</param>
    /// <param name="alternative10">The tenth alternative.</param>
    /// <param name="alternative11">The eleventh alternative.</param>
    /// <param name="alternative12">The twelfth alternative.</param>
    /// <param name="alternative13">The thirteenth alternative.</param>
    /// <param name="alternative14">The fourteenth alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative6Expression">
    /// The caller's expression for <paramref name="alternative6"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative7Expression">
    /// The caller's expression for <paramref name="alternative7"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative8Expression">
    /// The caller's expression for <paramref name="alternative8"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative9Expression">
    /// The caller's expression for <paramref name="alternative9"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative10Expression">
    /// The caller's expression for <paramref name="alternative10"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative11Expression">
    /// The caller's expression for <paramref name="alternative11"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative12Expression">
    /// The caller's expression for <paramref name="alternative12"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative13Expression">
    /// The caller's expression for <paramref name="alternative13"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative14Expression">
    /// The caller's expression for <paramref name="alternative14"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        Parser<TIn, TOut> alternative6,
        Parser<TIn, TOut> alternative7,
        Parser<TIn, TOut> alternative8,
        Parser<TIn, TOut> alternative9,
        Parser<TIn, TOut> alternative10,
        Parser<TIn, TOut> alternative11,
        Parser<TIn, TOut> alternative12,
        Parser<TIn, TOut> alternative13,
        Parser<TIn, TOut> alternative14,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = "",
        [CallerArgumentExpression(nameof(alternative6))] string alternative6Expression = "",
        [CallerArgumentExpression(nameof(alternative7))] string alternative7Expression = "",
        [CallerArgumentExpression(nameof(alternative8))] string alternative8Expression = "",
        [CallerArgumentExpression(nameof(alternative9))] string alternative9Expression = "",
        [CallerArgumentExpression(nameof(alternative10))] string alternative10Expression = "",
        [CallerArgumentExpression(nameof(alternative11))] string alternative11Expression = "",
        [CallerArgumentExpression(nameof(alternative12))] string alternative12Expression = "",
        [CallerArgumentExpression(nameof(alternative13))] string alternative13Expression = "",
        [CallerArgumentExpression(nameof(alternative14))] string alternative14Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
            Guard.NotNull(alternative6, alternative6Expression),
            Guard.NotNull(alternative7, alternative7Expression),
            Guard.NotNull(alternative8, alternative8Expression),
            Guard.NotNull(alternative9, alternative9Expression),
            Guard.NotNull(alternative10, alternative10Expression),
            Guard.NotNull(alternative11, alternative11Expression),
            Guard.NotNull(alternative12, alternative12Expression),
            Guard.NotNull(alternative13, alternative13Expression),
            Guard.NotNull(alternative14, alternative14Expression),
        ]);

    /// <summary>
    /// A parser that runs 15 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative6">The sixth alternative.</param>
    /// <param name="alternative7">The seventh alternative.</param>
    /// <param name="alternative8">The eighth alternative.</param>
    /// <param name="alternative9">The ninth alternative.</param>
    /// <param name="alternative10">The tenth alternative.</param>
    /// <param name="alternative11">The eleventh alternative.</param>
    /// <param name="alternative12">The twelfth alternative.</param>
    /// <param name="alternative13">The thirteenth alternative.</param>
    /// <param name="alternative14">The fourteenth alternative.</param>
    /// <param name="alternative15">The fifteenth alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative6Expression">
    /// The caller's expression for <paramref name="alternative6"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative7Expression">
    /// The caller's expression for <paramref name="alternative7"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative8Expression">
    /// The caller's expression for <paramref name="alternative8"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative9Expression">
    /// The caller's expression for <paramref name="alternative9"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative10Expression">
    /// The caller's expression for <paramref name="alternative10"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative11Expression">
    /// The caller's expression for <paramref name="alternative11"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative12Expression">
    /// The caller's expression for <paramref name="alternative12"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative13Expression">
    /// The caller's expression for <paramref name="alternative13"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative14Expression">
    /// The caller's expression for <paramref name="alternative14"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative15Expression">
    /// The caller's expression for <paramref name="alternative15"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        Parser<TIn, TOut> alternative6,
        Parser<TIn, TOut> alternative7,
        Parser<TIn, TOut> alternative8,
        Parser<TIn, TOut> alternative9,
        Parser<TIn, TOut> alternative10,
        Parser<TIn, TOut> alternative11,
        Parser<TIn, TOut> alternative12,
        Parser<TIn, TOut> alternative13,
        Parser<TIn, TOut> alternative14,
        Parser<TIn, TOut> alternative15,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = "",
        [CallerArgumentExpression(nameof(alternative6))] string alternative6Expression = "",
        [CallerArgumentExpression(nameof(alternative7))] string alternative7Expression = "",
        [CallerArgumentExpression(nameof(alternative8))] string alternative8Expression = "",
        [CallerArgumentExpression(nameof(alternative9))] string alternative9Expression = "",
        [CallerArgumentExpression(nameof(alternative10))] string alternative10Expression = "",
        [CallerArgumentExpression(nameof(alternative11))] string alternative11Expression = "",
        [CallerArgumentExpression(nameof(alternative12))] string alternative12Expression = "",
        [CallerArgumentExpression(nameof(alternative13))] string alternative13Expression = "",
        [CallerArgumentExpression(nameof(alternative14))] string alternative14Expression = "",
        [CallerArgumentExpression(nameof(alternative15))] string alternative15Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
            Guard.NotNull(alternative6, alternative6Expression),
            Guard.NotNull(alternative7, alternative7Expression),
            Guard.NotNull(alternative8, alternative8Expression),
            Guard.NotNull(alternative9, alternative9Expression),
            Guard.NotNull(alternative10, alternative10Expression),
            Guard.NotNull(alternative11, alternative11Expression),
            Guard.NotNull(alternative12, alternative12Expression),
            Guard.NotNull(alternative13, alternative13Expression),
            Guard.NotNull(alternative14, alternative14Expression),
            Guard.NotNull(alternative15, alternative15Expression),
        ]);

    /// <summary>
    /// A parser that runs 16 alternatives from the same position and produces the result of the one that consumed the most input.
    /// </summary>
    /// <typeparam name="TOut">The type every alternative produces.</typeparam>
    /// <param name="alternative1">The first alternative.</param>
    /// <param name="alternative2">The second alternative.</param>
    /// <param name="alternative3">The third alternative.</param>
    /// <param name="alternative4">The fourth alternative.</param>
    /// <param name="alternative5">The fifth alternative.</param>
    /// <param name="alternative6">The sixth alternative.</param>
    /// <param name="alternative7">The seventh alternative.</param>
    /// <param name="alternative8">The eighth alternative.</param>
    /// <param name="alternative9">The ninth alternative.</param>
    /// <param name="alternative10">The tenth alternative.</param>
    /// <param name="alternative11">The eleventh alternative.</param>
    /// <param name="alternative12">The twelfth alternative.</param>
    /// <param name="alternative13">The thirteenth alternative.</param>
    /// <param name="alternative14">The fourteenth alternative.</param>
    /// <param name="alternative15">The fifteenth alternative.</param>
    /// <param name="alternative16">The sixteenth alternative.</param>
    /// <param name="alternative1Expression">
    /// The caller's expression for <paramref name="alternative1"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative2Expression">
    /// The caller's expression for <paramref name="alternative2"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative3Expression">
    /// The caller's expression for <paramref name="alternative3"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative4Expression">
    /// The caller's expression for <paramref name="alternative4"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative5Expression">
    /// The caller's expression for <paramref name="alternative5"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative6Expression">
    /// The caller's expression for <paramref name="alternative6"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative7Expression">
    /// The caller's expression for <paramref name="alternative7"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative8Expression">
    /// The caller's expression for <paramref name="alternative8"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative9Expression">
    /// The caller's expression for <paramref name="alternative9"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative10Expression">
    /// The caller's expression for <paramref name="alternative10"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative11Expression">
    /// The caller's expression for <paramref name="alternative11"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative12Expression">
    /// The caller's expression for <paramref name="alternative12"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative13Expression">
    /// The caller's expression for <paramref name="alternative13"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative14Expression">
    /// The caller's expression for <paramref name="alternative14"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative15Expression">
    /// The caller's expression for <paramref name="alternative15"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <param name="alternative16Expression">
    /// The caller's expression for <paramref name="alternative16"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>A parser that produces the result of the longest match.</returns>
    /// <exception cref="GrammarException">An alternative is null.</exception>
    /// <remarks>
    /// Every alternative runs atomically from the same position: one that fails, even after consuming input, does not match, and the input it read is given back.
    /// Of the alternatives that match, the one that consumed the most input wins; a tie goes to the earliest.
    /// If every alternative fails, the parser fails without consuming input.
    /// For more than 16 alternatives, nest <c>Longest</c> calls: <c>Longest(a, Longest(b, c))</c> matches the same input as <c>Longest(a, b, c)</c>.
    /// </remarks>
    public static Parser<TIn, TOut> Longest<TOut>(
        Parser<TIn, TOut> alternative1,
        Parser<TIn, TOut> alternative2,
        Parser<TIn, TOut> alternative3,
        Parser<TIn, TOut> alternative4,
        Parser<TIn, TOut> alternative5,
        Parser<TIn, TOut> alternative6,
        Parser<TIn, TOut> alternative7,
        Parser<TIn, TOut> alternative8,
        Parser<TIn, TOut> alternative9,
        Parser<TIn, TOut> alternative10,
        Parser<TIn, TOut> alternative11,
        Parser<TIn, TOut> alternative12,
        Parser<TIn, TOut> alternative13,
        Parser<TIn, TOut> alternative14,
        Parser<TIn, TOut> alternative15,
        Parser<TIn, TOut> alternative16,
        [CallerArgumentExpression(nameof(alternative1))] string alternative1Expression = "",
        [CallerArgumentExpression(nameof(alternative2))] string alternative2Expression = "",
        [CallerArgumentExpression(nameof(alternative3))] string alternative3Expression = "",
        [CallerArgumentExpression(nameof(alternative4))] string alternative4Expression = "",
        [CallerArgumentExpression(nameof(alternative5))] string alternative5Expression = "",
        [CallerArgumentExpression(nameof(alternative6))] string alternative6Expression = "",
        [CallerArgumentExpression(nameof(alternative7))] string alternative7Expression = "",
        [CallerArgumentExpression(nameof(alternative8))] string alternative8Expression = "",
        [CallerArgumentExpression(nameof(alternative9))] string alternative9Expression = "",
        [CallerArgumentExpression(nameof(alternative10))] string alternative10Expression = "",
        [CallerArgumentExpression(nameof(alternative11))] string alternative11Expression = "",
        [CallerArgumentExpression(nameof(alternative12))] string alternative12Expression = "",
        [CallerArgumentExpression(nameof(alternative13))] string alternative13Expression = "",
        [CallerArgumentExpression(nameof(alternative14))] string alternative14Expression = "",
        [CallerArgumentExpression(nameof(alternative15))] string alternative15Expression = "",
        [CallerArgumentExpression(nameof(alternative16))] string alternative16Expression = ""
    ) =>
        new LongestNode<TIn, TOut>(
        [
            Guard.NotNull(alternative1, alternative1Expression),
            Guard.NotNull(alternative2, alternative2Expression),
            Guard.NotNull(alternative3, alternative3Expression),
            Guard.NotNull(alternative4, alternative4Expression),
            Guard.NotNull(alternative5, alternative5Expression),
            Guard.NotNull(alternative6, alternative6Expression),
            Guard.NotNull(alternative7, alternative7Expression),
            Guard.NotNull(alternative8, alternative8Expression),
            Guard.NotNull(alternative9, alternative9Expression),
            Guard.NotNull(alternative10, alternative10Expression),
            Guard.NotNull(alternative11, alternative11Expression),
            Guard.NotNull(alternative12, alternative12Expression),
            Guard.NotNull(alternative13, alternative13Expression),
            Guard.NotNull(alternative14, alternative14Expression),
            Guard.NotNull(alternative15, alternative15Expression),
            Guard.NotNull(alternative16, alternative16Expression),
        ]);
}
