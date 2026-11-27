using System.Diagnostics.CodeAnalysis;

namespace Creole;

/// <summary>
/// Runs <see cref="Layers"/> over the input and <see cref="Parser"/> over their items. Backs <c>Then</c> with a parser.
/// </summary>
/// <remarks>
/// The parser reads a buffer of the last layer's items, so it can rewind over them without the layers running again.
/// Every buffer of a run is bounded by its <see cref="ParseOptions.MaxBufferedItems"/>, and the parser reports its own buffer's limit as its error.
/// The parser's mode writer opens after every layer, so it may change any channel its layers read.
/// Under <c>ParseMany</c>, each item starts the channels the parser writes at their initial modes.
/// </remarks>
internal sealed class LayeredPipeline<TIn, TItem, TOut>(Layer<TIn, TItem> layers, Parser<TItem, TOut> parser) : Pipeline<TIn, TOut>
{
    private const string ParserLayer = "parser";

    internal override bool DecodesUtf8 => Layers.DecodesUtf8;

    public Layer<TIn, TItem> Layers { get; } = layers;

    public Parser<TItem, TOut> Parser { get; } = parser;

    /// <summary>
    /// Parses the item that starts at <paramref name="start"/>, with every layer's furthest failure forgotten first, and the channels the parser writes back at their initial modes.
    /// </summary>
    /// <returns><see langword="false"/> if no item remains at <paramref name="start"/>.</returns>
    private static bool TryNext(Plan<TItem, TOut> plan, StreamBuffer<TItem> items, FailureLog<TItem> log, ModeWriter modes, ParseOptions options, Func<SourceText?> text, long start, out ParseResult<TOut> result, out long end)
    {
        log.ForgetFailure();
        items.ForgetFailures();
        modes.Reset(items, start);

        var cursor = new Cursor<TItem>(items) { Failures = log, MaxDepth = options.MaxDepth, Modes = modes };
        TOut value = default!;
        ParseError error;

        end = start;

        try
        {
            cursor.Reset(start);

            if (!cursor.TryPeek(out _))
            {
                result = default;
                return false;
            }

            if (plan.TryParse(ref cursor, ref value))
            {
                end = cursor.Position > start
                    ? cursor.Position
                    : throw NullableRepetition.PipelineItem();

                result = new Parsed<TOut>(value);
                return true;
            }

            error = ParseErrors.Failure(in cursor, log, cursor.Position, ParserLayer, items.SpanOf);
        }
        catch (LayerFailedException exception)
        {
            error = exception.Error;
        }
        catch (DepthExceededException exception)
        {
            error = ParseErrors.Depth(exception, in cursor, log, ParserLayer, items.SpanOf);
        }
        catch (BufferLimitExceededException exception)
        {
            error = ParseErrors.BufferLimit(exception, log, ParserLayer, items.SpanOf);
        }
        catch (LayerStateException exception)
        {
            throw exception.ToGrammarException(ParserLayer);
        }

        result = new Failed(Diagnostics.Of(error, text()));

        return true;
    }

    internal override IEnumerable<ParseResult<TOut>> ParseMany(Func<ICursorSource<TIn>> openRoot, ParseOptions options, Func<SourceText?> text)
    {
        var plan = PlanCache.Get(Parser);

        var run = new PipelineRun(options, recordFailures: true);

        using var items = new StreamBuffer<TItem>(Layers.Open(openRoot(), run), maxBufferedItems: options.MaxBufferedItems);

        var log = new FailureLog<TItem>();
        var modes = run.Writer(ParserLayer);

        for (var position = 0L; TryNext(plan, items, log, modes, options, text, position, out var result, out var end); position = end)
        {
            yield return result;

            if (result is Failed)
            {
                yield break;
            }
        }
    }

    internal override bool TryParse(ICursorSource<TIn> root, ParseOptions options, bool recordFailures, out TOut value, [NotNullWhen(false)] out ParseError? error)
    {
        var plan = PlanCache.Get(Parser);

        var run = new PipelineRun(options, recordFailures);

        using var items = new StreamBuffer<TItem>(Layers.Open(root, run), maxBufferedItems: options.MaxBufferedItems);

        var log = recordFailures
            ? new FailureLog<TItem>()
            : null;

        var cursor = new Cursor<TItem>(items) { Failures = log, MaxDepth = options.MaxDepth, Modes = run.Writer(ParserLayer) };
        value = default!;
        error = null;

        try
        {
            if (plan.TryParse(ref cursor, ref value))
            {
                return true;
            }

            error = ParseErrors.Failure(in cursor, log, cursor.Position, ParserLayer, items.SpanOf);
        }
        catch (LayerFailedException exception)
        {
            error = exception.Error;
        }
        catch (DepthExceededException exception)
        {
            error = ParseErrors.Depth(exception, in cursor, log, ParserLayer, items.SpanOf);
        }
        catch (BufferLimitExceededException exception)
        {
            error = ParseErrors.BufferLimit(exception, log, ParserLayer, items.SpanOf);
        }
        catch (LayerStateException exception)
        {
            throw exception.ToGrammarException(ParserLayer);
        }

        return false;
    }
}
