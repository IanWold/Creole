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

    private static readonly bool[] _pinFirst = [false, true];

    private static readonly int[] _segmentSizes = [1, 2];

    private static readonly Layer<char, char> _select = Layer.Select<char, char>(static c => char.ToUpperInvariant(c));

    private static readonly Layer<char, int> _tokens = Layer.Tokens(Letter.SkipAtLeastOnce().Select(static _ => 1), Char(' ').SkipMany());

    private static readonly Layer<char, int> _transduce = Layer.Transduce(
        0,
        Letter.SkipAtLeastOnce().Select(static _ => Emit.One(1)).Or(Char(' ').SkipAtLeastOnce().Select(static _ => default(Emit<int>)))
    );

    private static readonly Layer<char, char> _where = Layer.Where<char>(static c => c != ' ');

    private static string? Difference<T>(string name, Layer<char, T> layer, int segmentSize, bool pinFirst)
    {
        var expected = LayerRun.Items(layer, Text).Select(static entry => entry.Slot.Span).ToList();
        var characters = new StreamBuffer<char>(new ArrayUpstream<char>(Text.ToCharArray()), segmentSize, new OverwritingPool<char>('#'));
        var output = new StreamBuffer<T>(layer.Open(characters, ParseOptions.Default, recordFailures: true), segmentSize, new OverwritingPool<T>(default!));
        var reader = new Cursor<T>(output);
        var floor = pinFirst
            ? reader.Pin()
            : reader.Floor;
        var spans = new List<SourceSpan>();
        var label = $"{name} in segments of {segmentSize}{(pinFirst ? ", first item pinned" : "")}";

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
        from size in _segmentSizes
        from pinFirst in _pinFirst
        select Difference(name, layer, size, pinFirst) into difference
        where difference is not null
        select difference;

    [Fact]
    public void EveryProducingLayerKeepsTheInputOfItsRunningStepAndOfEveryItemItsReaderHolds()
    {
        string[] differences = [
            .. Differences("Tokens", _tokens),
            .. Differences("Transduce", _transduce),
            .. Differences("Select", _select),
            .. Differences("Where", _where),
        ];

        Assert.Empty(differences);
    }
}
