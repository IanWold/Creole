using System.Runtime.CompilerServices;

namespace Creole;

public abstract partial class Grammar<TIn>
{
    /// <summary>
    /// A parser that runs two parsers in order and produces their results as a tuple.
    /// </summary>
    /// <typeparam name="T1">The type of value the first parser produces.</typeparam>
    /// <typeparam name="T2">The type of value the second parser produces.</typeparam>
    /// <param name="first">The first parser to run.</param>
    /// <param name="second">The second parser to run.</param>
    /// <param name="firstExpression">Filled in by the compiler with the expression passed as <paramref name="first"/>.</param>
    /// <param name="secondExpression">Filled in by the compiler with the expression passed as <paramref name="second"/>.</param>
    /// <returns>A single sequence node. Follow it with <c>Select</c> to combine the results; the combined parser is still one node.</returns>
    /// <exception cref="GrammarException">A parser argument is null.</exception>
    /// <remarks>
    /// The sequence fails if any parser fails.
    /// Once a parser has consumed input, a later failure is committed, so an enclosing choice does not try its next alternative.
    /// </remarks>
    public static Parser<TIn, (T1, T2)> Seq<T1, T2>(
        Parser<TIn, T1> first,
        Parser<TIn, T2> second,
        [CallerArgumentExpression(nameof(first))] string firstExpression = "",
        [CallerArgumentExpression(nameof(second))] string secondExpression = ""
    ) =>
        new SequenceNode<TIn, T1, T2, (T1, T2)>(
            Guard.NotNull(first, firstExpression),
            Guard.NotNull(second, secondExpression),
            SequenceNode<TIn, T1, T2, (T1, T2)>.TupleSelector
        );

    /// <summary>
    /// A parser that runs three parsers in order and produces their results as a tuple.
    /// </summary>
    /// <typeparam name="T1">The type of value the first parser produces.</typeparam>
    /// <typeparam name="T2">The type of value the second parser produces.</typeparam>
    /// <typeparam name="T3">The type of value the third parser produces.</typeparam>
    /// <param name="first">The first parser to run.</param>
    /// <param name="second">The second parser to run.</param>
    /// <param name="third">The third parser to run.</param>
    /// <param name="firstExpression">Filled in by the compiler with the expression passed as <paramref name="first"/>.</param>
    /// <param name="secondExpression">Filled in by the compiler with the expression passed as <paramref name="second"/>.</param>
    /// <param name="thirdExpression">Filled in by the compiler with the expression passed as <paramref name="third"/>.</param>
    /// <returns>A single sequence node. Follow it with <c>Select</c> to combine the results; the combined parser is still one node.</returns>
    /// <exception cref="GrammarException">A parser argument is null.</exception>
    /// <remarks>
    /// The sequence fails if any parser fails.
    /// Once a parser has consumed input, a later failure is committed, so an enclosing choice does not try its next alternative.
    /// </remarks>
    public static Parser<TIn, (T1, T2, T3)> Seq<T1, T2, T3>(
        Parser<TIn, T1> first,
        Parser<TIn, T2> second,
        Parser<TIn, T3> third,
        [CallerArgumentExpression(nameof(first))] string firstExpression = "",
        [CallerArgumentExpression(nameof(second))] string secondExpression = "",
        [CallerArgumentExpression(nameof(third))] string thirdExpression = ""
    ) =>
        new SequenceNode<TIn, T1, T2, T3, (T1, T2, T3)>(
            Guard.NotNull(first, firstExpression),
            Guard.NotNull(second, secondExpression),
            Guard.NotNull(third, thirdExpression),
            SequenceNode<TIn, T1, T2, T3, (T1, T2, T3)>.TupleSelector
        );

    /// <summary>
    /// A parser that runs four parsers in order and produces their results as a tuple.
    /// </summary>
    /// <typeparam name="T1">The type of value the first parser produces.</typeparam>
    /// <typeparam name="T2">The type of value the second parser produces.</typeparam>
    /// <typeparam name="T3">The type of value the third parser produces.</typeparam>
    /// <typeparam name="T4">The type of value the fourth parser produces.</typeparam>
    /// <param name="first">The first parser to run.</param>
    /// <param name="second">The second parser to run.</param>
    /// <param name="third">The third parser to run.</param>
    /// <param name="fourth">The fourth parser to run.</param>
    /// <param name="firstExpression">Filled in by the compiler with the expression passed as <paramref name="first"/>.</param>
    /// <param name="secondExpression">Filled in by the compiler with the expression passed as <paramref name="second"/>.</param>
    /// <param name="thirdExpression">Filled in by the compiler with the expression passed as <paramref name="third"/>.</param>
    /// <param name="fourthExpression">Filled in by the compiler with the expression passed as <paramref name="fourth"/>.</param>
    /// <returns>A single sequence node. Follow it with <c>Select</c> to combine the results; the combined parser is still one node.</returns>
    /// <exception cref="GrammarException">A parser argument is null.</exception>
    /// <remarks>
    /// The sequence fails if any parser fails.
    /// Once a parser has consumed input, a later failure is committed, so an enclosing choice does not try its next alternative.
    /// </remarks>
    public static Parser<TIn, (T1, T2, T3, T4)> Seq<T1, T2, T3, T4>(
        Parser<TIn, T1> first,
        Parser<TIn, T2> second,
        Parser<TIn, T3> third,
        Parser<TIn, T4> fourth,
        [CallerArgumentExpression(nameof(first))] string firstExpression = "",
        [CallerArgumentExpression(nameof(second))] string secondExpression = "",
        [CallerArgumentExpression(nameof(third))] string thirdExpression = "",
        [CallerArgumentExpression(nameof(fourth))] string fourthExpression = ""
    ) =>
        new SequenceNode<TIn, T1, T2, T3, T4, (T1, T2, T3, T4)>(
            Guard.NotNull(first, firstExpression),
            Guard.NotNull(second, secondExpression),
            Guard.NotNull(third, thirdExpression),
            Guard.NotNull(fourth, fourthExpression),
            SequenceNode<TIn, T1, T2, T3, T4, (T1, T2, T3, T4)>.TupleSelector
        );

    /// <summary>
    /// A parser that runs five parsers in order and produces their results as a tuple.
    /// </summary>
    /// <typeparam name="T1">The type of value the first parser produces.</typeparam>
    /// <typeparam name="T2">The type of value the second parser produces.</typeparam>
    /// <typeparam name="T3">The type of value the third parser produces.</typeparam>
    /// <typeparam name="T4">The type of value the fourth parser produces.</typeparam>
    /// <typeparam name="T5">The type of value the fifth parser produces.</typeparam>
    /// <param name="first">The first parser to run.</param>
    /// <param name="second">The second parser to run.</param>
    /// <param name="third">The third parser to run.</param>
    /// <param name="fourth">The fourth parser to run.</param>
    /// <param name="fifth">The fifth parser to run.</param>
    /// <param name="firstExpression">Filled in by the compiler with the expression passed as <paramref name="first"/>.</param>
    /// <param name="secondExpression">Filled in by the compiler with the expression passed as <paramref name="second"/>.</param>
    /// <param name="thirdExpression">Filled in by the compiler with the expression passed as <paramref name="third"/>.</param>
    /// <param name="fourthExpression">Filled in by the compiler with the expression passed as <paramref name="fourth"/>.</param>
    /// <param name="fifthExpression">Filled in by the compiler with the expression passed as <paramref name="fifth"/>.</param>
    /// <returns>A single sequence node. Follow it with <c>Select</c> to combine the results; the combined parser is still one node.</returns>
    /// <exception cref="GrammarException">A parser argument is null.</exception>
    /// <remarks>
    /// The sequence fails if any parser fails.
    /// Once a parser has consumed input, a later failure is committed, so an enclosing choice does not try its next alternative.
    /// </remarks>
    public static Parser<TIn, (T1, T2, T3, T4, T5)> Seq<T1, T2, T3, T4, T5>(
        Parser<TIn, T1> first,
        Parser<TIn, T2> second,
        Parser<TIn, T3> third,
        Parser<TIn, T4> fourth,
        Parser<TIn, T5> fifth,
        [CallerArgumentExpression(nameof(first))] string firstExpression = "",
        [CallerArgumentExpression(nameof(second))] string secondExpression = "",
        [CallerArgumentExpression(nameof(third))] string thirdExpression = "",
        [CallerArgumentExpression(nameof(fourth))] string fourthExpression = "",
        [CallerArgumentExpression(nameof(fifth))] string fifthExpression = ""
    ) =>
        new SequenceNode<TIn, T1, T2, T3, T4, T5, (T1, T2, T3, T4, T5)>(
            Guard.NotNull(first, firstExpression),
            Guard.NotNull(second, secondExpression),
            Guard.NotNull(third, thirdExpression),
            Guard.NotNull(fourth, fourthExpression),
            Guard.NotNull(fifth, fifthExpression),
            SequenceNode<TIn, T1, T2, T3, T4, T5, (T1, T2, T3, T4, T5)>.TupleSelector
        );

    /// <summary>
    /// A parser that runs six parsers in order and produces their results as a tuple.
    /// </summary>
    /// <typeparam name="T1">The type of value the first parser produces.</typeparam>
    /// <typeparam name="T2">The type of value the second parser produces.</typeparam>
    /// <typeparam name="T3">The type of value the third parser produces.</typeparam>
    /// <typeparam name="T4">The type of value the fourth parser produces.</typeparam>
    /// <typeparam name="T5">The type of value the fifth parser produces.</typeparam>
    /// <typeparam name="T6">The type of value the sixth parser produces.</typeparam>
    /// <param name="first">The first parser to run.</param>
    /// <param name="second">The second parser to run.</param>
    /// <param name="third">The third parser to run.</param>
    /// <param name="fourth">The fourth parser to run.</param>
    /// <param name="fifth">The fifth parser to run.</param>
    /// <param name="sixth">The sixth parser to run.</param>
    /// <param name="firstExpression">Filled in by the compiler with the expression passed as <paramref name="first"/>.</param>
    /// <param name="secondExpression">Filled in by the compiler with the expression passed as <paramref name="second"/>.</param>
    /// <param name="thirdExpression">Filled in by the compiler with the expression passed as <paramref name="third"/>.</param>
    /// <param name="fourthExpression">Filled in by the compiler with the expression passed as <paramref name="fourth"/>.</param>
    /// <param name="fifthExpression">Filled in by the compiler with the expression passed as <paramref name="fifth"/>.</param>
    /// <param name="sixthExpression">Filled in by the compiler with the expression passed as <paramref name="sixth"/>.</param>
    /// <returns>A single sequence node. Follow it with <c>Select</c> to combine the results; the combined parser is still one node.</returns>
    /// <exception cref="GrammarException">A parser argument is null.</exception>
    /// <remarks>
    /// The sequence fails if any parser fails.
    /// Once a parser has consumed input, a later failure is committed, so an enclosing choice does not try its next alternative.
    /// </remarks>
    public static Parser<TIn, (T1, T2, T3, T4, T5, T6)> Seq<T1, T2, T3, T4, T5, T6>(
        Parser<TIn, T1> first,
        Parser<TIn, T2> second,
        Parser<TIn, T3> third,
        Parser<TIn, T4> fourth,
        Parser<TIn, T5> fifth,
        Parser<TIn, T6> sixth,
        [CallerArgumentExpression(nameof(first))] string firstExpression = "",
        [CallerArgumentExpression(nameof(second))] string secondExpression = "",
        [CallerArgumentExpression(nameof(third))] string thirdExpression = "",
        [CallerArgumentExpression(nameof(fourth))] string fourthExpression = "",
        [CallerArgumentExpression(nameof(fifth))] string fifthExpression = "",
        [CallerArgumentExpression(nameof(sixth))] string sixthExpression = ""
    ) =>
        new SequenceNode<TIn, T1, T2, T3, T4, T5, T6, (T1, T2, T3, T4, T5, T6)>(
            Guard.NotNull(first, firstExpression),
            Guard.NotNull(second, secondExpression),
            Guard.NotNull(third, thirdExpression),
            Guard.NotNull(fourth, fourthExpression),
            Guard.NotNull(fifth, fifthExpression),
            Guard.NotNull(sixth, sixthExpression),
            SequenceNode<TIn, T1, T2, T3, T4, T5, T6, (T1, T2, T3, T4, T5, T6)>.TupleSelector
        );

    /// <summary>
    /// A parser that runs seven parsers in order and produces their results as a tuple.
    /// </summary>
    /// <typeparam name="T1">The type of value the first parser produces.</typeparam>
    /// <typeparam name="T2">The type of value the second parser produces.</typeparam>
    /// <typeparam name="T3">The type of value the third parser produces.</typeparam>
    /// <typeparam name="T4">The type of value the fourth parser produces.</typeparam>
    /// <typeparam name="T5">The type of value the fifth parser produces.</typeparam>
    /// <typeparam name="T6">The type of value the sixth parser produces.</typeparam>
    /// <typeparam name="T7">The type of value the seventh parser produces.</typeparam>
    /// <param name="first">The first parser to run.</param>
    /// <param name="second">The second parser to run.</param>
    /// <param name="third">The third parser to run.</param>
    /// <param name="fourth">The fourth parser to run.</param>
    /// <param name="fifth">The fifth parser to run.</param>
    /// <param name="sixth">The sixth parser to run.</param>
    /// <param name="seventh">The seventh parser to run.</param>
    /// <param name="firstExpression">Filled in by the compiler with the expression passed as <paramref name="first"/>.</param>
    /// <param name="secondExpression">Filled in by the compiler with the expression passed as <paramref name="second"/>.</param>
    /// <param name="thirdExpression">Filled in by the compiler with the expression passed as <paramref name="third"/>.</param>
    /// <param name="fourthExpression">Filled in by the compiler with the expression passed as <paramref name="fourth"/>.</param>
    /// <param name="fifthExpression">Filled in by the compiler with the expression passed as <paramref name="fifth"/>.</param>
    /// <param name="sixthExpression">Filled in by the compiler with the expression passed as <paramref name="sixth"/>.</param>
    /// <param name="seventhExpression">Filled in by the compiler with the expression passed as <paramref name="seventh"/>.</param>
    /// <returns>A single sequence node. Follow it with <c>Select</c> to combine the results; the combined parser is still one node.</returns>
    /// <exception cref="GrammarException">A parser argument is null.</exception>
    /// <remarks>
    /// The sequence fails if any parser fails.
    /// Once a parser has consumed input, a later failure is committed, so an enclosing choice does not try its next alternative.
    /// </remarks>
    public static Parser<TIn, (T1, T2, T3, T4, T5, T6, T7)> Seq<T1, T2, T3, T4, T5, T6, T7>(
        Parser<TIn, T1> first,
        Parser<TIn, T2> second,
        Parser<TIn, T3> third,
        Parser<TIn, T4> fourth,
        Parser<TIn, T5> fifth,
        Parser<TIn, T6> sixth,
        Parser<TIn, T7> seventh,
        [CallerArgumentExpression(nameof(first))] string firstExpression = "",
        [CallerArgumentExpression(nameof(second))] string secondExpression = "",
        [CallerArgumentExpression(nameof(third))] string thirdExpression = "",
        [CallerArgumentExpression(nameof(fourth))] string fourthExpression = "",
        [CallerArgumentExpression(nameof(fifth))] string fifthExpression = "",
        [CallerArgumentExpression(nameof(sixth))] string sixthExpression = "",
        [CallerArgumentExpression(nameof(seventh))] string seventhExpression = ""
    ) =>
        new SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, (T1, T2, T3, T4, T5, T6, T7)>(
            Guard.NotNull(first, firstExpression),
            Guard.NotNull(second, secondExpression),
            Guard.NotNull(third, thirdExpression),
            Guard.NotNull(fourth, fourthExpression),
            Guard.NotNull(fifth, fifthExpression),
            Guard.NotNull(sixth, sixthExpression),
            Guard.NotNull(seventh, seventhExpression),
            SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, (T1, T2, T3, T4, T5, T6, T7)>.TupleSelector
        );

    /// <summary>
    /// A parser that runs eight parsers in order and produces their results as a tuple.
    /// </summary>
    /// <typeparam name="T1">The type of value the first parser produces.</typeparam>
    /// <typeparam name="T2">The type of value the second parser produces.</typeparam>
    /// <typeparam name="T3">The type of value the third parser produces.</typeparam>
    /// <typeparam name="T4">The type of value the fourth parser produces.</typeparam>
    /// <typeparam name="T5">The type of value the fifth parser produces.</typeparam>
    /// <typeparam name="T6">The type of value the sixth parser produces.</typeparam>
    /// <typeparam name="T7">The type of value the seventh parser produces.</typeparam>
    /// <typeparam name="T8">The type of value the eighth parser produces.</typeparam>
    /// <param name="first">The first parser to run.</param>
    /// <param name="second">The second parser to run.</param>
    /// <param name="third">The third parser to run.</param>
    /// <param name="fourth">The fourth parser to run.</param>
    /// <param name="fifth">The fifth parser to run.</param>
    /// <param name="sixth">The sixth parser to run.</param>
    /// <param name="seventh">The seventh parser to run.</param>
    /// <param name="eighth">The eighth parser to run.</param>
    /// <param name="firstExpression">Filled in by the compiler with the expression passed as <paramref name="first"/>.</param>
    /// <param name="secondExpression">Filled in by the compiler with the expression passed as <paramref name="second"/>.</param>
    /// <param name="thirdExpression">Filled in by the compiler with the expression passed as <paramref name="third"/>.</param>
    /// <param name="fourthExpression">Filled in by the compiler with the expression passed as <paramref name="fourth"/>.</param>
    /// <param name="fifthExpression">Filled in by the compiler with the expression passed as <paramref name="fifth"/>.</param>
    /// <param name="sixthExpression">Filled in by the compiler with the expression passed as <paramref name="sixth"/>.</param>
    /// <param name="seventhExpression">Filled in by the compiler with the expression passed as <paramref name="seventh"/>.</param>
    /// <param name="eighthExpression">Filled in by the compiler with the expression passed as <paramref name="eighth"/>.</param>
    /// <returns>A single sequence node. Follow it with <c>Select</c> to combine the results; the combined parser is still one node.</returns>
    /// <exception cref="GrammarException">A parser argument is null.</exception>
    /// <remarks>
    /// The sequence fails if any parser fails.
    /// Once a parser has consumed input, a later failure is committed, so an enclosing choice does not try its next alternative.
    /// </remarks>
    public static Parser<TIn, (T1, T2, T3, T4, T5, T6, T7, T8)> Seq<T1, T2, T3, T4, T5, T6, T7, T8>(
        Parser<TIn, T1> first,
        Parser<TIn, T2> second,
        Parser<TIn, T3> third,
        Parser<TIn, T4> fourth,
        Parser<TIn, T5> fifth,
        Parser<TIn, T6> sixth,
        Parser<TIn, T7> seventh,
        Parser<TIn, T8> eighth,
        [CallerArgumentExpression(nameof(first))] string firstExpression = "",
        [CallerArgumentExpression(nameof(second))] string secondExpression = "",
        [CallerArgumentExpression(nameof(third))] string thirdExpression = "",
        [CallerArgumentExpression(nameof(fourth))] string fourthExpression = "",
        [CallerArgumentExpression(nameof(fifth))] string fifthExpression = "",
        [CallerArgumentExpression(nameof(sixth))] string sixthExpression = "",
        [CallerArgumentExpression(nameof(seventh))] string seventhExpression = "",
        [CallerArgumentExpression(nameof(eighth))] string eighthExpression = ""
    ) =>
        new SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, T8, (T1, T2, T3, T4, T5, T6, T7, T8)>(
            Guard.NotNull(first, firstExpression),
            Guard.NotNull(second, secondExpression),
            Guard.NotNull(third, thirdExpression),
            Guard.NotNull(fourth, fourthExpression),
            Guard.NotNull(fifth, fifthExpression),
            Guard.NotNull(sixth, sixthExpression),
            Guard.NotNull(seventh, seventhExpression),
            Guard.NotNull(eighth, eighthExpression),
            SequenceNode<TIn, T1, T2, T3, T4, T5, T6, T7, T8, (T1, T2, T3, T4, T5, T6, T7, T8)>.TupleSelector
        );
}
