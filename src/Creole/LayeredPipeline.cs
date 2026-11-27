using System.Diagnostics.CodeAnalysis;

namespace Creole;

/// <summary>
/// One run of a <see cref="LayeredPipeline{TIn, TItem, TOut}"/>'s parser over the buffer of its last layer's items, one item at a time.
/// </summary>
/// <remarks>
/// Each item runs on a cursor of its own, from where the last item ended.
/// A run that restarts takes every layer's bookmark at each item's start, after the item's failures are forgotten and its modes reset, and gives the cursor the item's start as its floor.
/// When the item starves, the parser's mode writes are undone and its failure log cleared, keeping the diagnostics raised before the bookmark, and then every buffer and layer goes back to its own bookmark.
/// </remarks>
/// <param name="plan">The parser's plan.</param>
/// <param name="items">The last layer's items, which the run disposes.</param>
/// <param name="log">The parser's failure log.</param>
/// <param name="modes">The parser's mode writer.</param>
/// <param name="options">The settings for the parse.</param>
/// <param name="text">Makes the text a failed result holds.</param>
/// <param name="restarts">Whether the run takes bookmarks, so a starved item can run again.</param>
file sealed class LayeredItems<TItem, TOut>(Plan<TItem, TOut> plan, StreamBuffer<TItem> items, FailureLog<TItem> log, ModeWriter modes, ParseOptions options, Func<SourceText?> text, bool restarts) : PipelineItems<TOut>
{
    private int _diagnostics;
    private readonly StreamBuffer<TItem> _items = items;
    private readonly FailureLog<TItem> _log = log;
    private readonly ModeWriter _modes = modes;
    private readonly ParseOptions _options = options;
    private readonly Plan<TItem, TOut> _plan = plan;
    private long _position;
    private readonly bool _restarts = restarts;
    private readonly Func<SourceText?> _text = text;

    private ItemOutcome Restart()
    {
        if (!_restarts)
        {
            throw new InvalidOperationException("The input starved, but this run takes no bookmarks to restart from.");
        }

        _modes.Restart();
        _log.Restart(_diagnostics);
        _items.Restart();

        return ItemOutcome.Starved;
    }

    public override void Dispose() => _items.Dispose();

    public override ItemOutcome Next(out ParseResult<TOut> result)
    {
        _log.ForgetFailure();
        _items.ForgetFailures();
        _modes.Reset(_items, _position);

        if (_restarts)
        {
            _diagnostics = _log.DiagnosticCount;
            _modes.Bookmark();
            _items.Bookmark();
        }

        var cursor = new Cursor<TItem>(_items)
        {
            Failures = _log,
            Floor = _restarts
                ? _position
                : long.MaxValue,
            MaxDepth = _options.MaxDepth,
            Modes = _modes,
        };
        TOut value = default!;
        ParseError error;

        result = default;

        try
        {
            cursor.Reset(_position);

            if (!cursor.TryPeek(out _))
            {
                return cursor.Starved
                    ? Restart()
                    : ItemOutcome.End;
            }

            var parsed = _plan.TryParse(ref cursor, ref value);

            if (cursor.Starved)
            {
                return Restart();
            }

            if (parsed)
            {
                _position = cursor.Position > _position
                    ? cursor.Position
                    : throw NullableRepetition.PipelineItem();

                result = new Parsed<TOut>(value);
                return ItemOutcome.Result;
            }

            error = ParseErrors.Failure(in cursor, _log, cursor.Position, Parsing.Layer, _items.SpanOf);
        }
        catch (LayerFailedException exception)
        {
            error = exception.Error;
        }
        catch (DepthExceededException exception)
        {
            error = ParseErrors.Depth(exception, in cursor, _log, Parsing.Layer, _items.SpanOf);
        }
        catch (BufferLimitExceededException exception)
        {
            error = ParseErrors.BufferLimit(exception, _log, Parsing.Layer, _items.SpanOf);
        }
        catch (LayerStateException exception)
        {
            throw exception.ToGrammarException(Parsing.Layer);
        }

        result = new Failed(Diagnostics.Of(error, _text()));

        return ItemOutcome.Result;
    }
}

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

    internal override PipelineItems<TOut> OpenItems(ICursorSource<TIn> root, ParseOptions options, Func<SourceText?> text, bool restarts, List<object>? logs = null)
    {
        var plan = PlanCache.Get(Parser);

        var run = new PipelineRun(options, recordFailures: true) { Logs = logs };

        var items = new StreamBuffer<TItem>(Layers.Open(root, run), maxBufferedItems: options.MaxBufferedItems);

        return new LayeredItems<TItem, TOut>(plan, items, run.Log<TItem>()!, run.Writer(ParserLayer), options, text, restarts);
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
