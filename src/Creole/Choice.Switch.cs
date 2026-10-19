using System.Runtime.CompilerServices;

namespace Creole;

public static partial class Choice
{
    extension<TIn, TKey>(Parser<TIn, TKey> parser)
    {
        /// <summary>
        /// A parser that runs this parser, then the branch of the case whose key equals its result, choosing from 1 case and <paramref name="default"/>.
        /// </summary>
        /// <typeparam name="TOut">The type every branch produces.</typeparam>
        /// <param name="case1">The first case: a key and the branch to run when this parser produces it.</param>
        /// <param name="default">The branch to run when this parser produces a value that matches no key.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case1Expression">
        /// The caller's expression for <paramref name="case1"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="defaultExpression">
        /// The caller's expression for <paramref name="default"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the chosen branch's result.</returns>
        /// <exception cref="GrammarException">This parser, a branch or <paramref name="default"/> is null.</exception>
        /// <remarks>
        /// <para>
        /// Keys are compared with <see cref="EqualityComparer{T}.Default"/>.
        /// The chosen branch runs from where this parser stopped, and its result is the result of the switch.
        /// If this parser or the chosen branch fails, the switch fails; the failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them, unlike a parser chosen by a function at parse time.
        /// A key must have this parser's type, or type inference fails: for a <see cref="byte"/> scrutinee, write <c>((byte)1, branch)</c>.
        /// For more than 8 cases, pass the cases as a collection instead: <c>Switch([(1, a), (2, b), …], @default)</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Switch<TOut>(
            (TKey Key, Parser<TIn, TOut> Branch) case1,
            Parser<TIn, TOut> @default,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(@default))] string defaultExpression = ""
        ) =>
            BuildSwitch(
                Guard.NotNull(parser, parserExpression),
                [case1],
                [case1Expression],
                Guard.NotNull(@default, defaultExpression)
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case whose key equals its result, choosing from 2 cases and <paramref name="default"/>.
        /// </summary>
        /// <typeparam name="TOut">The type every branch produces.</typeparam>
        /// <param name="case1">The first case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case2">The second case: a key and the branch to run when this parser produces it.</param>
        /// <param name="default">The branch to run when this parser produces a value that matches no key.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case1Expression">
        /// The caller's expression for <paramref name="case1"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case2Expression">
        /// The caller's expression for <paramref name="case2"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="defaultExpression">
        /// The caller's expression for <paramref name="default"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the chosen branch's result.</returns>
        /// <exception cref="GrammarException">This parser, a branch or <paramref name="default"/> is null, or two cases have the same key.</exception>
        /// <remarks>
        /// <para>
        /// Keys are compared with <see cref="EqualityComparer{T}.Default"/>.
        /// The chosen branch runs from where this parser stopped, and its result is the result of the switch.
        /// If this parser or the chosen branch fails, the switch fails; the failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them, unlike a parser chosen by a function at parse time.
        /// A key must have this parser's type, or type inference fails: for a <see cref="byte"/> scrutinee, write <c>((byte)1, branch)</c>.
        /// For more than 8 cases, pass the cases as a collection instead: <c>Switch([(1, a), (2, b), …], @default)</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Switch<TOut>(
            (TKey Key, Parser<TIn, TOut> Branch) case1,
            (TKey Key, Parser<TIn, TOut> Branch) case2,
            Parser<TIn, TOut> @default,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(@default))] string defaultExpression = ""
        ) =>
            BuildSwitch(
                Guard.NotNull(parser, parserExpression),
                [case1, case2],
                [case1Expression, case2Expression],
                Guard.NotNull(@default, defaultExpression)
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case whose key equals its result, choosing from 3 cases and <paramref name="default"/>.
        /// </summary>
        /// <typeparam name="TOut">The type every branch produces.</typeparam>
        /// <param name="case1">The first case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case2">The second case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case3">The third case: a key and the branch to run when this parser produces it.</param>
        /// <param name="default">The branch to run when this parser produces a value that matches no key.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case1Expression">
        /// The caller's expression for <paramref name="case1"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case2Expression">
        /// The caller's expression for <paramref name="case2"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case3Expression">
        /// The caller's expression for <paramref name="case3"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="defaultExpression">
        /// The caller's expression for <paramref name="default"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the chosen branch's result.</returns>
        /// <exception cref="GrammarException">This parser, a branch or <paramref name="default"/> is null, or two cases have the same key.</exception>
        /// <remarks>
        /// <para>
        /// Keys are compared with <see cref="EqualityComparer{T}.Default"/>.
        /// The chosen branch runs from where this parser stopped, and its result is the result of the switch.
        /// If this parser or the chosen branch fails, the switch fails; the failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them, unlike a parser chosen by a function at parse time.
        /// A key must have this parser's type, or type inference fails: for a <see cref="byte"/> scrutinee, write <c>((byte)1, branch)</c>.
        /// For more than 8 cases, pass the cases as a collection instead: <c>Switch([(1, a), (2, b), …], @default)</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Switch<TOut>(
            (TKey Key, Parser<TIn, TOut> Branch) case1,
            (TKey Key, Parser<TIn, TOut> Branch) case2,
            (TKey Key, Parser<TIn, TOut> Branch) case3,
            Parser<TIn, TOut> @default,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = "",
            [CallerArgumentExpression(nameof(@default))] string defaultExpression = ""
        ) =>
            BuildSwitch(
                Guard.NotNull(parser, parserExpression),
                [case1, case2, case3],
                [case1Expression, case2Expression, case3Expression],
                Guard.NotNull(@default, defaultExpression)
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case whose key equals its result, choosing from 4 cases and <paramref name="default"/>.
        /// </summary>
        /// <typeparam name="TOut">The type every branch produces.</typeparam>
        /// <param name="case1">The first case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case2">The second case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case3">The third case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case4">The fourth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="default">The branch to run when this parser produces a value that matches no key.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case1Expression">
        /// The caller's expression for <paramref name="case1"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case2Expression">
        /// The caller's expression for <paramref name="case2"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case3Expression">
        /// The caller's expression for <paramref name="case3"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case4Expression">
        /// The caller's expression for <paramref name="case4"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="defaultExpression">
        /// The caller's expression for <paramref name="default"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the chosen branch's result.</returns>
        /// <exception cref="GrammarException">This parser, a branch or <paramref name="default"/> is null, or two cases have the same key.</exception>
        /// <remarks>
        /// <para>
        /// Keys are compared with <see cref="EqualityComparer{T}.Default"/>.
        /// The chosen branch runs from where this parser stopped, and its result is the result of the switch.
        /// If this parser or the chosen branch fails, the switch fails; the failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them, unlike a parser chosen by a function at parse time.
        /// A key must have this parser's type, or type inference fails: for a <see cref="byte"/> scrutinee, write <c>((byte)1, branch)</c>.
        /// For more than 8 cases, pass the cases as a collection instead: <c>Switch([(1, a), (2, b), …], @default)</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Switch<TOut>(
            (TKey Key, Parser<TIn, TOut> Branch) case1,
            (TKey Key, Parser<TIn, TOut> Branch) case2,
            (TKey Key, Parser<TIn, TOut> Branch) case3,
            (TKey Key, Parser<TIn, TOut> Branch) case4,
            Parser<TIn, TOut> @default,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = "",
            [CallerArgumentExpression(nameof(case4))] string case4Expression = "",
            [CallerArgumentExpression(nameof(@default))] string defaultExpression = ""
        ) =>
            BuildSwitch(
                Guard.NotNull(parser, parserExpression),
                [case1, case2, case3, case4],
                [case1Expression, case2Expression, case3Expression, case4Expression],
                Guard.NotNull(@default, defaultExpression)
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case whose key equals its result, choosing from 5 cases and <paramref name="default"/>.
        /// </summary>
        /// <typeparam name="TOut">The type every branch produces.</typeparam>
        /// <param name="case1">The first case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case2">The second case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case3">The third case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case4">The fourth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case5">The fifth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="default">The branch to run when this parser produces a value that matches no key.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case1Expression">
        /// The caller's expression for <paramref name="case1"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case2Expression">
        /// The caller's expression for <paramref name="case2"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case3Expression">
        /// The caller's expression for <paramref name="case3"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case4Expression">
        /// The caller's expression for <paramref name="case4"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case5Expression">
        /// The caller's expression for <paramref name="case5"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="defaultExpression">
        /// The caller's expression for <paramref name="default"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the chosen branch's result.</returns>
        /// <exception cref="GrammarException">This parser, a branch or <paramref name="default"/> is null, or two cases have the same key.</exception>
        /// <remarks>
        /// <para>
        /// Keys are compared with <see cref="EqualityComparer{T}.Default"/>.
        /// The chosen branch runs from where this parser stopped, and its result is the result of the switch.
        /// If this parser or the chosen branch fails, the switch fails; the failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them, unlike a parser chosen by a function at parse time.
        /// A key must have this parser's type, or type inference fails: for a <see cref="byte"/> scrutinee, write <c>((byte)1, branch)</c>.
        /// For more than 8 cases, pass the cases as a collection instead: <c>Switch([(1, a), (2, b), …], @default)</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Switch<TOut>(
            (TKey Key, Parser<TIn, TOut> Branch) case1,
            (TKey Key, Parser<TIn, TOut> Branch) case2,
            (TKey Key, Parser<TIn, TOut> Branch) case3,
            (TKey Key, Parser<TIn, TOut> Branch) case4,
            (TKey Key, Parser<TIn, TOut> Branch) case5,
            Parser<TIn, TOut> @default,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = "",
            [CallerArgumentExpression(nameof(case4))] string case4Expression = "",
            [CallerArgumentExpression(nameof(case5))] string case5Expression = "",
            [CallerArgumentExpression(nameof(@default))] string defaultExpression = ""
        ) =>
            BuildSwitch(
                Guard.NotNull(parser, parserExpression),
                [case1, case2, case3, case4, case5],
                [case1Expression, case2Expression, case3Expression, case4Expression, case5Expression],
                Guard.NotNull(@default, defaultExpression)
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case whose key equals its result, choosing from 6 cases and <paramref name="default"/>.
        /// </summary>
        /// <typeparam name="TOut">The type every branch produces.</typeparam>
        /// <param name="case1">The first case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case2">The second case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case3">The third case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case4">The fourth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case5">The fifth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case6">The sixth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="default">The branch to run when this parser produces a value that matches no key.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case1Expression">
        /// The caller's expression for <paramref name="case1"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case2Expression">
        /// The caller's expression for <paramref name="case2"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case3Expression">
        /// The caller's expression for <paramref name="case3"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case4Expression">
        /// The caller's expression for <paramref name="case4"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case5Expression">
        /// The caller's expression for <paramref name="case5"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case6Expression">
        /// The caller's expression for <paramref name="case6"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="defaultExpression">
        /// The caller's expression for <paramref name="default"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the chosen branch's result.</returns>
        /// <exception cref="GrammarException">This parser, a branch or <paramref name="default"/> is null, or two cases have the same key.</exception>
        /// <remarks>
        /// <para>
        /// Keys are compared with <see cref="EqualityComparer{T}.Default"/>.
        /// The chosen branch runs from where this parser stopped, and its result is the result of the switch.
        /// If this parser or the chosen branch fails, the switch fails; the failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them, unlike a parser chosen by a function at parse time.
        /// A key must have this parser's type, or type inference fails: for a <see cref="byte"/> scrutinee, write <c>((byte)1, branch)</c>.
        /// For more than 8 cases, pass the cases as a collection instead: <c>Switch([(1, a), (2, b), …], @default)</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Switch<TOut>(
            (TKey Key, Parser<TIn, TOut> Branch) case1,
            (TKey Key, Parser<TIn, TOut> Branch) case2,
            (TKey Key, Parser<TIn, TOut> Branch) case3,
            (TKey Key, Parser<TIn, TOut> Branch) case4,
            (TKey Key, Parser<TIn, TOut> Branch) case5,
            (TKey Key, Parser<TIn, TOut> Branch) case6,
            Parser<TIn, TOut> @default,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = "",
            [CallerArgumentExpression(nameof(case4))] string case4Expression = "",
            [CallerArgumentExpression(nameof(case5))] string case5Expression = "",
            [CallerArgumentExpression(nameof(case6))] string case6Expression = "",
            [CallerArgumentExpression(nameof(@default))] string defaultExpression = ""
        ) =>
            BuildSwitch(
                Guard.NotNull(parser, parserExpression),
                [case1, case2, case3, case4, case5, case6],
                [case1Expression, case2Expression, case3Expression, case4Expression, case5Expression, case6Expression],
                Guard.NotNull(@default, defaultExpression)
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case whose key equals its result, choosing from 7 cases and <paramref name="default"/>.
        /// </summary>
        /// <typeparam name="TOut">The type every branch produces.</typeparam>
        /// <param name="case1">The first case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case2">The second case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case3">The third case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case4">The fourth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case5">The fifth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case6">The sixth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case7">The seventh case: a key and the branch to run when this parser produces it.</param>
        /// <param name="default">The branch to run when this parser produces a value that matches no key.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case1Expression">
        /// The caller's expression for <paramref name="case1"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case2Expression">
        /// The caller's expression for <paramref name="case2"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case3Expression">
        /// The caller's expression for <paramref name="case3"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case4Expression">
        /// The caller's expression for <paramref name="case4"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case5Expression">
        /// The caller's expression for <paramref name="case5"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case6Expression">
        /// The caller's expression for <paramref name="case6"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case7Expression">
        /// The caller's expression for <paramref name="case7"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="defaultExpression">
        /// The caller's expression for <paramref name="default"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the chosen branch's result.</returns>
        /// <exception cref="GrammarException">This parser, a branch or <paramref name="default"/> is null, or two cases have the same key.</exception>
        /// <remarks>
        /// <para>
        /// Keys are compared with <see cref="EqualityComparer{T}.Default"/>.
        /// The chosen branch runs from where this parser stopped, and its result is the result of the switch.
        /// If this parser or the chosen branch fails, the switch fails; the failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them, unlike a parser chosen by a function at parse time.
        /// A key must have this parser's type, or type inference fails: for a <see cref="byte"/> scrutinee, write <c>((byte)1, branch)</c>.
        /// For more than 8 cases, pass the cases as a collection instead: <c>Switch([(1, a), (2, b), …], @default)</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Switch<TOut>(
            (TKey Key, Parser<TIn, TOut> Branch) case1,
            (TKey Key, Parser<TIn, TOut> Branch) case2,
            (TKey Key, Parser<TIn, TOut> Branch) case3,
            (TKey Key, Parser<TIn, TOut> Branch) case4,
            (TKey Key, Parser<TIn, TOut> Branch) case5,
            (TKey Key, Parser<TIn, TOut> Branch) case6,
            (TKey Key, Parser<TIn, TOut> Branch) case7,
            Parser<TIn, TOut> @default,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = "",
            [CallerArgumentExpression(nameof(case4))] string case4Expression = "",
            [CallerArgumentExpression(nameof(case5))] string case5Expression = "",
            [CallerArgumentExpression(nameof(case6))] string case6Expression = "",
            [CallerArgumentExpression(nameof(case7))] string case7Expression = "",
            [CallerArgumentExpression(nameof(@default))] string defaultExpression = ""
        ) =>
            BuildSwitch(
                Guard.NotNull(parser, parserExpression),
                [case1, case2, case3, case4, case5, case6, case7],
                [case1Expression, case2Expression, case3Expression, case4Expression, case5Expression, case6Expression, case7Expression],
                Guard.NotNull(@default, defaultExpression)
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case whose key equals its result, choosing from 8 cases and <paramref name="default"/>.
        /// </summary>
        /// <typeparam name="TOut">The type every branch produces.</typeparam>
        /// <param name="case1">The first case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case2">The second case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case3">The third case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case4">The fourth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case5">The fifth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case6">The sixth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case7">The seventh case: a key and the branch to run when this parser produces it.</param>
        /// <param name="case8">The eighth case: a key and the branch to run when this parser produces it.</param>
        /// <param name="default">The branch to run when this parser produces a value that matches no key.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case1Expression">
        /// The caller's expression for <paramref name="case1"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case2Expression">
        /// The caller's expression for <paramref name="case2"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case3Expression">
        /// The caller's expression for <paramref name="case3"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case4Expression">
        /// The caller's expression for <paramref name="case4"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case5Expression">
        /// The caller's expression for <paramref name="case5"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case6Expression">
        /// The caller's expression for <paramref name="case6"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case7Expression">
        /// The caller's expression for <paramref name="case7"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case8Expression">
        /// The caller's expression for <paramref name="case8"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="defaultExpression">
        /// The caller's expression for <paramref name="default"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the chosen branch's result.</returns>
        /// <exception cref="GrammarException">This parser, a branch or <paramref name="default"/> is null, or two cases have the same key.</exception>
        /// <remarks>
        /// <para>
        /// Keys are compared with <see cref="EqualityComparer{T}.Default"/>.
        /// The chosen branch runs from where this parser stopped, and its result is the result of the switch.
        /// If this parser or the chosen branch fails, the switch fails; the failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them, unlike a parser chosen by a function at parse time.
        /// A key must have this parser's type, or type inference fails: for a <see cref="byte"/> scrutinee, write <c>((byte)1, branch)</c>.
        /// For more than 8 cases, pass the cases as a collection instead: <c>Switch([(1, a), (2, b), …], @default)</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Switch<TOut>(
            (TKey Key, Parser<TIn, TOut> Branch) case1,
            (TKey Key, Parser<TIn, TOut> Branch) case2,
            (TKey Key, Parser<TIn, TOut> Branch) case3,
            (TKey Key, Parser<TIn, TOut> Branch) case4,
            (TKey Key, Parser<TIn, TOut> Branch) case5,
            (TKey Key, Parser<TIn, TOut> Branch) case6,
            (TKey Key, Parser<TIn, TOut> Branch) case7,
            (TKey Key, Parser<TIn, TOut> Branch) case8,
            Parser<TIn, TOut> @default,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = "",
            [CallerArgumentExpression(nameof(case4))] string case4Expression = "",
            [CallerArgumentExpression(nameof(case5))] string case5Expression = "",
            [CallerArgumentExpression(nameof(case6))] string case6Expression = "",
            [CallerArgumentExpression(nameof(case7))] string case7Expression = "",
            [CallerArgumentExpression(nameof(case8))] string case8Expression = "",
            [CallerArgumentExpression(nameof(@default))] string defaultExpression = ""
        ) =>
            BuildSwitch(
                Guard.NotNull(parser, parserExpression),
                [case1, case2, case3, case4, case5, case6, case7, case8],
                [case1Expression, case2Expression, case3Expression, case4Expression, case5Expression, case6Expression, case7Expression, case8Expression],
                Guard.NotNull(@default, defaultExpression)
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case whose key equals its result, choosing from any number of <paramref name="cases"/> and <paramref name="default"/>.
        /// </summary>
        /// <typeparam name="TOut">The type every branch produces.</typeparam>
        /// <param name="cases">The cases, each a key and the branch to run when this parser produces it. The switch keeps a copy.</param>
        /// <param name="default">The branch to run when this parser produces a value that matches no key.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="casesExpression">
        /// The caller's expression for <paramref name="cases"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="defaultExpression">
        /// The caller's expression for <paramref name="default"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the chosen branch's result.</returns>
        /// <exception cref="GrammarException">
        /// This parser, <paramref name="cases"/>, a branch or <paramref name="default"/> is null;
        /// <paramref name="cases"/> is empty;
        /// or two cases have the same key.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Behaves like the overloads that take each case as a separate argument, and builds the same grammar node.
        /// Use it for more than 8 cases, or for a table of cases built in code.
        /// Error messages count cases from 0.
        /// </para>
        /// <para>
        /// Keys are compared with <see cref="EqualityComparer{T}.Default"/>.
        /// The chosen branch runs from where this parser stopped, and its result is the result of the switch.
        /// If this parser or the chosen branch fails, the switch fails; the failure is committed if any input was consumed.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Switch<TOut>(
            IReadOnlyList<(TKey Key, Parser<TIn, TOut> Branch)> cases,
            Parser<TIn, TOut> @default,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(cases))] string casesExpression = "",
            [CallerArgumentExpression(nameof(@default))] string defaultExpression = ""
        )
        {
            Guard.NotNull(parser, parserExpression);
            Guard.NotNull(cases, casesExpression);

            if (cases.Count == 0)
            {
                throw new GrammarException($"'{casesExpression}' has no cases.");
            }

            return BuildSwitch(parser, [.. cases], null, Guard.NotNull(@default, defaultExpression));
        }
    }

    /// <summary>
    /// Copies the cases into a new node, rejecting a null branch or a repeated key.
    /// Cases are named by <paramref name="caseExpressions"/> in messages, or by index when it is null.
    /// </summary>
    private static SwitchNode<TIn, TKey, TOut> BuildSwitch<TIn, TKey, TOut>(
        Parser<TIn, TKey> scrutinee,
        (TKey Key, Parser<TIn, TOut> Branch)[] cases,
        string[]? caseExpressions,
        Parser<TIn, TOut> @default
    )
    {
        string Describe(int i) => caseExpressions is null ? $"case {i}" : $"'{caseExpressions[i]}'";

        var keys = new TKey[cases.Length];
        var branches = new Parser<TIn, TOut>[cases.Length];

        for (var i = 0; i < cases.Length; i++)
        {
            keys[i] = cases[i].Key;
            branches[i] = Guard.BranchNotNull(cases[i].Branch, Describe(i));

            for (var j = 0; j < i; j++)
            {
                if (EqualityComparer<TKey>.Default.Equals(keys[j], keys[i]))
                {
                    throw new GrammarException($"Two cases of a Switch have the same key: {Describe(j)} and {Describe(i)}. Each key may appear in only one case.");
                }
            }
        }

        return new SwitchNode<TIn, TKey, TOut>(scrutinee, keys, branches, @default);
    }
}
