using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Source spans in grammars, called as methods on a parser: <c>call.Spanned()</c>, <c>call.WithSpan((value, span) =&gt; new Call(value, span))</c>.
/// </summary>
/// <remarks>
/// <para>
/// A span runs from the start of the first item a parser consumed to the end of the last, in the original source, whatever layer the parser reads.
/// Over input held whole in memory, such as a <see cref="string"/> or an array of tokens, its offsets are item positions.
/// </para>
/// <para>
/// These are extension members rather than instance methods of <see cref="Parser{TIn, TOut}"/>.
/// An instance method that builds a <c>Parser&lt;TIn, Spanned&lt;TOut&gt;&gt;</c> from a <c>Parser&lt;TIn, TOut&gt;</c> makes Native AOT expand the parser type without end (IL3054).
/// </para>
/// </remarks>
public static class SpanCapture
{
    extension<TIn, TOut>(Parser<TIn, TOut> parser)
    {
        /// <summary>
        /// A parser that runs this parser and produces its result with the source span of the input it consumed.
        /// </summary>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>A parser that matches the same input as this parser, and fails as it does.</returns>
        /// <exception cref="GrammarException">This parser is null.</exception>
        /// <remarks>
        /// If this parser consumes nothing, the span is zero-width where <see cref="Grammar{TIn}.Position"/> would be.
        /// Over a layer's items, the span leaves out input the layer skipped, such as whitespace before the first token.
        /// </remarks>
        public Parser<TIn, Spanned<TOut>> Spanned([CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            new SpannedNode<TIn, TOut>(Guard.NotNull(parser, parserExpression));

        /// <summary>
        /// A parser that runs this parser and transforms its result and the source span of the input it consumed with <paramref name="selector"/>, typically into a syntax node that keeps its location.
        /// </summary>
        /// <typeparam name="TResult">The type of value <paramref name="selector"/> produces.</typeparam>
        /// <param name="selector">Transforms this parser's result and its span. It must be pure: an engine may run it more than once, or not at all when the parse does not use the result.</param>
        /// <param name="selectorExpression">Filled in by the compiler with the expression passed as <paramref name="selector"/>.</param>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>A parser that matches the same input as this parser, and fails as it does.</returns>
        /// <exception cref="GrammarException">This parser or <paramref name="selector"/> is null.</exception>
        /// <remarks>
        /// The span is the one <see cref="Spanned"/> gives.
        /// </remarks>
        public Parser<TIn, TResult> WithSpan<TResult>(
            Func<TOut, SourceSpan, TResult> selector,
            [CallerArgumentExpression(nameof(selector))] string selectorExpression = "",
            [CallerArgumentExpression(nameof(parser))] string parserExpression = ""
        ) =>
            new WithSpanNode<TIn, TOut, TResult>(Guard.NotNull(parser, parserExpression), Guard.NotNull(selector, selectorExpression));
    }
}
