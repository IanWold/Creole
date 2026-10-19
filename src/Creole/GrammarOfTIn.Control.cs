using System.Runtime.CompilerServices;

namespace Creole;

public abstract partial class Grammar<TIn>
{
    /// <summary>
    /// A parser that succeeds without consuming input and, while it stays in force, stops the <see cref="Atomic"/>s around it from rewinding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Put it where the grammar knows which construct it is reading, such as after <c>if</c> and <c>(</c>: <c>Seq(If, LParen, Cut, Condition, RParen)</c>.
    /// A later failure is then reported there instead of being retried elsewhere, and engines can release input they no longer need to rewind to.
    /// </para>
    /// <para>
    /// The cut stops being in force when the parse leaves the nearest enclosing <see cref="Lookahead"/>, <see cref="Not"/> or <c>Longest</c> alternative.
    /// It also stops when the attempt that passed it fails without consuming.
    /// An attempt is each alternative of <c>Or</c> or <c>OneOf</c>, including the last; the item of <c>Optional</c>; each item of a repetition, mandatory or not; each separator and each terminator; and each operator and atom of <c>Expression.Build</c>.
    /// The cut is dropped whether the combinator then moves on, stops or fails.
    /// Otherwise it lasts until the parse ends, and nesting a sequence inside another does not change that.
    /// A cut never turns a failure that consumed nothing into a committed one, so <c>Or</c> and repetition behave as they would without it, and engines may dispatch on the next item instead of trying attempts in order.
    /// </para>
    /// <para>
    /// Every use refers to the same parser.
    /// </para>
    /// </remarks>
    public static Parser<TIn, Unit> Cut { get; } = new CutNode<TIn>();

    /// <summary>
    /// A parser that runs <paramref name="parser"/> and, if it fails after consuming input, rewinds so the failure counts as having consumed nothing.
    /// </summary>
    /// <typeparam name="TOut">The type of value <paramref name="parser"/> produces.</typeparam>
    /// <param name="parser">The parser to run.</param>
    /// <param name="parserExpression">Filled in by the compiler with the expression passed as <paramref name="parser"/>.</param>
    /// <returns>A parser that matches the same input as <paramref name="parser"/>.</returns>
    /// <exception cref="GrammarException"><paramref name="parser"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Use it to let an enclosing choice try its next alternative after this one has read part of the input: <c>Atomic(Seq(Ident, Colon)).Or(Seq(Ident, Equals))</c>.
    /// It is Parsec's <c>try</c>.
    /// </para>
    /// <para>
    /// If a <see cref="Cut"/> succeeded inside <paramref name="parser"/> and is still in force, the failure is not rewound and stays committed.
    /// A cut is still in force at any depth, unless it was inside a <see cref="Lookahead"/>, <see cref="Not"/> or <c>Longest</c> alternative, or was dropped with an attempt that failed without consuming (see <see cref="Cut"/>).
    /// A cut passed before this parser started has no effect on it.
    /// Success, and failure without consuming, pass through unchanged.
    /// </para>
    /// </remarks>
    public static Parser<TIn, TOut> Atomic<TOut>(Parser<TIn, TOut> parser, [CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
        new AtomicNode<TIn, TOut>(Guard.NotNull(parser, parserExpression));

    /// <summary>
    /// A parser that runs <paramref name="parser"/> and, if it succeeds, rewinds and produces its value without consuming input.
    /// </summary>
    /// <typeparam name="TOut">The type of value <paramref name="parser"/> produces.</typeparam>
    /// <param name="parser">The parser to run.</param>
    /// <param name="parserExpression">Filled in by the compiler with the expression passed as <paramref name="parser"/>.</param>
    /// <returns>A parser that never consumes input when it succeeds.</returns>
    /// <exception cref="GrammarException"><paramref name="parser"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// If <paramref name="parser"/> fails after consuming input, the failure stays committed, as in Parsec. Wrap it in <see cref="Atomic"/> to make that failure empty.
    /// </para>
    /// <para>
    /// A <see cref="Cut"/> inside <paramref name="parser"/> stays inside it: it stops <see cref="Atomic"/>s within <paramref name="parser"/> from rewinding, but not one around this parser.
    /// </para>
    /// </remarks>
    public static Parser<TIn, TOut> Lookahead<TOut>(Parser<TIn, TOut> parser, [CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
        new LookaheadNode<TIn, TOut>(Guard.NotNull(parser, parserExpression));

    /// <summary>
    /// A parser that succeeds without consuming input if <paramref name="parser"/> fails, and fails without consuming input if <paramref name="parser"/> succeeds.
    /// </summary>
    /// <typeparam name="TOut">The type of value <paramref name="parser"/> produces. Its value is discarded.</typeparam>
    /// <param name="parser">The parser that must not match here.</param>
    /// <param name="parserExpression">Filled in by the compiler with the expression passed as <paramref name="parser"/>.</param>
    /// <returns>A parser that never consumes input.</returns>
    /// <exception cref="GrammarException"><paramref name="parser"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// It rewinds in every case, so it succeeds even when <paramref name="parser"/> fails after consuming input.
    /// Use it to stop a keyword matching the start of an identifier: <c>String("if").Before(Not(LetterOrDigit))</c>.
    /// </para>
    /// <para>
    /// A <see cref="Cut"/> inside <paramref name="parser"/> stays inside it, as for <see cref="Lookahead"/>.
    /// </para>
    /// </remarks>
    public static Parser<TIn, Unit> Not<TOut>(Parser<TIn, TOut> parser, [CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
        new NotNode<TIn, TOut>(Guard.NotNull(parser, parserExpression));
}
