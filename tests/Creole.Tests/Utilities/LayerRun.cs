namespace Creole.Tests;

/// <summary>
/// Runs a layer over text, as a pipeline will, and reads what it produces.
/// </summary>
/// <remarks>
/// Character <c>i</c> of the text has the span <c>[i, i + 1)</c>, as a root layer's does.
/// The run records failures, so a failed step reports what it expected.
/// </remarks>
internal static class LayerRun
{
    /// <summary>
    /// The error the layer fails with on <paramref name="text"/>.
    /// </summary>
    public static ParseError Error<T>(Layer<char, T> layer, string text, ParseOptions? options = null) =>
        Assert.Throws<LayerFailedException>(() => Items(layer, text, options: options)).Error;

    /// <summary>
    /// Every item the layer produces from <paramref name="text"/>, with its slot.
    /// </summary>
    public static List<(T Item, StreamSlot Slot)> Items<T>(Layer<char, T> layer, string text, int segmentSize = 1024, ParseOptions? options = null)
    {
        var output = Open(layer, text, out _, segmentSize, options);
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
    public static StreamBuffer<T> Open<T>(Layer<char, T> layer, string text, out ArrayUpstream<char> characters, int segmentSize = 1024, ParseOptions? options = null)
    {
        characters = new ArrayUpstream<char>(text.ToCharArray());

        var input = new StreamBuffer<char>(characters, segmentSize);

        return new StreamBuffer<T>(layer.Open(input, options ?? ParseOptions.Default, recordFailures: true), segmentSize);
    }

    /// <summary>
    /// Runs <paramref name="parser"/> over the items the layer produces from <paramref name="text"/>, as the last layer of a pipeline will, with or without a failure log of its own.
    /// </summary>
    public static (bool Ok, TOut Value, long Position) Run<T, TOut>(Layer<char, T> layer, Parser<T, TOut> parser, string text, int segmentSize = 1024, bool recordFailures = true)
    {
        var cursor = new Cursor<T>(Open(layer, text, out _, segmentSize)) { Failures = recordFailures ? new FailureLog<T>() : null };
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
