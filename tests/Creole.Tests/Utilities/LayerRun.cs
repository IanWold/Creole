namespace Creole.Tests;

/// <summary>
/// Runs a layer over text or bytes, as a pipeline will, and reads what it produces.
/// </summary>
/// <remarks>
/// Input item <c>i</c> has the span <c>[i, i + 1)</c>, as a root layer's character or byte does.
/// The run records failures, so a failed step reports what it expected.
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
    public static List<(T Item, StreamSlot Slot)> Items<T>(Layer<char, T> layer, string text, int segmentSize = 1024, ParseOptions? options = null) =>
        Items(layer, text.ToCharArray(), segmentSize, options);

    /// <summary>
    /// Every item the layer produces from <paramref name="input"/>, with its slot.
    /// </summary>
    public static List<(T Item, StreamSlot Slot)> Items<TIn, T>(Layer<TIn, T> layer, TIn[] input, int segmentSize = 1024, ParseOptions? options = null)
    {
        var output = Open(layer, input, out _, segmentSize, options);
        var items = new List<(T, StreamSlot)>();

        for (var position = 0L; output.TryGetSegment(position, out _, out _); position++)
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
    public static StreamBuffer<T> Open<TIn, T>(Layer<TIn, T> layer, TIn[] input, out ArrayUpstream<TIn> root, int segmentSize = 1024, ParseOptions? options = null)
    {
        root = new ArrayUpstream<TIn>(input);

        var buffer = new StreamBuffer<TIn>(root, segmentSize);

        return new StreamBuffer<T>(layer.Open(buffer, options ?? ParseOptions.Default, recordFailures: true), segmentSize);
    }

    /// <summary>
    /// Runs <paramref name="parser"/> over the items the layer produces from <paramref name="text"/>, as the last layer of a pipeline will, with or without a failure log of its own.
    /// </summary>
    public static (bool Ok, TOut Value, long Position) Run<T, TOut>(Layer<char, T> layer, Parser<T, TOut> parser, string text, int segmentSize = 1024, bool recordFailures = true) =>
        Run(layer, parser, text.ToCharArray(), segmentSize, recordFailures);

    /// <summary>
    /// Runs <paramref name="parser"/> over the items the layer produces from <paramref name="input"/>, as the last layer of a pipeline will, with or without a failure log of its own.
    /// </summary>
    public static (bool Ok, TOut Value, long Position) Run<TIn, T, TOut>(Layer<TIn, T> layer, Parser<T, TOut> parser, TIn[] input, int segmentSize = 1024, bool recordFailures = true)
    {
        var cursor = new Cursor<T>(Open(layer, input, out _, segmentSize)) { Failures = recordFailures ? new FailureLog<T>() : null };
        TOut value = default!;
        var ok = PlanCache.Get(parser).TryParse(ref cursor, ref value);

        return (ok, value, cursor.Position);
    }

    /// <summary>
    /// Every item the layer produces from <paramref name="text"/>.
    /// </summary>
    public static List<T> Values<T>(Layer<char, T> layer, string text) =>
        [.. Items(layer, text).Select(static entry => entry.Item)];
}
