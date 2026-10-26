using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Runs a parser on text or on items in memory, called as a method on the parser: <c>parser.Parse(text)</c>.
/// </summary>
/// <remarks>
/// <para>
/// A parse succeeds when the parser does, even if input remains after it. To require the whole input, end the grammar with <c>End</c>, as in <c>Value.Before(End)</c>.
/// </para>
/// <para>
/// A parser can be run from several threads at once. Each parse keeps its own state.
/// </para>
/// <para>
/// A parse runs the grammar without recording failures. Only if it fails does it run the grammar again from the start, recording them, to build its error.
/// So a successful parse pays nothing for errors, a failed parse costs up to about twice as much, and the grammar's functions run again when a parse fails.
/// </para>
/// </remarks>
public static class Parsing
{
    private const string Layer = "parser";

    extension<TOut>(Parser<char, TOut> parser)
    {
        /// <summary>
        /// Parses <paramref name="input"/> with this parser.
        /// </summary>
        /// <param name="input">The text to parse.</param>
        /// <param name="source">The name to show the input under in diagnostics, such as a file name, or <see langword="null"/> for none.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// <see cref="Parsed{T}"/> with the parser's value if it succeeded.
        /// Otherwise <see cref="Failed"/> with one error, which holds <paramref name="input"/> so that <see cref="Diagnostics.Render()"/> shows the error against it.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The error is the parse's furthest failure, from layer <c>parser</c>, with spans that are character offsets in <paramref name="input"/> and the source <see cref="SourceId.Unnamed"/>.
        /// A parse that goes deeper than <see cref="ParseOptions.MaxDepth"/>, or runs short of stack, fails with one error where the level would have started, whose message names the cause.
        /// </para>
        /// <para>
        /// The result refers to <paramref name="input"/> and copies none of it.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This parser is null, or its grammar is not valid.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
        public ParseResult<TOut> Parse(string input, string? source = null, ParseOptions? options = null, [CallerArgumentExpression(nameof(parser))] string parserExpression = "")
        {
            var checkedParser = Guard.NotNull(parser, parserExpression);

            ArgumentNullException.ThrowIfNull(input);

            return Run(checkedParser, input.AsMemory(), source, options ?? ParseOptions.Default);
        }

        /// <summary>
        /// Parses <paramref name="input"/> with this parser.
        /// </summary>
        /// <param name="input">The text to parse.</param>
        /// <param name="source">The name to show the input under in diagnostics, such as a file name, or <see langword="null"/> for none.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// <see cref="Parsed{T}"/> with the parser's value if it succeeded.
        /// Otherwise <see cref="Failed"/> with one error, which holds <paramref name="input"/> so that <see cref="Diagnostics.Render()"/> shows the error against it.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The error is the parse's furthest failure, from layer <c>parser</c>, with spans that are character offsets in <paramref name="input"/> and the source <see cref="SourceId.Unnamed"/>.
        /// A parse that goes deeper than <see cref="ParseOptions.MaxDepth"/>, or runs short of stack, fails with one error where the level would have started, whose message names the cause.
        /// </para>
        /// <para>
        /// The result and the slices in its value refer to <paramref name="input"/> and copy none of it.
        /// Render the result and finish with the slices before reusing the memory.
        /// </para>
        /// <para>
        /// A character parser given memory or a <see langword="char"/> array always runs through this overload, never the one for items of any type, so its errors render against the text.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This parser is null, or its grammar is not valid.</exception>
        [OverloadResolutionPriority(1)]
        public ParseResult<TOut> Parse(ReadOnlyMemory<char> input, string? source = null, ParseOptions? options = null, [CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            Run(Guard.NotNull(parser, parserExpression), input, source, options ?? ParseOptions.Default);
    }

    extension<TIn, TOut>(Parser<TIn, TOut> parser)
    {
        /// <summary>
        /// Parses <paramref name="input"/>, such as the tokens from a lexer, with this parser.
        /// </summary>
        /// <param name="input">The items to parse. An array converts to this type.</param>
        /// <param name="options">The settings for the parse, or <see langword="null"/> for <see cref="ParseOptions.Default"/>.</param>
        /// <param name="parserExpression">
        /// The caller's expression for this parser, used in the error message.
        /// The compiler fills it in; do not pass it.
        /// </param>
        /// <returns>
        /// <see cref="Parsed{T}"/> with the parser's value if it succeeded.
        /// Otherwise <see cref="Failed"/> with one error.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The error is the parse's furthest failure, from layer <c>parser</c>, with spans that are item offsets in <paramref name="input"/> and the source <see cref="SourceId.Unnamed"/>: the span of the third token is 2..3.
        /// The input is not text, so the result holds no source and <see cref="Diagnostics.Render()"/> writes one line per error.
        /// A parse that goes deeper than <see cref="ParseOptions.MaxDepth"/>, or runs short of stack, fails with one error where the level would have started, whose message names the cause.
        /// </para>
        /// </remarks>
        /// <exception cref="GrammarException">This parser is null, or its grammar is not valid.</exception>
        public ParseResult<TOut> Parse(ReadOnlyMemory<TIn> input, ParseOptions? options = null, [CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            TryRun(Guard.NotNull(parser, parserExpression), input, options ?? ParseOptions.Default, out var value, out var error)
            ? new Parsed<TOut>(value)
            : new Failed(new Diagnostics([error]));
    }

    private static ParseError DepthError<TIn>(DepthExceededException exception, ref readonly Cursor<TIn> cursor, FailureLog<TIn> log)
    {
        var position = exception.Position;
        var end = cursor.TryPeekAt(position, out _)
            ? position + 1
            : position;
        var message = exception.Cause == DepthCause.MaxDepth
            ? string.Create(CultureInfo.InvariantCulture, $"nesting exceeds ParseOptions.MaxDepth of {cursor.MaxDepth}")
            : "nesting is too deep for the thread's stack";

        return new ParseError(SpanOf(position, end), Layer, new CustomMessage(message), [], log.ContextAt(position, SpanOf));
    }

    private static ParseError NothingRecorded<TIn>(ref readonly Cursor<TIn> cursor)
    {
        var atItem = cursor.TryPeekAt(0, out var item);
        Unexpected unexpected = atItem
            ? new UnexpectedItem(Display<TIn>.Item(item))
            : new EndOfInput();
        var end = atItem
            ? 1
            : 0;

        return new ParseError(SpanOf(0, end), Layer, unexpected, [], []);
    }

    private static ParseResult<TOut> Run<TOut>(Parser<char, TOut> parser, ReadOnlyMemory<char> input, string? source, ParseOptions options) =>
        TryRun(parser, input, options, out var value, out var error)
        ? new Parsed<TOut>(value)
        : new Failed(new Diagnostics([error], new SourceText(SourceId.Unnamed, source, input)));

    private static SourceSpan SpanOf(long start, long end) =>
        new(SourceId.Unnamed, start, end);

    private static bool TryRun<TIn, TOut>(Parser<TIn, TOut> parser, ReadOnlyMemory<TIn> input, ParseOptions options, out TOut value, [NotNullWhen(false)] out ParseError? error)
    {
        var plan = PlanCache.Get(parser);

        if (TrySucceed(plan, input, options, out value))
        {
            error = null;
            return true;
        }

        var log = SpareLog<TIn>.Take();

        try
        {
            return TryParse(log, plan, input, options, out value, out error);
        }
        finally
        {
            SpareLog<TIn>.Return(log);
        }
    }

    private static bool TrySucceed<TIn, TOut>(Plan<TIn, TOut> plan, ReadOnlyMemory<TIn> input, ParseOptions options, out TOut value)
    {
        var cursor = Cursor<TIn>.FromMemory(input) with { MaxDepth = options.MaxDepth };
        value = default!;

        try
        {
            return plan.TryParse(ref cursor, ref value);
        }
        catch (DepthExceededException)
        {
            return false;
        }
    }

    /// <summary>
    /// Runs <paramref name="plan"/> once over <paramref name="input"/>, recording failures into <paramref name="log"/>, as a failed parse's second run does.
    /// </summary>
    /// <returns>Whether the plan succeeded. If not, <paramref name="error"/> is the furthest failure's error, or the depth error.</returns>
    internal static bool TryParse<TIn, TOut>(FailureLog<TIn> log, Plan<TIn, TOut> plan, ReadOnlyMemory<TIn> input, ParseOptions options, out TOut value, [NotNullWhen(false)] out ParseError? error)
    {
        var cursor = Cursor<TIn>.FromMemory(input) with { Failures = log, MaxDepth = options.MaxDepth };
        value = default!;
        error = null;

        try
        {
            if (plan.TryParse(ref cursor, ref value))
            {
                return true;
            }

            error = log.ToError(in cursor, Layer, SpanOf) ?? NothingRecorded(in cursor);
        }
        catch (DepthExceededException exception)
        {
            error = DepthError(exception, in cursor, log);
        }

        return false;
    }
}

/// <summary>
/// The failure log each thread keeps for parses over items of type <typeparamref name="T"/>, so that a parse allocates none.
/// </summary>
/// <remarks>
/// A parse takes the log and leaves none in its place, so a parse that runs inside another, such as one called from a <c>Select</c> function, makes its own.
/// </remarks>
file static class SpareLog<T>
{
    [ThreadStatic]
    private static FailureLog<T>? _log;

    public static void Return(FailureLog<T> log)
    {
        log.Clear();
        _log = log;
    }

    public static FailureLog<T> Take()
    {
        var log = _log ?? new FailureLog<T>();
        _log = null;

        return log;
    }
}
