using System.Diagnostics.CodeAnalysis;

namespace Creole;

/// <summary>
/// A pipeline: layers that turn the input into items, and a parser that reads those items, such as a lexer and a parser over its tokens.
/// </summary>
/// <typeparam name="TIn">The type of item the pipeline reads, such as <see cref="char"/>.</typeparam>
/// <typeparam name="TOut">The type of the parser's value.</typeparam>
/// <remarks>
/// <para>
/// Make a pipeline by ending a layer in a parser, as in <c>Pipeline.From(lexer).Then(parser)</c>, and run it with the <c>Parse</c> methods of <see cref="Pipeline"/>.
/// </para>
/// <para>
/// A pipeline is data, as its layers and parser are: building one runs nothing.
/// It is immutable, so one pipeline can be run from several threads at once. Each run has its own buffers and state.
/// </para>
/// <para>
/// Only Creole defines pipeline types.
/// </para>
/// </remarks>
public abstract class Pipeline<TIn, TOut>
{
    /// <summary>
    /// Whether the first layer is <see cref="Layer.Utf8"/>, so the input is UTF-8 that decodes to the text a result can hold.
    /// </summary>
    internal abstract bool DecodesUtf8 { get; }

    internal Pipeline() { }

    /// <summary>
    /// Opens a run of the parser over the items of <paramref name="root"/>, with new buffers, which runs one item each time it is asked.
    /// </summary>
    /// <param name="root">The first layer's input.</param>
    /// <param name="options">The settings for the parse, whose limits apply to each layer and to the parser.</param>
    /// <param name="text">Makes the text a failed result holds for rendering, or null to hold none. It is called at most once.</param>
    /// <param name="restarts">Whether the run bookmarks every layer at each item's start, so that an item that starves can run again.</param>
    /// <param name="logs">A list that gets each layer's failure log, in the order the layers open, and the parser's last, for tests; or null.</param>
    internal abstract PipelineItems<TOut> OpenItems(ICursorSource<TIn> root, ParseOptions options, Func<SourceText?> text, bool restarts, List<object>? logs = null);

    /// <summary>
    /// Runs the parser at the start of the input and again after each item until no item remains, with every layer recording failures, and returns each item's result as the caller asks for it.
    /// </summary>
    /// <param name="openRoot">Makes the first layer's input, once for each enumeration.</param>
    /// <param name="options">The settings for the parse, whose depth limit applies to each layer and to the parser.</param>
    /// <param name="text">Makes the text a failed result holds for rendering, or null to hold none. It is called at most once for each enumeration.</param>
    /// <returns>Each item's result. A failed item is the last.</returns>
    /// <remarks>
    /// Each item starts with no furthest failure in any layer.
    /// The buffers of an enumeration return their segments to their pools when it ends or is disposed.
    /// </remarks>
    internal IEnumerable<ParseResult<TOut>> ParseMany(Func<ICursorSource<TIn>> openRoot, ParseOptions options, Func<SourceText?> text)
    {
        using var items = OpenItems(openRoot(), options, text, restarts: false);

        while (items.Next(out var result) == ItemOutcome.Result)
        {
            yield return result;

            if (result is Failed)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Runs the pipeline once over <paramref name="root"/>, with new buffers.
    /// </summary>
    /// <param name="root">The first layer's input.</param>
    /// <param name="options">The settings for the parse, whose depth limit applies to each layer and to the parser.</param>
    /// <param name="recordFailures">Whether every layer and the parser keep a failure log, so that the error says what was expected.</param>
    /// <param name="value">The parser's value, if it succeeded.</param>
    /// <param name="error">The error that ended the run, if it failed, with spans in the source.</param>
    /// <returns>Whether the parser succeeded.</returns>
    /// <remarks>
    /// Every buffer the run creates returns its segments to their pools before this returns, or throws.
    /// </remarks>
    internal abstract bool TryParse(ICursorSource<TIn> root, ParseOptions options, bool recordFailures, out TOut value, [NotNullWhen(false)] out ParseError? error);
}
