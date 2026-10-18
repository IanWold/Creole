using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Operations on whole grammars that do not depend on a grammar's input type.
/// </summary>
public static class Grammar
{
    /// <summary>
    /// Asserts that <paramref name="parser"/> is LL(1), so engines can dispatch every choice on one item of lookahead and parse in linear time.
    /// </summary>
    /// <typeparam name="TIn">The input item type.</typeparam>
    /// <typeparam name="TOut">The type of value the parser produces.</typeparam>
    /// <param name="parser">The parser to check.</param>
    /// <param name="parserExpression">
    /// The caller's expression for <paramref name="parser"/>, used in the error message.
    /// The compiler fills it in; do not pass it.
    /// </param>
    /// <returns>The parser.</returns>
    /// <exception cref="GrammarException"><paramref name="parser"/> is null.</exception>
    /// <remarks>
    /// The LL(1) check is not implemented yet. For now this returns <paramref name="parser"/> unchanged.
    /// </remarks>
    public static Parser<TIn, TOut> Deterministic<TIn, TOut>(Parser<TIn, TOut> parser, [CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
        Guard.NotNull(parser, parserExpression);
}
