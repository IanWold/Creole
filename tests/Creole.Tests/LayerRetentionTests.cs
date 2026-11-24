using System.Text;
using static Creole.CharGrammar;

namespace Creole.Tests;

/// <summary>
/// Producing layers over streamed input that is released as the layer above reads, against the same layers over input that is never released.
/// </summary>
/// <remarks>
/// Segments hold one or two items, and released arrays are overwritten before they are reused.
/// After each read, every item the layer's output still holds must still have the input its step began at.
/// A reader that pins the first item holds every item, so the layer must hold its input from the first item's step on.
/// </remarks>
public class LayerRetentionTests
{
    private const string Text = "  abc de   fghij k  lm nopq r stu   vw xyz  ";

    private static readonly byte[] _encoded = Encoding.UTF8.GetBytes("  ab\u00E9 \u20AC de   f\U0001F600hij k  lm \u00F6pq r stu   vw xyz  ");

    private static readonly bool[] _pinFirst = [false, true];

    private static readonly int[] _segmentSizes = [1, 2];

    private static readonly Layer<char, char> _select = Layer.Select<char, char>(static c => char.ToUpperInvariant(c));

    private static readonly Layer<char, int> _tokens = Layer.Tokens(Letter.SkipAtLeastOnce().Select(static _ => 1), Char(' ').SkipMany());

    private static readonly Layer<char, int> _transduce = Layer.Transduce(0, Letter.SkipAtLeastOnce().Select(static _ => Emit.One(1)).Or(Char(' ').SkipAtLeastOnce().Select(static _ => default(Emit<int>))));

    private static readonly Layer<char, char> _where = Layer.Where<char>(static c => c != ' ');

    private static string? Difference<TIn, T>(string name, Layer<TIn, T> layer, TIn[] input, TIn filler, int segmentSize, bool pinFirst)
    {
        var expected = LayerRun.Items(layer, input).Select(static entry => entry.Slot.Span).ToList();
        var characters = new StreamBuffer<TIn>(new ArrayUpstream<TIn>(input), segmentSize, new OverwritingPool<TIn>(filler));
        var output = new StreamBuffer<T>(layer.Open(characters, ParseOptions.Default, recordFailures: true), segmentSize, new OverwritingPool<T>(default!));
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

                for (var position = output.Start; position < output.Filled; position++)
                {
                    if (output.SlotAt(position).StepStart < characters.Start)
                    {
                        return $"{label}: item {position} began at {output.SlotAt(position).StepStart}, below the input's start {characters.Start}";
                    }
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
            : !pinFirst && characters.Start == 0
                ? $"{label}: no input was released"
                : null;
    }

    private static IEnumerable<string> Differences<T>(string name, Layer<char, T> layer) =>
        Differences(name, layer, Text.ToCharArray(), '#');

    private static IEnumerable<string> Differences<TIn, T>(string name, Layer<TIn, T> layer, TIn[] input, TIn filler) =>
        from size in _segmentSizes
        from pinFirst in _pinFirst
        select Difference(name, layer, input, filler, size, pinFirst) into difference
        where difference is not null
        select difference;

    [Fact]
    public void EveryProducingLayerKeepsTheInputOfItsRunningStepAndOfEveryItemItsReaderHolds() =>
        Assert.Empty((string[])[
            .. Differences("Tokens", _tokens),
            .. Differences("Transduce", _transduce),
            .. Differences("Select", _select),
            .. Differences("Where", _where),
            .. Differences("Utf8", Layer.Utf8(), _encoded, (byte)'#'),
        ]);
}
