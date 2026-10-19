using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// <c>Select</c> over a tuple-valued parser with one lambda parameter per element: <c>Seq(p, q, r).Select((a, b, c) =&gt; ...)</c>.
/// </summary>
/// <remarks>
/// <para>
/// These are extension members because the receiver must be a tuple-valued parser, which an instance method of <see cref="Parser{TIn, TOut}"/> cannot require.
/// The one-parameter <c>Select</c> is an instance method.
/// </para>
/// <para>
/// On a parser built by <c>Seq</c>, <c>Select</c> replaces the sequence's tuple with its own selector, so the result is still one analyzable sequence node.
/// </para>
/// </remarks>
public static class Sequencing
{
    extension<TIn, T1, T2>(Parser<TIn, (T1, T2)> parser)
    {
        /// <summary>
        /// A parser that combines this parser's two results with <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of value <paramref name="selector"/> produces. It is inferred; see the remarks.</typeparam>
        /// <param name="selector">Combines the results, one parameter per element of the tuple.</param>
        /// <param name="selectorExpression">Filled in by the compiler with the expression passed as <paramref name="selector"/>.</param>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>
        /// On a parser built by <c>Seq</c>, the same sequence with <paramref name="selector"/> in place of the tuple, as a single node.
        /// On any other parser, a map over it.
        /// </returns>
        /// <exception cref="GrammarException">This parser or <paramref name="selector"/> is null.</exception>
        /// <remarks>
        /// <typeparamref name="TResult"/> cannot be given explicitly, because <c>Select&lt;T&gt;</c> binds to the one-parameter <see cref="Parser{TIn, TOut}.Select{TResult}"/>.
        /// To produce a union or a base type, cast inside the lambda: <c>(a, b) =&gt; (Stmt)new Assign(a, b)</c>.
        /// </remarks>
        public Parser<TIn, TResult> Select<TResult>(
            Func<T1, T2, TResult> selector,
            [CallerArgumentExpression(nameof(selector))] string selectorExpression = "",
            [CallerArgumentExpression(nameof(parser))] string parserExpression = ""
        )
        {
            Guard.NotNull(parser, parserExpression);
            Guard.NotNull(selector, selectorExpression);

            return parser is SequenceNode<TIn, T1, T2, (T1, T2)> seq && ReferenceEquals(seq.Selector, SequenceNode<TIn, T1, T2, (T1, T2)>.TupleSelector)
                ? new SequenceNode<TIn, T1, T2, TResult>(seq.First, seq.Second, selector)
                : new MapNode<TIn, (T1, T2), TResult>(parser, tuple => selector(tuple.Item1, tuple.Item2));
        }
    }

    extension<TIn, T1, T2, T3>(Parser<TIn, (T1, T2, T3)> parser)
    {
        /// <summary>
        /// A parser that combines this parser's three results with <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of value <paramref name="selector"/> produces. It is inferred; see the remarks.</typeparam>
        /// <param name="selector">Combines the results, one parameter per element of the tuple.</param>
        /// <param name="selectorExpression">Filled in by the compiler with the expression passed as <paramref name="selector"/>.</param>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>
        /// On a parser built by <c>Seq</c>, the same sequence with <paramref name="selector"/> in place of the tuple, as a single node.
        /// On any other parser, a map over it.
        /// </returns>
        /// <exception cref="GrammarException">This parser or <paramref name="selector"/> is null.</exception>
        /// <remarks>
        /// <typeparamref name="TResult"/> cannot be given explicitly, because <c>Select&lt;T&gt;</c> binds to the one-parameter <see cref="Parser{TIn, TOut}.Select{TResult}"/>.
        /// To produce a union or a base type, cast inside the lambda: <c>(a, b) =&gt; (Stmt)new Assign(a, b)</c>.
        /// </remarks>
        public Parser<TIn, TResult> Select<TResult>(
            Func<T1, T2, T3, TResult> selector,
            [CallerArgumentExpression(nameof(selector))] string selectorExpression = "",
            [CallerArgumentExpression(nameof(parser))] string parserExpression = ""
        )
        {
            Guard.NotNull(parser, parserExpression);
            Guard.NotNull(selector, selectorExpression);

            return parser is SequenceNode<TIn, T1, T2, T3, (T1, T2, T3)> seq && ReferenceEquals(seq.Selector, SequenceNode<TIn, T1, T2, T3, (T1, T2, T3)>.TupleSelector)
                ? new SequenceNode<TIn, T1, T2, T3, TResult>(seq.First, seq.Second, seq.Third, selector)
                : new MapNode<TIn, (T1, T2, T3), TResult>(parser, tuple => selector(tuple.Item1, tuple.Item2, tuple.Item3));
        }
    }

    extension<TIn, T1, T2, T3, T4>(Parser<TIn, (T1, T2, T3, T4)> parser)
    {
        /// <summary>
        /// A parser that combines this parser's four results with <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of value <paramref name="selector"/> produces. It is inferred; see the remarks.</typeparam>
        /// <param name="selector">Combines the results, one parameter per element of the tuple.</param>
        /// <param name="selectorExpression">Filled in by the compiler with the expression passed as <paramref name="selector"/>.</param>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>
        /// On a parser built by <c>Seq</c>, the same sequence with <paramref name="selector"/> in place of the tuple, as a single node.
        /// On any other parser, a map over it.
        /// </returns>
        /// <exception cref="GrammarException">This parser or <paramref name="selector"/> is null.</exception>
        /// <remarks>
        /// <typeparamref name="TResult"/> cannot be given explicitly, because <c>Select&lt;T&gt;</c> binds to the one-parameter <see cref="Parser{TIn, TOut}.Select{TResult}"/>.
        /// To produce a union or a base type, cast inside the lambda: <c>(a, b) =&gt; (Stmt)new Assign(a, b)</c>.
        /// </remarks>
        public Parser<TIn, TResult> Select<TResult>(
            Func<T1, T2, T3, T4, TResult> selector,
            [CallerArgumentExpression(nameof(selector))] string selectorExpression = "",
            [CallerArgumentExpression(nameof(parser))] string parserExpression = ""
        )
        {
            Guard.NotNull(parser, parserExpression);
            Guard.NotNull(selector, selectorExpression);

            return parser is SequenceNode<TIn, T1, T2, T3, T4, (T1, T2, T3, T4)> seq && ReferenceEquals(seq.Selector, SequenceNode<TIn, T1, T2, T3, T4, (T1, T2, T3, T4)>.TupleSelector)
                ? new SequenceNode<TIn, T1, T2, T3, T4, TResult>(seq.First, seq.Second, seq.Third, seq.Fourth, selector)
                : new MapNode<TIn, (T1, T2, T3, T4), TResult>(parser, tuple => selector(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4));
        }
    }

    extension<TIn, T1, T2, T3, T4, T5>(Parser<TIn, (T1, T2, T3, T4, T5)> parser)
    {
        /// <summary>
        /// A parser that combines this parser's five results with <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of value <paramref name="selector"/> produces. It is inferred; see the remarks.</typeparam>
        /// <param name="selector">Combines the results, one parameter per element of the tuple.</param>
        /// <param name="selectorExpression">Filled in by the compiler with the expression passed as <paramref name="selector"/>.</param>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>
        /// On a parser built by <c>Seq</c>, the same sequence with <paramref name="selector"/> in place of the tuple, as a single node.
        /// On any other parser, a map over it.
        /// </returns>
        /// <exception cref="GrammarException">This parser or <paramref name="selector"/> is null.</exception>
        /// <remarks>
        /// <typeparamref name="TResult"/> cannot be given explicitly, because <c>Select&lt;T&gt;</c> binds to the one-parameter <see cref="Parser{TIn, TOut}.Select{TResult}"/>.
        /// To produce a union or a base type, cast inside the lambda: <c>(a, b) =&gt; (Stmt)new Assign(a, b)</c>.
        /// </remarks>
        public Parser<TIn, TResult> Select<TResult>(
            Func<T1, T2, T3, T4, T5, TResult> selector,
            [CallerArgumentExpression(nameof(selector))] string selectorExpression = "",
            [CallerArgumentExpression(nameof(parser))] string parserExpression = ""
        )
        {
            Guard.NotNull(parser, parserExpression);
            Guard.NotNull(selector, selectorExpression);

            return parser is SequenceNode<TIn, T1, T2, T3, T4, T5, (T1, T2, T3, T4, T5)> seq && ReferenceEquals(seq.Selector, SequenceNode<TIn, T1, T2, T3, T4, T5, (T1, T2, T3, T4, T5)>.TupleSelector)
                ? new SequenceNode<TIn, T1, T2, T3, T4, T5, TResult>(seq.First, seq.Second, seq.Third, seq.Fourth, seq.Fifth, selector)
                : new MapNode<TIn, (T1, T2, T3, T4, T5), TResult>(parser, tuple => selector(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5));
        }
    }

    extension<TIn, T1, T2, T3, T4, T5, T6>(Parser<TIn, (T1, T2, T3, T4, T5, T6)> parser)
    {
        /// <summary>
        /// A parser that combines this parser's six results with <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of value <paramref name="selector"/> produces. It is inferred; see the remarks.</typeparam>
        /// <param name="selector">Combines the results, one parameter per element of the tuple.</param>
        /// <param name="selectorExpression">Filled in by the compiler with the expression passed as <paramref name="selector"/>.</param>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>
        /// On a parser built by <c>Seq</c>, the same sequence with <paramref name="selector"/> in place of the tuple, as a single node.
        /// On any other parser, a map over it.
        /// </returns>
        /// <exception cref="GrammarException">This parser or <paramref name="selector"/> is null.</exception>
        /// <remarks>
        /// <typeparamref name="TResult"/> cannot be given explicitly, because <c>Select&lt;T&gt;</c> binds to the one-parameter <see cref="Parser{TIn, TOut}.Select{TResult}"/>.
        /// To produce a union or a base type, cast inside the lambda: <c>(a, b) =&gt; (Stmt)new Assign(a, b)</c>.
        /// </remarks>
        public Parser<TIn, TResult> Select<TResult>(
            Func<T1, T2, T3, T4, T5, T6, TResult> selector,
            [CallerArgumentExpression(nameof(selector))] string selectorExpression = "",
            [CallerArgumentExpression(nameof(parser))] string parserExpression = ""
        )
        {
            Guard.NotNull(parser, parserExpression);
            Guard.NotNull(selector, selectorExpression);

            return parser is SequenceNode<TIn, T1, T2, T3, T4, T5, T6, (T1, T2, T3, T4, T5, T6)> seq && ReferenceEquals(seq.Selector, SequenceNode<TIn, T1, T2, T3, T4, T5, T6, (T1, T2, T3, T4, T5, T6)>.TupleSelector)
                ? new SequenceNode<TIn, T1, T2, T3, T4, T5, T6, TResult>(seq.First, seq.Second, seq.Third, seq.Fourth, seq.Fifth, seq.Sixth, selector)
                : new MapNode<TIn, (T1, T2, T3, T4, T5, T6), TResult>(parser, tuple => selector(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6));
        }
    }

    extension<TIn, T1, T2, T3, T4, T5, T6, T7>(Parser<TIn, (T1, T2, T3, T4, T5, T6, T7)> parser)
    {
        /// <summary>
        /// A parser that combines this parser's seven results with <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of value <paramref name="selector"/> produces. It is inferred; see the remarks.</typeparam>
        /// <param name="selector">Combines the results, one parameter per element of the tuple.</param>
        /// <param name="selectorExpression">Filled in by the compiler with the expression passed as <paramref name="selector"/>.</param>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>
        /// On a parser built by <c>Seq</c>, the same sequence with <paramref name="selector"/> in place of the tuple, as a single node.
        /// On any other parser, a map over it.
        /// </returns>
        /// <exception cref="GrammarException">This parser or <paramref name="selector"/> is null.</exception>
        /// <remarks>
        /// <typeparamref name="TResult"/> cannot be given explicitly, because <c>Select&lt;T&gt;</c> binds to the one-parameter <see cref="Parser{TIn, TOut}.Select{TResult}"/>.
        /// To produce a union or a base type, cast inside the lambda: <c>(a, b) =&gt; (Stmt)new Assign(a, b)</c>.
        /// </remarks>
        public Parser<TIn, TResult> Select<TResult>(
            Func<T1, T2, T3, T4, T5, T6, T7, TResult> selector,
            [CallerArgumentExpression(nameof(selector))] string selectorExpression = "",
            [CallerArgumentExpression(nameof(parser))] string parserExpression = ""
        )
        {
            Guard.NotNull(parser, parserExpression);
            Guard.NotNull(selector, selectorExpression);

            return parser is SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, (T1, T2, T3, T4, T5, T6, T7)> seq && ReferenceEquals(seq.Selector, SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, (T1, T2, T3, T4, T5, T6, T7)>.TupleSelector)
                ? new SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, TResult>(seq.First, seq.Second, seq.Third, seq.Fourth, seq.Fifth, seq.Sixth, seq.Seventh, selector)
                : new MapNode<TIn, (T1, T2, T3, T4, T5, T6, T7), TResult>(parser, tuple => selector(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7));
        }
    }

    extension<TIn, T1, T2, T3, T4, T5, T6, T7, T8>(Parser<TIn, (T1, T2, T3, T4, T5, T6, T7, T8)> parser)
    {
        /// <summary>
        /// A parser that combines this parser's eight results with <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of value <paramref name="selector"/> produces. It is inferred; see the remarks.</typeparam>
        /// <param name="selector">Combines the results, one parameter per element of the tuple.</param>
        /// <param name="selectorExpression">Filled in by the compiler with the expression passed as <paramref name="selector"/>.</param>
        /// <param name="parserExpression">Filled in by the compiler with the expression this method is called on.</param>
        /// <returns>
        /// On a parser built by <c>Seq</c>, the same sequence with <paramref name="selector"/> in place of the tuple, as a single node.
        /// On any other parser, a map over it.
        /// </returns>
        /// <exception cref="GrammarException">This parser or <paramref name="selector"/> is null.</exception>
        /// <remarks>
        /// <typeparamref name="TResult"/> cannot be given explicitly, because <c>Select&lt;T&gt;</c> binds to the one-parameter <see cref="Parser{TIn, TOut}.Select{TResult}"/>.
        /// To produce a union or a base type, cast inside the lambda: <c>(a, b) =&gt; (Stmt)new Assign(a, b)</c>.
        /// </remarks>
        public Parser<TIn, TResult> Select<TResult>(
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> selector,
            [CallerArgumentExpression(nameof(selector))] string selectorExpression = "",
            [CallerArgumentExpression(nameof(parser))] string parserExpression = ""
        )
        {
            Guard.NotNull(parser, parserExpression);
            Guard.NotNull(selector, selectorExpression);

            return parser is SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, T8, (T1, T2, T3, T4, T5, T6, T7, T8)> seq && ReferenceEquals(seq.Selector, SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, T8, (T1, T2, T3, T4, T5, T6, T7, T8)>.TupleSelector)
                ? new SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, T8, TResult>(seq.First, seq.Second, seq.Third, seq.Fourth, seq.Fifth, seq.Sixth, seq.Seventh, seq.Eighth, selector)
                : new MapNode<TIn, (T1, T2, T3, T4, T5, T6, T7, T8), TResult>(parser, tuple => selector(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7, tuple.Item8));
        }
    }
}
