namespace Creole.Tests;

/// <summary>
/// Runs a layer over text or bytes, as a pipeline will, and reads what it produces.
/// </summary>
/// <remarks>
/// Input item <c>i</c> has the span <c>[i, i + 1)</c>, as a root layer's character or byte does.
/// The run records failures, so a failed step reports what it expected, unless a method is told not to, as a parse's first run does.
/// </remarks>
internal static class LayerRun
{
    /// <summary>
    /// The error the layer fails with on <paramref name="text"/>.
    /// </summary>
    public static ParseError Error<T>(Layer<char, T> layer, string text, ParseOptions? options = null) =>
        Error(layer, text.ToCharArray(), options);

    /// <summary>
    /// The error the layer fails with on <paramref name="input"/>.
    /// </summary>
    public static ParseError Error<TIn, T>(Layer<TIn, T> layer, TIn[] input, ParseOptions? options = null) =>
        Assert.Throws<LayerFailedException>(() => Items(layer, input, options: options)).Error;

    /// <summary>
    /// The error for the furthest failure <paramref name="parser"/> records over the items the layer produces from <paramref name="text"/>, from layer <c>parser</c>, or null if it recorded none.
    /// </summary>
    public static ParseError? Error<T, TOut>(Layer<char, T> layer, Parser<T, TOut> parser, string text, int segmentSize = 1024) =>
        Error(layer, parser, text.ToCharArray(), segmentSize);

    /// <summary>
    /// The error for the furthest failure <paramref name="parser"/> records over the items the layer produces from <paramref name="input"/>, from layer <c>parser</c>, or null if it recorded none.
    /// </summary>
    public static ParseError? Error<TIn, T, TOut>(Layer<TIn, T> layer, Parser<T, TOut> parser, TIn[] input, int segmentSize = 1024)
    {
        var tokens = Open(layer, input, out _, segmentSize);
        var log = new FailureLog<T>();
        var cursor = new Cursor<T>(tokens) { Failures = log };
        TOut value = default!;

        _ = PlanCache.Get(parser).TryParse(ref cursor, ref value);

        return log.ToError(in cursor, "parser", tokens.SpanOf);
    }

    /// <summary>
    /// Every item the layer produces from <paramref name="text"/>, with its slot.
    /// </summary>
    public static List<(T Item, StreamSlot Slot)> Items<T>(Layer<char, T> layer, string text, int segmentSize = 1024, ParseOptions? options = null, bool recordFailures = true, PipelineRun? run = null) =>
        Items(layer, text.ToCharArray(), segmentSize, options, recordFailures, run);

    /// <summary>
    /// Every item the layer produces from <paramref name="input"/>, with its slot.
    /// </summary>
    public static List<(T Item, StreamSlot Slot)> Items<TIn, T>(Layer<TIn, T> layer, TIn[] input, int segmentSize = 1024, ParseOptions? options = null, bool recordFailures = true, PipelineRun? run = null)
    {
        var output = Open(layer, input, out _, segmentSize, options, recordFailures, run);
        var items = new List<(T, StreamSlot)>();

        for (var position = 0L; output.TryGetSegment(position, horizon: 0, out _, out _); position++)
        {
            items.Add((output.ItemAt(position), output.SlotAt(position)));
        }

        return items;
    }

    /// <summary>
    /// Starts the layer over <paramref name="text"/> and returns the buffer of its items, which fills as it is read.
    /// </summary>
    public static StreamBuffer<T> Open<T>(Layer<char, T> layer, string text, out ArrayUpstream<char> characters, int segmentSize = 1024, ParseOptions? options = null) =>
        Open(layer, text.ToCharArray(), out characters, segmentSize, options);

    /// <summary>
    /// Starts the layer over <paramref name="input"/> and returns the buffer of its items, which fills as it is read.
    /// </summary>
    /// <remarks>
    /// Both buffers are bounded by the options' <see cref="ParseOptions.MaxBufferedItems"/>, as a pipeline's are.
    /// </remarks>
    public static StreamBuffer<T> Open<TIn, T>(Layer<TIn, T> layer, TIn[] input, out ArrayUpstream<TIn> root, int segmentSize = 1024, ParseOptions? options = null, bool recordFailures = true, PipelineRun? run = null)
    {
        options ??= ParseOptions.Default;
        root = new ArrayUpstream<TIn>(input);

        var buffer = new StreamBuffer<TIn>(root, segmentSize, maxBufferedItems: options.MaxBufferedItems);

        return new StreamBuffer<T>(layer.Open(buffer, run ?? new PipelineRun(options, recordFailures)), segmentSize, maxBufferedItems: options.MaxBufferedItems);
    }

    /// <summary>
    /// Runs <paramref name="parser"/> over the items the layer produces from <paramref name="text"/>, as the last layer of a pipeline will, with or without a failure log of its own.
    /// </summary>
    public static (bool Ok, TOut Value, long Position) Run<T, TOut>(Layer<char, T> layer, Parser<T, TOut> parser, string text, int segmentSize = 1024, bool recordFailures = true, ParseOptions? options = null) =>
        Run(layer, parser, text.ToCharArray(), segmentSize, recordFailures, options);

    /// <summary>
    /// Runs <paramref name="parser"/> over the items the layer produces from <paramref name="input"/>, as the last layer of a pipeline will, with or without a failure log of its own.
    /// </summary>
    public static (bool Ok, TOut Value, long Position) Run<TIn, T, TOut>(Layer<TIn, T> layer, Parser<T, TOut> parser, TIn[] input, int segmentSize = 1024, bool recordFailures = true, ParseOptions? options = null)
    {
        var cursor = new Cursor<T>(Open(layer, input, out _, segmentSize, options)) { Failures = recordFailures ? new FailureLog<T>() : null };
        TOut value = default!;
        var ok = PlanCache.Get(parser).TryParse(ref cursor, ref value);

        return (ok, value, cursor.Position);
    }

    /// <summary>
    /// Runs <paramref name="parser"/> over the items <paramref name="layer"/> produces from <paramref name="text"/>, as a pipeline's parser runs, with the mode writer of <paramref name="run"/>.
    /// </summary>
    /// <remarks>
    /// Both buffers hold <paramref name="segmentSize"/> items to a segment, from pools that overwrite each array given back, so a read of released or discarded items shows.
    /// </remarks>
    /// <param name="released">Given the first position the items' buffer still holds when the parse ends, so a test can check that it released what it meant to.</param>
    /// <param name="builder">The builder that lowers <paramref name="parser"/>, so a test can choose its rewrites; by default, the plan a pipeline would run.</param>
    /// <returns>The parser's value, or the error of the parser or of the layer that failed.</returns>
    public static (bool Ok, TOut Value, ParseError? Error) Parse<T, TOut>(Layer<char, T> layer, Parser<T, TOut> parser, string text, int segmentSize = 1024, PipelineRun? run = null, Action<long>? released = null, PlanBuilder? builder = null)
    {
        run ??= new PipelineRun(ParseOptions.Default, recordFailures: true);

        var characters = new StreamBuffer<char>(new ArrayUpstream<char>(text.ToCharArray()), segmentSize, new OverwritingPool<char>('\0'));

        using var items = new StreamBuffer<T>(layer.Open(characters, run), segmentSize, new OverwritingPool<T>(default!));

        var log = new FailureLog<T>();
        var cursor = new Cursor<T>(items) { Failures = log, Modes = run.Writer("parser") };
        TOut value = default!;

        try
        {
            return (builder?.Lower(parser) ?? PlanCache.Get(parser)).TryParse(ref cursor, ref value)
                ? (true, value, null)
                : (false, value, log.ToError(in cursor, "parser", items.SpanOf));
        }
        catch (LayerFailedException exception)
        {
            return (false, value, exception.Error);
        }
        finally
        {
            released?.Invoke(items.Start);
        }
    }

    /// <summary>
    /// Every item the layer produces from <paramref name="text"/>.
    /// </summary>
    public static List<T> Values<T>(Layer<char, T> layer, string text) =>
        [.. Items(layer, text).Select(static entry => entry.Item)];
}
