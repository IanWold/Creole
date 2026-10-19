using System.Runtime.CompilerServices;

namespace Creole;

public abstract partial class Parser<TIn, TOut>
{
    /// <summary>
    /// A parser that runs this parser, then <paramref name="next"/>, and produces this parser's result.
    /// </summary>
    /// <typeparam name="TNext">The type of value <paramref name="next"/> produces. It is discarded.</typeparam>
    /// <param name="next">The parser to run after this one, such as trailing whitespace or <c>End</c>.</param>
    /// <param name="nextExpression">Filled in by the compiler with the expression passed as <paramref name="next"/>.</param>
    /// <returns>A single sequence node: this parser, then <paramref name="next"/>.</returns>
    /// <exception cref="GrammarException"><paramref name="next"/> is null.</exception>
    public Parser<TIn, TOut> Before<TNext>(Parser<TIn, TNext> next, [CallerArgumentExpression(nameof(next))] string nextExpression = "") =>
        new SequenceNode<TIn, TOut, TNext, TOut>(this, Guard.NotNull(next, nextExpression), static (value, _) => value);

    /// <summary>
    /// A parser that runs <paramref name="open"/>, this parser, then <paramref name="close"/>, and produces this parser's result.
    /// </summary>
    /// <typeparam name="TOpen">The type of value <paramref name="open"/> produces. It is discarded.</typeparam>
    /// <typeparam name="TClose">The type of value <paramref name="close"/> produces. It is discarded.</typeparam>
    /// <param name="open">The parser to run first, such as an opening bracket.</param>
    /// <param name="close">The parser to run last, such as a closing bracket.</param>
    /// <param name="openExpression">Filled in by the compiler with the expression passed as <paramref name="open"/>.</param>
    /// <param name="closeExpression">Filled in by the compiler with the expression passed as <paramref name="close"/>.</param>
    /// <returns>A single sequence node: <paramref name="open"/>, this parser, then <paramref name="close"/>.</returns>
    /// <exception cref="GrammarException"><paramref name="open"/> or <paramref name="close"/> is null.</exception>
    public Parser<TIn, TOut> Between<TOpen, TClose>(
        Parser<TIn, TOpen> open,
        Parser<TIn, TClose> close,
        [CallerArgumentExpression(nameof(open))] string openExpression = "",
        [CallerArgumentExpression(nameof(close))] string closeExpression = ""
    ) =>
        new SequenceNode<TIn, TOpen, TOut, TClose, TOut>(
            Guard.NotNull(open, openExpression),
            this,
            Guard.NotNull(close, closeExpression),
            static (_, value, _) => value
        );

    /// <summary>
    /// A parser that runs this parser, then <paramref name="next"/>, and produces the result of <paramref name="next"/>.
    /// </summary>
    /// <typeparam name="TNext">The type of value <paramref name="next"/> produces.</typeparam>
    /// <param name="next">The parser to run after this one.</param>
    /// <param name="nextExpression">Filled in by the compiler with the expression passed as <paramref name="next"/>.</param>
    /// <returns>A single sequence node: this parser, then <paramref name="next"/>.</returns>
    /// <exception cref="GrammarException"><paramref name="next"/> is null.</exception>
    /// <remarks>
    /// This parser's result is discarded.
    /// </remarks>
    public Parser<TIn, TNext> Then<TNext>(Parser<TIn, TNext> next, [CallerArgumentExpression(nameof(next))] string nextExpression = "") =>
        new SequenceNode<TIn, TOut, TNext, TNext>(this, Guard.NotNull(next, nextExpression), static (_, value) => value);
}
