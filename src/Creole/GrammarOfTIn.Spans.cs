using System.Runtime.CompilerServices;

namespace Creole;

public abstract partial class Grammar<TIn>
{
    /// <summary>
    /// A parser that succeeds without consuming input and produces a zero-width <see cref="SourceSpan"/> at the current position.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The span is where the next item starts, so over a layer's items it lies after any input the layer skipped, such as whitespace.
    /// At the end of the input it is at the end of the last item, and in an empty input at offset 0.
    /// Over input held whole in memory, offsets are item positions.
    /// </para>
    /// <para>
    /// It reads the next item to find where it starts, as a lookahead would.
    /// </para>
    /// <para>
    /// Every use refers to the same parser.
    /// </para>
    /// </remarks>
    public static Parser<TIn, SourceSpan> Position { get; } = new PositionNode<TIn>();

    /// <summary>
    /// A parser that runs two parsers in order, as <c>Seq</c> does, and succeeds only if the second starts exactly where the first ended, with no skipped input between them.
    /// </summary>
    /// <typeparam name="T1">The type of value the first parser produces.</typeparam>
    /// <typeparam name="T2">The type of value the second parser produces.</typeparam>
    /// <param name="first">The first parser to run.</param>
    /// <param name="second">The second parser to run.</param>
    /// <param name="firstExpression">Filled in by the compiler with the expression passed as <paramref name="first"/>.</param>
    /// <param name="secondExpression">Filled in by the compiler with the expression passed as <paramref name="second"/>.</param>
    /// <returns>A parser that produces both results as a tuple.</returns>
    /// <exception cref="GrammarException">A parser argument is null.</exception>
    /// <remarks>
    /// <para>
    /// It joins tokens that a lexer split: over single <c>&gt;</c> tokens, <c>Atomic(Adjacent(Kind(Gt), Kind(Gt)))</c> reads a shift in <c>a &gt;&gt; b</c>, while <c>List&lt;List&lt;int&gt;&gt;</c> still closes two type arguments.
    /// The two touch when the first's span ends where the second's starts, in the same source.
    /// A first parser that consumes nothing touches whatever follows it.
    /// </para>
    /// <para>
    /// It commits as <c>Seq</c> does. If either parser fails, it fails the same way.
    /// If the two do not touch, it fails where the first started, over the input both consumed, and the failure is committed if either consumed input.
    /// Wrap it in <see cref="Atomic"/> where the grammar should then try something shorter.
    /// </para>
    /// <para>
    /// The message names both parsers, as in <c>'&gt;' must come right after '&gt;', with nothing between them</c>: each by its label, or by what its leading primitive expects.
    /// If either has no name, it is <c>these items must touch, with nothing between them</c>.
    /// </para>
    /// <para>
    /// Over input held whole in memory, offsets are item positions, so consecutive items always touch.
    /// </para>
    /// </remarks>
    public static Parser<TIn, (T1, T2)> Adjacent<T1, T2>(
        Parser<TIn, T1> first,
        Parser<TIn, T2> second,
        [CallerArgumentExpression(nameof(first))] string firstExpression = "",
        [CallerArgumentExpression(nameof(second))] string secondExpression = ""
    ) =>
        new AdjacentNode<TIn, T1, T2>(Guard.NotNull(first, firstExpression), Guard.NotNull(second, secondExpression));
}
