using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Repetition combinators, called as methods on a parser: <c>p.Many()</c>, <c>p.SepBy(comma)</c>.
/// </summary>
/// <remarks>
/// <para>
/// Each attempt that fails without consuming input ends the repetition.
/// An attempt that fails after consuming input fails the whole repetition.
/// </para>
/// <para>
/// These are extension members rather than instance methods of <see cref="Parser{TIn, TOut}"/>.
/// An instance method that builds a <c>Parser&lt;TIn, IReadOnlyList&lt;TOut&gt;&gt;</c> from a <c>Parser&lt;TIn, TOut&gt;</c>
/// makes Native AOT expand the parser type without end (IL3054).
/// </para>
/// </remarks>
public static class Repetition
{
    extension<TIn, TOut>(Parser<TIn, TOut> parser)
    {
        /// <summary>
        /// A parser that runs this parser zero or more times and produces the results in order.
        /// </summary>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the list of results, which is empty if this parser never matched.</returns>
        /// <exception cref="GrammarException">This parser is null.</exception>
        /// <remarks>
        /// Repetition stops at the first attempt that fails without consuming input.
        /// An attempt that fails after consuming input fails the whole repetition.
        /// If this parser succeeds without consuming input, the parse throws a <see cref="GrammarException"/> when it reaches that point, because the repetition would never end.
        /// <c>Repeat(min, max)</c> is bounded, so it does not throw.
        /// </remarks>
        public Parser<TIn, IReadOnlyList<TOut>> Many([CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            new RepeatNode<TIn, TOut>(Guard.NotNull(parser, parserExpression), 0, null);

        /// <summary>
        /// A parser that runs this parser one or more times and produces the results in order.
        /// </summary>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the list of results, which is never empty.</returns>
        /// <exception cref="GrammarException">This parser is null.</exception>
        /// <remarks>
        /// Like <see cref="Many"/>, but fails if this parser does not match at least once.
        /// If this parser succeeds without consuming input, the parse throws a <see cref="GrammarException"/> when it reaches that point, because the repetition would never end.
        /// <c>Repeat(min, max)</c> is bounded, so it does not throw.
        /// </remarks>
        public Parser<TIn, IReadOnlyList<TOut>> AtLeastOnce([CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            new RepeatNode<TIn, TOut>(Guard.NotNull(parser, parserExpression), 1, null);

        /// <summary>
        /// A parser that runs this parser exactly <paramref name="count"/> times and produces the results in order.
        /// </summary>
        /// <param name="count">The number of times to run this parser.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the list of <paramref name="count"/> results.</returns>
        /// <exception cref="GrammarException">This parser is null, or <paramref name="count"/> is negative.</exception>
        /// <remarks>
        /// The same as <c>Repeat(count, count)</c>. It fails if any of the <paramref name="count"/> attempts fails.
        /// </remarks>
        public Parser<TIn, IReadOnlyList<TOut>> Repeat(int count, [CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            parser.Repeat(count, count, parserExpression);

        /// <summary>
        /// A parser that runs this parser at least <paramref name="min"/> and at most <paramref name="max"/> times and produces the results in order.
        /// </summary>
        /// <param name="min">The fewest times this parser must match.</param>
        /// <param name="max">The most times this parser is run.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the list of results.</returns>
        /// <exception cref="GrammarException">
        /// This parser is null, <paramref name="min"/> is negative, or <paramref name="max"/> is less than <paramref name="min"/>.
        /// </exception>
        /// <remarks>
        /// Repetition is greedy: it reads as many items as it can, up to <paramref name="max"/>, and does not try fewer.
        /// It stops at the first attempt that fails without consuming input, and fails if that leaves fewer than <paramref name="min"/> results.
        /// An attempt that fails after consuming input fails the whole repetition.
        /// </remarks>
        public Parser<TIn, IReadOnlyList<TOut>> Repeat(int min, int max, [CallerArgumentExpression(nameof(parser))] string parserExpression = "")
        {
            Guard.NotNull(parser, parserExpression);
            CheckCounts(min, max);
            return new RepeatNode<TIn, TOut>(parser, min, max);
        }

        /// <summary>
        /// A parser that runs this parser zero or more times with <paramref name="separator"/> between the matches, and produces this parser's results in order.
        /// </summary>
        /// <typeparam name="TSep">The type of value the separator produces. It is discarded.</typeparam>
        /// <param name="separator">The parser that matches between items, such as a comma.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="separatorExpression">
        /// The caller's expression for <paramref name="separator"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the list of items.</returns>
        /// <exception cref="GrammarException">This parser or <paramref name="separator"/> is null.</exception>
        /// <remarks>
        /// A separator that fails without consuming input ends the list.
        /// A separator must be followed by an item, so <c>a,</c> fails if the separator consumed the comma.
        /// A trailing separator is accepted by <c>SepEndBy</c>.
        /// If a separator and the item after it both succeed without consuming input, the parse throws a <see cref="GrammarException"/> when it reaches that point, because the list would never end.
        /// </remarks>
        public Parser<TIn, IReadOnlyList<TOut>> SepBy<TSep>(
            Parser<TIn, TSep> separator,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(separator))] string separatorExpression = ""
        ) =>
            new SeparatedNode<TIn, TOut, TSep>(Guard.NotNull(parser, parserExpression), Guard.NotNull(separator, separatorExpression), 0, false);

        /// <summary>
        /// A parser that runs this parser one or more times with <paramref name="separator"/> between the matches, and produces this parser's results in order.
        /// </summary>
        /// <typeparam name="TSep">The type of value the separator produces. It is discarded.</typeparam>
        /// <param name="separator">The parser that matches between items, such as a comma.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="separatorExpression">
        /// The caller's expression for <paramref name="separator"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the list of items, which is never empty.</returns>
        /// <exception cref="GrammarException">This parser or <paramref name="separator"/> is null.</exception>
        /// <remarks>
        /// Like <c>SepBy</c>, but fails if there is not at least one item.
        /// If a separator and the item after it both succeed without consuming input, the parse throws a <see cref="GrammarException"/> when it reaches that point, because the list would never end.
        /// </remarks>
        public Parser<TIn, IReadOnlyList<TOut>> SepBy1<TSep>(
            Parser<TIn, TSep> separator,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(separator))] string separatorExpression = ""
        ) =>
            new SeparatedNode<TIn, TOut, TSep>(Guard.NotNull(parser, parserExpression), Guard.NotNull(separator, separatorExpression), 1, false);

        /// <summary>
        /// A parser that runs this parser zero or more times with <paramref name="separator"/> between the matches and optionally after the last one, and produces this parser's results in order.
        /// </summary>
        /// <typeparam name="TSep">The type of value the separator produces. It is discarded.</typeparam>
        /// <param name="separator">The parser that matches between items, such as a comma.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="separatorExpression">
        /// The caller's expression for <paramref name="separator"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the list of items.</returns>
        /// <exception cref="GrammarException">This parser or <paramref name="separator"/> is null.</exception>
        /// <remarks>
        /// Like <c>SepBy</c>, but a separator may also follow the last item, so both <c>a,b</c> and <c>a,b,</c> match.
        /// An item that fails without consuming input after a separator ends the list.
        /// If a separator and the item after it both succeed without consuming input, the parse throws a <see cref="GrammarException"/> when it reaches that point, because the list would never end.
        /// </remarks>
        public Parser<TIn, IReadOnlyList<TOut>> SepEndBy<TSep>(
            Parser<TIn, TSep> separator,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(separator))] string separatorExpression = ""
        ) =>
            new SeparatedNode<TIn, TOut, TSep>(Guard.NotNull(parser, parserExpression), Guard.NotNull(separator, separatorExpression), 0, true);

        /// <summary>
        /// A parser that runs this parser zero or more times until <paramref name="terminator"/> matches, and produces this parser's results in order.
        /// </summary>
        /// <typeparam name="TTerm">The type of value the terminator produces. It is discarded.</typeparam>
        /// <param name="terminator">The parser that ends the repetition.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="terminatorExpression">
        /// The caller's expression for <paramref name="terminator"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the list of items before the terminator.</returns>
        /// <exception cref="GrammarException">This parser or <paramref name="terminator"/> is null.</exception>
        /// <remarks>
        /// Before each item, the terminator is tried first.
        /// If it matches, it is consumed and the repetition succeeds.
        /// If it fails without consuming input, this parser must match an item, or the whole repetition fails.
        /// A terminator that fails after consuming input also fails the whole repetition.
        /// If this parser succeeds without consuming input, the parse throws a <see cref="GrammarException"/> when it reaches that point, because the repetition would never end.
        /// </remarks>
        public Parser<TIn, IReadOnlyList<TOut>> Until<TTerm>(
            Parser<TIn, TTerm> terminator,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(terminator))] string terminatorExpression = ""
        ) =>
            new UntilNode<TIn, TOut, TTerm>(Guard.NotNull(parser, parserExpression), Guard.NotNull(terminator, terminatorExpression));

        /// <summary>
        /// A parser that runs this parser zero or more times and discards the results, without allocating.
        /// </summary>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces <see cref="Unit"/>.</returns>
        /// <exception cref="GrammarException">This parser is null.</exception>
        /// <remarks>
        /// Matches the same input as <see cref="Many"/>.
        /// If this parser succeeds without consuming input, the parse throws a <see cref="GrammarException"/> when it reaches that point, because the repetition would never end.
        /// <c>Repeat(min, max)</c> is bounded, so it does not throw.
        /// </remarks>
        public Parser<TIn, Unit> SkipMany([CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            new SkipRepeatNode<TIn, TOut>(Guard.NotNull(parser, parserExpression), 0, null);

        /// <summary>
        /// A parser that runs this parser one or more times and discards the results, without allocating.
        /// </summary>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces <see cref="Unit"/>.</returns>
        /// <exception cref="GrammarException">This parser is null.</exception>
        /// <remarks>
        /// Matches the same input as <see cref="AtLeastOnce"/>.
        /// If this parser succeeds without consuming input, the parse throws a <see cref="GrammarException"/> when it reaches that point, because the repetition would never end.
        /// <c>Repeat(min, max)</c> is bounded, so it does not throw.
        /// </remarks>
        public Parser<TIn, Unit> SkipAtLeastOnce([CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            new SkipRepeatNode<TIn, TOut>(Guard.NotNull(parser, parserExpression), 1, null);
    }

    private static void CheckCounts(int min, int max)
    {
        if (min < 0)
        {
            throw new GrammarException($"Repeat count {min} is negative.");
        }

        if (max < min)
        {
            throw new GrammarException($"Repeat maximum {max} is less than the minimum {min}.");
        }
    }
}
