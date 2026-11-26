using System.Runtime.CompilerServices;

namespace Creole;

public abstract partial class Parser<TIn, TOut>
{
    /// <summary>
    /// A parser that runs this parser, then passes its result, the layer's state and the source span of the input it consumed to <paramref name="scan"/>, which gives the result to produce and the new state.
    /// </summary>
    /// <typeparam name="TState">The type of the layer's state.</typeparam>
    /// <typeparam name="TResult">The type of value <paramref name="scan"/> produces.</typeparam>
    /// <param name="scan">Gives the result and the new state from this parser's result, the state as this parser left it, and the span. It must be deterministic: an engine runs it on every success, even when the result is not used, and may run it more than once.</param>
    /// <param name="scanExpression">Filled in by the compiler with the expression passed as <paramref name="scan"/>.</param>
    /// <returns>A parser that matches the same input as this parser, and fails as it does.</returns>
    /// <exception cref="GrammarException">
    /// <paramref name="scan"/> is null.
    /// When it runs: the layer has no state, or its state is not a <typeparamref name="TState"/>, as for <see cref="Grammar{TIn}.GetState{TState}"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// It reads and replaces the state in one step, so a grammar can decide from what it has read so far, such as how deeply braces nest: <c>Char('{').Scan&lt;int, char&gt;((brace, depth, _) =&gt; (brace, depth + 1))</c>.
    /// The span is the one <c>Spanned</c> gives: zero-width where the next item starts if this parser consumes nothing.
    /// </para>
    /// <para>
    /// The new state lasts as one set by <see cref="Grammar{TIn}.SetState{TState}"/> does, and backtracking puts back the state from before it in the same way.
    /// In a layer given an <c>initialState</c>, each step must leave a state of that state's type.
    /// The state should be immutable, because backtracking restores it by keeping a reference to it.
    /// A value-type state, such as an <see cref="int"/>, is boxed each time <paramref name="scan"/> returns it.
    /// </para>
    /// </remarks>
    public Parser<TIn, TResult> Scan<TState, TResult>(
        Func<TOut, TState, SourceSpan, (TResult Result, TState State)> scan,
        [CallerArgumentExpression(nameof(scan))] string scanExpression = ""
    ) =>
        new ScanNode<TIn, TOut, TState, TResult>(this, Guard.NotNull(scan, scanExpression));
}
