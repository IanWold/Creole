using System.Globalization;

namespace Creole;

/// <summary>
/// The error a layer's parse reports when it fails: its furthest failure, the error for a parse that recorded nothing, or the depth error.
/// </summary>
/// <remarks>
/// The parser layer and every other layer build these the same way, each with its own name and its own map from positions to spans.
/// </remarks>
internal static class ParseErrors
{
    /// <summary>
    /// The error for a parse that failed at <paramref name="position"/> without recording a failure, because it kept no failure log.
    /// </summary>
    /// <param name="cursor">A cursor over the layer's input.</param>
    /// <param name="position">Where the parse failed, such as the start of the input or of a step.</param>
    /// <param name="layer">The name of the layer.</param>
    /// <param name="spanOf">The source span of the layer's input from a start position up to an end position.</param>
    /// <returns>An error that the item at <paramref name="position"/>, or the end of the input, was unexpected.</returns>
    private static ParseError NothingRecorded<T>(ref readonly Cursor<T> cursor, long position, string layer, Func<long, long, SourceSpan> spanOf)
    {
        var atItem = cursor.TryPeekAt(position, out var item);

        Unexpected unexpected = atItem
            ? new UnexpectedItem(Display<T>.Item(item))
            : new EndOfInput();

        var end = atItem
            ? position + 1
            : position;

        return new ParseError(spanOf(position, end), layer, unexpected, [], []);
    }

    /// <summary>
    /// The error for a parse that went deeper than its cursor's <see cref="Cursor{T}.MaxDepth"/> or ran short of stack.
    /// </summary>
    /// <param name="exception">The exception that ended the parse.</param>
    /// <param name="cursor">The cursor of the layer whose parse went too deep.</param>
    /// <param name="failures">The layer's failure log, which gives the context lines, or null if it keeps none.</param>
    /// <param name="layer">The name of the layer.</param>
    /// <param name="spanOf">The source span of the layer's input from a start position up to an end position.</param>
    /// <returns>An error at the item where the level would have started, whose message names the cause.</returns>
    public static ParseError Depth<T>(DepthExceededException exception, ref readonly Cursor<T> cursor, FailureLog<T>? failures, string layer, Func<long, long, SourceSpan> spanOf)
    {
        var position = exception.Position;

        var end = cursor.TryPeekAt(position, out _)
            ? position + 1
            : position;

        var message = exception.Cause == DepthCause.MaxDepth
            ? string.Create(CultureInfo.InvariantCulture, $"nesting exceeds ParseOptions.MaxDepth of {cursor.MaxDepth}")
            : "nesting is too deep for the thread's stack";

        var context = failures?.ContextAt(position, spanOf) ?? [];

        return new ParseError(spanOf(position, end), layer, new CustomMessage(message), [], context);
    }

    /// <summary>
    /// The error for the furthest failure in <paramref name="failures"/>, or, if it holds none or is null, the error that the item at <paramref name="position"/> was unexpected.
    /// </summary>
    /// <param name="cursor">A cursor over the layer's input, which still holds every item the failures need.</param>
    /// <param name="failures">The layer's failure log, or null if it keeps none.</param>
    /// <param name="position">Where the parse failed if no failure was recorded, such as the start of the input or of a step.</param>
    /// <param name="layer">The name of the layer.</param>
    /// <param name="spanOf">The source span of the layer's input from a start position up to an end position.</param>
    /// <returns>The error.</returns>
    public static ParseError Failure<T>(ref readonly Cursor<T> cursor, FailureLog<T>? failures, long position, string layer, Func<long, long, SourceSpan> spanOf) =>
        failures?.ToError(in cursor, layer, spanOf) ?? NothingRecorded(in cursor, position, layer, spanOf);
}
