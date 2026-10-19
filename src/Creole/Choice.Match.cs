using System.Runtime.CompilerServices;

namespace Creole;

public static partial class Choice
{
    extension<TIn, TUnion>(Parser<TIn, TUnion> parser)
        where TUnion : IUnion
    {
        /// <summary>
        /// A parser that runs this parser, then the branch of the case for the union value it produces, choosing from 1 case.
        /// </summary>
        /// <typeparam name="TOut">The type every case produces.</typeparam>
        /// <param name="case1">The first case, made with <c>Case</c>.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="case1Expression">
        /// The caller's expression for <paramref name="case1"/>, used in error messages.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the applying case's result.</returns>
        /// <exception cref="GrammarException">This parser or the case is null.</exception>
        /// <remarks>
        /// <para>
        /// The cases are tried in order, and a case applies when the union's value is of its type.
        /// The case's branch runs from where this parser stopped, and its combine function receives the value and the branch's result.
        /// If no case applies, as for a union case without a branch or a <see langword="default"/> union, the match fails.
        /// If this parser or the branch fails, the match fails. Each failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them and can check that every union case has one.
        /// </para>
        /// <para>
        /// For more than 8 cases, pass the cases as a collection instead: <c>Match([Case(...), Case(...), …])</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Match<TOut>(
            MatchCase<TIn, TOut> case1,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = ""
        ) =>
            BuildMatch(
                Guard.NotNull(parser, parserExpression),
                [Guard.NotNull(case1, case1Expression)],
                [case1Expression]
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case for the union value it produces, choosing from 2 cases.
        /// </summary>
        /// <typeparam name="TOut">The type every case produces.</typeparam>
        /// <param name="case1">The first case, made with <c>Case</c>.</param>
        /// <param name="case2">The second case, made with <c>Case</c>.</param>
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
        /// <returns>A parser that produces the applying case's result.</returns>
        /// <exception cref="GrammarException">This parser or a case is null, or two cases have the same case type.</exception>
        /// <remarks>
        /// <para>
        /// The cases are tried in order, and a case applies when the union's value is of its type.
        /// The case's branch runs from where this parser stopped, and its combine function receives the value and the branch's result.
        /// If no case applies, as for a union case without a branch or a <see langword="default"/> union, the match fails.
        /// If this parser or the branch fails, the match fails. Each failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them and can check that every union case has one.
        /// </para>
        /// <para>
        /// For more than 8 cases, pass the cases as a collection instead: <c>Match([Case(...), Case(...), …])</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Match<TOut>(
            MatchCase<TIn, TOut> case1,
            MatchCase<TIn, TOut> case2,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = ""
        ) =>
            BuildMatch(
                Guard.NotNull(parser, parserExpression),
                [Guard.NotNull(case1, case1Expression), Guard.NotNull(case2, case2Expression)],
                [case1Expression, case2Expression]
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case for the union value it produces, choosing from 3 cases.
        /// </summary>
        /// <typeparam name="TOut">The type every case produces.</typeparam>
        /// <param name="case1">The first case, made with <c>Case</c>.</param>
        /// <param name="case2">The second case, made with <c>Case</c>.</param>
        /// <param name="case3">The third case, made with <c>Case</c>.</param>
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
        /// <returns>A parser that produces the applying case's result.</returns>
        /// <exception cref="GrammarException">This parser or a case is null, or two cases have the same case type.</exception>
        /// <remarks>
        /// <para>
        /// The cases are tried in order, and a case applies when the union's value is of its type.
        /// The case's branch runs from where this parser stopped, and its combine function receives the value and the branch's result.
        /// If no case applies, as for a union case without a branch or a <see langword="default"/> union, the match fails.
        /// If this parser or the branch fails, the match fails. Each failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them and can check that every union case has one.
        /// </para>
        /// <para>
        /// For more than 8 cases, pass the cases as a collection instead: <c>Match([Case(...), Case(...), …])</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Match<TOut>(
            MatchCase<TIn, TOut> case1,
            MatchCase<TIn, TOut> case2,
            MatchCase<TIn, TOut> case3,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = ""
        ) =>
            BuildMatch(
                Guard.NotNull(parser, parserExpression),
                [Guard.NotNull(case1, case1Expression), Guard.NotNull(case2, case2Expression), Guard.NotNull(case3, case3Expression)],
                [case1Expression, case2Expression, case3Expression]
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case for the union value it produces, choosing from 4 cases.
        /// </summary>
        /// <typeparam name="TOut">The type every case produces.</typeparam>
        /// <param name="case1">The first case, made with <c>Case</c>.</param>
        /// <param name="case2">The second case, made with <c>Case</c>.</param>
        /// <param name="case3">The third case, made with <c>Case</c>.</param>
        /// <param name="case4">The fourth case, made with <c>Case</c>.</param>
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
        /// <returns>A parser that produces the applying case's result.</returns>
        /// <exception cref="GrammarException">This parser or a case is null, or two cases have the same case type.</exception>
        /// <remarks>
        /// <para>
        /// The cases are tried in order, and a case applies when the union's value is of its type.
        /// The case's branch runs from where this parser stopped, and its combine function receives the value and the branch's result.
        /// If no case applies, as for a union case without a branch or a <see langword="default"/> union, the match fails.
        /// If this parser or the branch fails, the match fails. Each failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them and can check that every union case has one.
        /// </para>
        /// <para>
        /// For more than 8 cases, pass the cases as a collection instead: <c>Match([Case(...), Case(...), …])</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Match<TOut>(
            MatchCase<TIn, TOut> case1,
            MatchCase<TIn, TOut> case2,
            MatchCase<TIn, TOut> case3,
            MatchCase<TIn, TOut> case4,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = "",
            [CallerArgumentExpression(nameof(case4))] string case4Expression = ""
        ) =>
            BuildMatch(
                Guard.NotNull(parser, parserExpression),
                [Guard.NotNull(case1, case1Expression), Guard.NotNull(case2, case2Expression), Guard.NotNull(case3, case3Expression), Guard.NotNull(case4, case4Expression)],
                [case1Expression, case2Expression, case3Expression, case4Expression]
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case for the union value it produces, choosing from 5 cases.
        /// </summary>
        /// <typeparam name="TOut">The type every case produces.</typeparam>
        /// <param name="case1">The first case, made with <c>Case</c>.</param>
        /// <param name="case2">The second case, made with <c>Case</c>.</param>
        /// <param name="case3">The third case, made with <c>Case</c>.</param>
        /// <param name="case4">The fourth case, made with <c>Case</c>.</param>
        /// <param name="case5">The fifth case, made with <c>Case</c>.</param>
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
        /// <returns>A parser that produces the applying case's result.</returns>
        /// <exception cref="GrammarException">This parser or a case is null, or two cases have the same case type.</exception>
        /// <remarks>
        /// <para>
        /// The cases are tried in order, and a case applies when the union's value is of its type.
        /// The case's branch runs from where this parser stopped, and its combine function receives the value and the branch's result.
        /// If no case applies, as for a union case without a branch or a <see langword="default"/> union, the match fails.
        /// If this parser or the branch fails, the match fails. Each failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them and can check that every union case has one.
        /// </para>
        /// <para>
        /// For more than 8 cases, pass the cases as a collection instead: <c>Match([Case(...), Case(...), …])</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Match<TOut>(
            MatchCase<TIn, TOut> case1,
            MatchCase<TIn, TOut> case2,
            MatchCase<TIn, TOut> case3,
            MatchCase<TIn, TOut> case4,
            MatchCase<TIn, TOut> case5,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = "",
            [CallerArgumentExpression(nameof(case4))] string case4Expression = "",
            [CallerArgumentExpression(nameof(case5))] string case5Expression = ""
        ) =>
            BuildMatch(
                Guard.NotNull(parser, parserExpression),
                [Guard.NotNull(case1, case1Expression), Guard.NotNull(case2, case2Expression), Guard.NotNull(case3, case3Expression), Guard.NotNull(case4, case4Expression), Guard.NotNull(case5, case5Expression)],
                [case1Expression, case2Expression, case3Expression, case4Expression, case5Expression]
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case for the union value it produces, choosing from 6 cases.
        /// </summary>
        /// <typeparam name="TOut">The type every case produces.</typeparam>
        /// <param name="case1">The first case, made with <c>Case</c>.</param>
        /// <param name="case2">The second case, made with <c>Case</c>.</param>
        /// <param name="case3">The third case, made with <c>Case</c>.</param>
        /// <param name="case4">The fourth case, made with <c>Case</c>.</param>
        /// <param name="case5">The fifth case, made with <c>Case</c>.</param>
        /// <param name="case6">The sixth case, made with <c>Case</c>.</param>
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
        /// <returns>A parser that produces the applying case's result.</returns>
        /// <exception cref="GrammarException">This parser or a case is null, or two cases have the same case type.</exception>
        /// <remarks>
        /// <para>
        /// The cases are tried in order, and a case applies when the union's value is of its type.
        /// The case's branch runs from where this parser stopped, and its combine function receives the value and the branch's result.
        /// If no case applies, as for a union case without a branch or a <see langword="default"/> union, the match fails.
        /// If this parser or the branch fails, the match fails. Each failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them and can check that every union case has one.
        /// </para>
        /// <para>
        /// For more than 8 cases, pass the cases as a collection instead: <c>Match([Case(...), Case(...), …])</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Match<TOut>(
            MatchCase<TIn, TOut> case1,
            MatchCase<TIn, TOut> case2,
            MatchCase<TIn, TOut> case3,
            MatchCase<TIn, TOut> case4,
            MatchCase<TIn, TOut> case5,
            MatchCase<TIn, TOut> case6,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = "",
            [CallerArgumentExpression(nameof(case4))] string case4Expression = "",
            [CallerArgumentExpression(nameof(case5))] string case5Expression = "",
            [CallerArgumentExpression(nameof(case6))] string case6Expression = ""
        ) =>
            BuildMatch(
                Guard.NotNull(parser, parserExpression),
                [Guard.NotNull(case1, case1Expression), Guard.NotNull(case2, case2Expression), Guard.NotNull(case3, case3Expression), Guard.NotNull(case4, case4Expression), Guard.NotNull(case5, case5Expression), Guard.NotNull(case6, case6Expression)],
                [case1Expression, case2Expression, case3Expression, case4Expression, case5Expression, case6Expression]
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case for the union value it produces, choosing from 7 cases.
        /// </summary>
        /// <typeparam name="TOut">The type every case produces.</typeparam>
        /// <param name="case1">The first case, made with <c>Case</c>.</param>
        /// <param name="case2">The second case, made with <c>Case</c>.</param>
        /// <param name="case3">The third case, made with <c>Case</c>.</param>
        /// <param name="case4">The fourth case, made with <c>Case</c>.</param>
        /// <param name="case5">The fifth case, made with <c>Case</c>.</param>
        /// <param name="case6">The sixth case, made with <c>Case</c>.</param>
        /// <param name="case7">The seventh case, made with <c>Case</c>.</param>
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
        /// <returns>A parser that produces the applying case's result.</returns>
        /// <exception cref="GrammarException">This parser or a case is null, or two cases have the same case type.</exception>
        /// <remarks>
        /// <para>
        /// The cases are tried in order, and a case applies when the union's value is of its type.
        /// The case's branch runs from where this parser stopped, and its combine function receives the value and the branch's result.
        /// If no case applies, as for a union case without a branch or a <see langword="default"/> union, the match fails.
        /// If this parser or the branch fails, the match fails. Each failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them and can check that every union case has one.
        /// </para>
        /// <para>
        /// For more than 8 cases, pass the cases as a collection instead: <c>Match([Case(...), Case(...), …])</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Match<TOut>(
            MatchCase<TIn, TOut> case1,
            MatchCase<TIn, TOut> case2,
            MatchCase<TIn, TOut> case3,
            MatchCase<TIn, TOut> case4,
            MatchCase<TIn, TOut> case5,
            MatchCase<TIn, TOut> case6,
            MatchCase<TIn, TOut> case7,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = "",
            [CallerArgumentExpression(nameof(case4))] string case4Expression = "",
            [CallerArgumentExpression(nameof(case5))] string case5Expression = "",
            [CallerArgumentExpression(nameof(case6))] string case6Expression = "",
            [CallerArgumentExpression(nameof(case7))] string case7Expression = ""
        ) =>
            BuildMatch(
                Guard.NotNull(parser, parserExpression),
                [Guard.NotNull(case1, case1Expression), Guard.NotNull(case2, case2Expression), Guard.NotNull(case3, case3Expression), Guard.NotNull(case4, case4Expression), Guard.NotNull(case5, case5Expression), Guard.NotNull(case6, case6Expression), Guard.NotNull(case7, case7Expression)],
                [case1Expression, case2Expression, case3Expression, case4Expression, case5Expression, case6Expression, case7Expression]
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case for the union value it produces, choosing from 8 cases.
        /// </summary>
        /// <typeparam name="TOut">The type every case produces.</typeparam>
        /// <param name="case1">The first case, made with <c>Case</c>.</param>
        /// <param name="case2">The second case, made with <c>Case</c>.</param>
        /// <param name="case3">The third case, made with <c>Case</c>.</param>
        /// <param name="case4">The fourth case, made with <c>Case</c>.</param>
        /// <param name="case5">The fifth case, made with <c>Case</c>.</param>
        /// <param name="case6">The sixth case, made with <c>Case</c>.</param>
        /// <param name="case7">The seventh case, made with <c>Case</c>.</param>
        /// <param name="case8">The eighth case, made with <c>Case</c>.</param>
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
        /// <returns>A parser that produces the applying case's result.</returns>
        /// <exception cref="GrammarException">This parser or a case is null, or two cases have the same case type.</exception>
        /// <remarks>
        /// <para>
        /// The cases are tried in order, and a case applies when the union's value is of its type.
        /// The case's branch runs from where this parser stopped, and its combine function receives the value and the branch's result.
        /// If no case applies, as for a union case without a branch or a <see langword="default"/> union, the match fails.
        /// If this parser or the branch fails, the match fails. Each failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them and can check that every union case has one.
        /// </para>
        /// <para>
        /// For more than 8 cases, pass the cases as a collection instead: <c>Match([Case(...), Case(...), …])</c>.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Match<TOut>(
            MatchCase<TIn, TOut> case1,
            MatchCase<TIn, TOut> case2,
            MatchCase<TIn, TOut> case3,
            MatchCase<TIn, TOut> case4,
            MatchCase<TIn, TOut> case5,
            MatchCase<TIn, TOut> case6,
            MatchCase<TIn, TOut> case7,
            MatchCase<TIn, TOut> case8,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(case1))] string case1Expression = "",
            [CallerArgumentExpression(nameof(case2))] string case2Expression = "",
            [CallerArgumentExpression(nameof(case3))] string case3Expression = "",
            [CallerArgumentExpression(nameof(case4))] string case4Expression = "",
            [CallerArgumentExpression(nameof(case5))] string case5Expression = "",
            [CallerArgumentExpression(nameof(case6))] string case6Expression = "",
            [CallerArgumentExpression(nameof(case7))] string case7Expression = "",
            [CallerArgumentExpression(nameof(case8))] string case8Expression = ""
        ) =>
            BuildMatch(
                Guard.NotNull(parser, parserExpression),
                [Guard.NotNull(case1, case1Expression), Guard.NotNull(case2, case2Expression), Guard.NotNull(case3, case3Expression), Guard.NotNull(case4, case4Expression), Guard.NotNull(case5, case5Expression), Guard.NotNull(case6, case6Expression), Guard.NotNull(case7, case7Expression), Guard.NotNull(case8, case8Expression)],
                [case1Expression, case2Expression, case3Expression, case4Expression, case5Expression, case6Expression, case7Expression, case8Expression]
            );

        /// <summary>
        /// A parser that runs this parser, then the branch of the case for the union value it produces, choosing from any number of <paramref name="cases"/>.
        /// </summary>
        /// <typeparam name="TOut">The type every case produces.</typeparam>
        /// <param name="cases">The cases, each made with <c>Case</c>. The match keeps a copy.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <param name="casesExpression">
        /// The caller's expression for <paramref name="cases"/>, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>A parser that produces the applying case's result.</returns>
        /// <exception cref="GrammarException">
        /// This parser, <paramref name="cases"/> or a case is null;
        /// <paramref name="cases"/> is empty;
        /// or two cases have the same case type.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Behaves like the overloads that take each case as a separate argument, and builds the same grammar node.
        /// Use it for more than 8 cases, or for cases built in code.
        /// Error messages count cases from 0.
        /// </para>
        /// <para>
        /// The cases are tried in order, and a case applies when the union's value is of its type.
        /// The case's branch runs from where this parser stopped, and its combine function receives the value and the branch's result.
        /// If no case applies, as for a union case without a branch or a <see langword="default"/> union, the match fails.
        /// If this parser or the branch fails, the match fails. Each failure is committed if any input was consumed.
        /// </para>
        /// <para>
        /// Every branch is part of the grammar, so analysis sees all of them and can check that every union case has one.
        /// </para>
        /// </remarks>
        public Parser<TIn, TOut> Match<TOut>(
            IReadOnlyList<MatchCase<TIn, TOut>> cases,
            [CallerArgumentExpression(nameof(parser))] string parserExpression = "",
            [CallerArgumentExpression(nameof(cases))] string casesExpression = ""
        )
        {
            Guard.NotNull(parser, parserExpression);
            Guard.NotNull(cases, casesExpression);

            if (cases.Count == 0)
            {
                throw new GrammarException($"'{casesExpression}' has no cases.");
            }

            MatchCase<TIn, TOut>[] copy = [.. cases];

            for (var i = 0; i < copy.Length; i++)
            {
                if (copy[i] is null)
                {
                    throw new GrammarException($"Case {i} of the Match is null.");
                }
            }

            return BuildMatch(parser, copy, null);
        }
    }

    /// <summary>
    /// Builds the node, rejecting two cases for the same case type.
    /// Cases are named by <paramref name="caseExpressions"/> in messages, or by index when it is null.
    /// </summary>
    private static MatchNode<TIn, TUnion, TOut> BuildMatch<TIn, TUnion, TOut>(
        Parser<TIn, TUnion> scrutinee,
        MatchCase<TIn, TOut>[] cases,
        string[]? caseExpressions
    )
        where TUnion : IUnion
    {
        string Describe(int i) =>
            caseExpressions is null
            ? $"case {i}"
            : $"'{caseExpressions[i]}'";

        for (var i = 0; i < cases.Length; i++)
        {
            for (var j = 0; j < i; j++)
            {
                if (cases[j].CaseType == cases[i].CaseType)
                {
                    throw new GrammarException($"Two cases of a Match are for the same type: {Describe(j)} and {Describe(i)}. Each case type may appear in only one case.");
                }
            }
        }

        return new MatchNode<TIn, TUnion, TOut>(scrutinee, cases);
    }
}
