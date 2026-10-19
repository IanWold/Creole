using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Choice combinators, called as methods on a parser: <c>p.Or(q)</c>.
/// </summary>
/// <remarks>
/// <para>
/// Choice uses commit semantics.
/// An alternative is tried only if the one before it failed without consuming input.
/// An alternative that fails after consuming input fails the whole choice.
/// </para>
/// <para>
/// <c>OneOf</c>, for more than two alternatives, is a member of <see cref="Grammar{TIn}"/>.
/// </para>
/// </remarks>
public static partial class Choice
{
    extension<TIn, TOut>(Parser<TIn, TOut> parser)
    {
        /// <summary>
        /// A parser that tries this parser, and then <paramref name="other"/> if this parser fails without consuming input.
        /// </summary>
        /// <param name="other">The alternative to try second.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="otherExpression">
        /// The caller's expression for <paramref name="other"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the result of whichever alternative matched.</returns>
        /// <exception cref="GrammarException">This parser or <paramref name="other"/> is null.</exception>
        /// <remarks>
        /// If this parser fails after consuming input, <paramref name="other"/> is not tried and the choice fails.
        /// <c>a.Or(b).Or(c)</c> matches the same input as <c>OneOf(a, b, c)</c>.
        /// </remarks>
        public Parser<TIn, TOut> Or(
            Parser<TIn, TOut> other,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(other))] string otherExpression = ""
        ) =>
            new OrNode<TIn, TOut>([Guard.NotNull(parser, parserExpression), Guard.NotNull(other, otherExpression)]);
    }
}
