using System.Globalization;
using System.Runtime.CompilerServices;

namespace Creole;

/// <summary>
/// Runs a parser on text, called as a method on the parser: <c>parser.Parse(text)</c>.
/// </summary>
/// <remarks>
/// <para>
/// A parse succeeds when the parser does, even if input remains after it. To require the whole input, end the grammar with <c>End</c>, as in <c>Value.Before(End)</c>.
/// </para>
/// <para>
/// A parser can be run from several threads at once. Each parse keeps its own state.
/// </para>
/// </remarks>
public static class Parsing
{
    private const string Layer = "parser";

    [ThreadStatic]
    private static FailureLog<char>? _spareLog;

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
        /// </remarks>
        /// <exception cref="GrammarException">This parser is null, or its grammar is not valid.</exception>
        public ParseResult<TOut> Parse(ReadOnlyMemory<char> input, string? source = null, ParseOptions? options = null, [CallerArgumentExpression(nameof(parser))] string parserExpression = "") =>
            Run(Guard.NotNull(parser, parserExpression), input, source, options ?? ParseOptions.Default);
    }

    private static ParseError DepthError(DepthExceededException exception, ref readonly Cursor<char> cursor, FailureLog<char> log)
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

    private static ParseError NothingRecorded(ref readonly Cursor<char> cursor)
    {
        var atItem = cursor.TryPeekAt(0, out var item);
        Unexpected unexpected = atItem
            ? new UnexpectedItem(Display<char>.Item(item))
            : new EndOfInput();
        var end = atItem
            ? 1
            : 0;

        return new ParseError(SpanOf(0, end), Layer, unexpected, [], []);
    }

    private static ParseResult<TOut> ParseWith<TOut>(FailureLog<char> log, Parser<char, TOut> parser, ReadOnlyMemory<char> input, string? source, ParseOptions options)
    {
        var plan = PlanCache.Get(parser);
        var cursor = Cursor<char>.FromMemory(input) with { Failures = log, MaxDepth = options.MaxDepth };
        TOut value = default!;
        ParseError error;

        try
        {
            if (plan.TryParse(ref cursor, ref value))
            {
                return new Parsed<TOut>(value);
            }

            error = log.ToError(in cursor, Layer, SpanOf) ?? NothingRecorded(in cursor);
        }
        catch (DepthExceededException exception)
        {
            error = DepthError(exception, in cursor, log);
        }

        return new Failed(new Diagnostics([error], new SourceText(SourceId.Unnamed, source, input)));
    }

    private static ParseResult<TOut> Run<TOut>(Parser<char, TOut> parser, ReadOnlyMemory<char> input, string? source, ParseOptions options)
    {
        var log = _spareLog ?? new FailureLog<char>();
        _spareLog = null;

        try
        {
            return ParseWith(log, parser, input, source, options);
        }
        finally
        {
            log.Clear();
            _spareLog = log;
        }
    }

    private static SourceSpan SpanOf(long start, long end) =>
        new(SourceId.Unnamed, start, end);
}
