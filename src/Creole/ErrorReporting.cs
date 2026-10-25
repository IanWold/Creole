using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Combinators that shape what a failure reports, called as methods on a parser: <c>value.Named("value")</c>, <c>members.Context("in object")</c>.
/// </summary>
/// <remarks>
/// Neither changes what the parser matches.
/// Success, failure and how much input is consumed are the same as without them.
/// </remarks>
public static class ErrorReporting
{
    extension<TIn, TOut>(Parser<TIn, TOut> parser)
    {
        /// <summary>
        /// A parser that runs this parser and, while it runs, adds <paramref name="text"/> to the context of any failure recorded past the position where it started.
        /// </summary>
        /// <param name="text">The context line, such as <c>in object</c>.</param>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>A parser that matches the same input as this parser.</returns>
        /// <exception cref="GrammarException">This parser is null, or <paramref name="text"/> is null or empty.</exception>
        /// <remarks>
        /// Context lines appear in <see cref="ParseError.Context"/>, innermost first, and as <c>= note:</c> lines when rendered, such as <c>= note: in object starting at 2:1</c>.
        /// Each line's <see cref="ContextLine.Start"/> is the span of the first item this parser read.
        /// A failure at the position where this parser started gets no line, because the construct had not begun.
        /// </remarks>
        public Parser<TIn, TOut> Context(string text, [CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            new ContextNode<TIn, TOut>(Guard.NotNull(parser, parserExpression), Guard.NotNullOrEmpty(text));

        /// <summary>
        /// A parser that runs this parser and, when it consumes nothing, reports <paramref name="label"/> as what was expected in place of the items it tried.
        /// </summary>
        /// <param name="label">The name for what this parser accepts, such as <c>value</c>.</param>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>A parser that matches the same input as this parser.</returns>
        /// <exception cref="GrammarException">This parser is null, or <paramref name="label"/> is null or empty.</exception>
        /// <remarks>
        /// <para>
        /// The label applies whether this parser fails or succeeds, as long as it consumed nothing.
        /// It replaces the expectations this parser recorded at the position where it started, and adds none if this parser recorded no failure there.
        /// So <c>Digit.Many().Named("digits")</c> followed by <c>Char(';')</c> reports "expected digits or ';'" on input <c>x</c>.
        /// A failure that expected nothing in particular counts, so <c>Satisfy(char.IsAsciiHexDigit).Named("hex digit")</c> reports "expected hex digit".
        /// </para>
        /// <para>
        /// Expectations recorded at that position before this parser ran stay, ahead of the label, and a message, such as one from <c>Fail</c>, is never removed.
        /// If this parser consumes input, the label changes nothing: a failure after consuming keeps its precise expectations.
        /// Expectations at later positions, such as a deeper failure that an <c>Atomic</c> rewound past, are also kept.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Named(string label, [CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            new NamedNode<TIn, TOut>(Guard.NotNull(parser, parserExpression), new Label(Guard.NotNullOrEmpty(label)));
    }
}
