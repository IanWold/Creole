using System.Text;
using static Creole.CharGrammar;
using static Creole.Grammar<char>;

namespace Creole.Tests;

/// <summary>
/// Producing layers over a layer that produces their input, from streamed input that is released as the reader reads, against the same layers over input that is never released.
/// </summary>
/// <remarks>
/// <para>
/// Segments hold one or two items, and released arrays are overwritten before they are reused.
/// After each read, every output at or above the reader's horizon must still have the input its step began at.
/// So must every item the layer reads at or above the layer's own pin, the step start of the output at the reader's horizon.
/// A reader that pins the first item can go back to every item, so both layers must hold their input from the first item's step on.
/// </para>
/// <para>
/// A reader that states a horizon above its true one lets the layer below release input that an item it can go back to still needs, and fails here.
/// </para>
/// </remarks>
public class LayerRetentionTests
{
    private const string Text = "  abc de   fghij k  lm nopq r stu   vw xyz  ";

    private static readonly Layer<byte, byte> _bytes = Layer.Transduce(0, Grammar<byte>.Any.Select(static b => Emit.One(b)));

    private static readonly Layer<char, char> _characters = Layer.Transduce(0, Any.Select(static c => Emit.One(c)));

    private static readonly byte[] _encoded = Encoding.UTF8.GetBytes("  abé € de   f\U0001F600hij k  lm öpq r stu   vw xyz  ");

    private static readonly bool[] _pinFirst = [false, true];

    private static readonly int[] _segmentSizes = [1, 2];

    private static readonly Layer<char, char> _select = Layer.Select<char, char>(static c => char.ToUpperInvariant(c));

    private static readonly Layer<char, int> _tokens = Layer.Tokens(Letter.SkipAtLeastOnce().Select(static _ => 1), Char(' ').SkipMany());

    private static readonly Layer<char, int> _transduce = Layer.Transduce(0, Letter.SkipAtLeastOnce().Select(static _ => Emit.One(1)).Or(Char(' ').SkipAtLeastOnce().Select(static _ => default(Emit<int>))));

    private static readonly Layer<char, char> _where = Layer.Where<char>(static c => c != ' ');

    private static string? Difference<TIn, TMiddle, T>(string name, Layer<TIn, TMiddle> below, Layer<TMiddle, T> layer, TIn[] input, TIn filler, TMiddle middleFiller, int segmentSize, bool pinFirst)
    {
        var expected = LayerRun.Items(below.Then(layer), input).Select(static entry => entry.Slot.Span).ToList();
        var root = new StreamBuffer<TIn>(new ArrayUpstream<TIn>(input), segmentSize, new OverwritingPool<TIn>(filler));
        var middle = new StreamBuffer<TMiddle>(below.Open(root, ParseOptions.Default, recordFailures: true), segmentSize, new OverwritingPool<TMiddle>(middleFiller));
        var output = new StreamBuffer<T>(layer.Open(middle, ParseOptions.Default, recordFailures: true), segmentSize, new OverwritingPool<T>(default!));
        var reader = new Cursor<T>(output);
        var spans = new List<SourceSpan>();
        var label = $"{name} in segments of {segmentSize}{(pinFirst ? ", first item pinned" : "")}";
        var floor = pinFirst
            ? reader.Pin()
            : reader.Floor;

        try
        {
            while (reader.TryRead(out _))
            {
                spans.Add(output.SpanOf(reader.Position - 1, reader.Position));

                var horizon = Math.Min(reader.Position, reader.Floor);

                if (Unheld(output, horizon, middle) is { } item)
                {
                    return $"{label}: output {item} began at {output.SlotAt(item).StepStart}, below the start {middle.Start} of the layer's input";
                }

                if (horizon < output.Filled && Unheld(middle, output.SlotAt(horizon).StepStart, root) is { } read)
                {
                    return $"{label}: input item {read} began at {middle.SlotAt(read).StepStart}, below the start {root.Start} of the input below";
                }
            }
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return $"{label}: {exception.Message}";
        }

        reader.Unpin(floor);

        return !spans.SequenceEqual(expected)
            ? $"{label}: spans [{string.Join(", ", spans)}], expected [{string.Join(", ", expected)}]"
            : !pinFirst && (root.Start == 0 || middle.Start == 0)
                ? $"{label}: no input was released at one boundary"
                : null;
    }

    private static IEnumerable<string> Differences<T>(string name, Layer<char, T> layer) =>
        Differences(name, _characters, layer, Text.ToCharArray(), '#', '#');

    private static IEnumerable<string> Differences<TIn, TMiddle, T>(string name, Layer<TIn, TMiddle> below, Layer<TMiddle, T> layer, TIn[] input, TIn filler, TMiddle middleFiller) =>
        from size in _segmentSizes
        from pinFirst in _pinFirst
        select Difference(name, below, layer, input, filler, middleFiller, size, pinFirst) into difference
        where difference is not null
        select difference;

    private static long? Unheld<T, TIn>(StreamBuffer<T> buffer, long from, StreamBuffer<TIn> input)
    {
        for (var position = from; position < buffer.Filled; position++)
        {
            if (buffer.SlotAt(position).StepStart < input.Start)
            {
                return position;
            }
        }

        return null;
    }

    [Fact]
    public void EveryProducingLayerKeepsTheInputOfEveryItemItsReaderCanStillGoBackTo() =>
        Assert.Empty((string[])[
            .. Differences("Tokens", _tokens),
            .. Differences("Transduce", _transduce),
            .. Differences("Select", _select),
            .. Differences("Where", _where),
            .. Differences("Utf8", _bytes, Layer.Utf8(), _encoded, (byte)'#', (byte)'#'),
        ]);
}
