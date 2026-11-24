using System.Text;
using CsCheck;
using static Creole.CharGrammar;

namespace Creole.Tests;

public class Utf8LayerTests
{
    private static readonly byte[] _byteOrderMark = [0xEF, 0xBB, 0xBF];

    private static readonly Gen<Rune> _runes = Gen.OneOf(
        Gen.Int[0x20, 0x7E],
        Gen.Bool.Select(static carriageReturn =>
            carriageReturn
                ? (int)'\r'
                : '\n'
        ),
        Gen.Int[0x80, 0x7FF],
        Gen.Int[0x800, 0xD7FF],
        Gen.Int[0xE000, 0xFFFF],
        Gen.Int[0x10000, 0x10FFFF]
    ).Select(static value => new Rune(value));

    private static readonly Gen<byte[]> _fragments = Gen.OneOf(
        Gen.Byte.Select(static value => new[] { value }),
        _runes.Select(static rune => Encoding.UTF8.GetBytes(rune.ToString())),
        _runes.Select(static rune => Encoding.UTF8.GetBytes(rune.ToString())[..^1])
    );

    private static readonly UTF8Encoding _strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private static (List<(char Item, StreamSlot Slot)> Items, ParseError? Error) Decode(byte[] bytes, int segmentSize)
    {
        var output = LayerRun.Open(Layer.Utf8(), bytes, out _, segmentSize);
        var items = new List<(char, StreamSlot)>();

        try
        {
            for (var position = 0L; output.TryGetSegment(position, out _, out _); position++)
            {
                items.Add((output.ItemAt(position), output.SlotAt(position)));
            }
        }
        catch (LayerFailedException exception)
        {
            return (items, exception.Error);
        }

        return (items, null);
    }

    /// <summary>
    /// The text a strict decoder reads from valid <paramref name="bytes"/>, without a leading byte-order mark, and the byte its first character starts at.
    /// </summary>
    private static string Decoded(byte[] bytes, out long firstByte)
    {
        var text = _strict.GetString(bytes);

        firstByte = text.StartsWith('\uFEFF')
            ? _byteOrderMark.Length
            : 0;

        return text.StartsWith('\uFEFF')
            ? text[1..]
            : text;
    }

    /// <summary>
    /// The items and slots the layer should produce for <paramref name="text"/>, encoded from <paramref name="firstByte"/>.
    /// </summary>
    private static List<(char Item, StreamSlot Slot)> Expected(string text, long firstByte)
    {
        var expected = new List<(char, StreamSlot)>();
        var bytes = firstByte;
        var chars = 0L;

        foreach (var rune in text.EnumerateRunes())
        {
            var units = rune.ToString();

            for (var group = 0; group < units.Length; group++)
            {
                expected.Add((units[group], Slot(bytes, bytes + rune.Utf8SequenceLength, chars, chars + units.Length, group)));
            }

            bytes += rune.Utf8SequenceLength;
            chars += units.Length;
        }

        return expected;
    }

    private static StreamSlot Slot(long start, long end, long charStart, long charEnd, int group = 0) =>
        new(new SourceSpan(SourceId.Unnamed, start, end, charStart, charEnd), start, StateBefore: null, group);

    [Fact]
    public void ALeadingByteOrderMarkIsSkippedAndALaterOneIsACharacter() =>
        Assert.Equal<(char, StreamSlot)>(
            [('a', Slot(3, 4, 0, 1)), ('\uFEFF', Slot(4, 7, 1, 2))],
            LayerRun.Items(Layer.Utf8(), (byte[])[0xEF, 0xBB, 0xBF, 0x61, 0xEF, 0xBB, 0xBF])
        );

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF })]
    public void AnEmptyInputOrAByteOrderMarkAloneProducesNothing(byte[] bytes) =>
        Assert.Empty(LayerRun.Items(Layer.Utf8(), bytes));

    [Fact]
    public void AnInvalidByteReadsAsADecoderError() =>
        Assert.Equal("error[decoder] at 3..3: invalid UTF-8 byte FF", LayerRun.Error(Layer.Utf8(), (byte[])[0x61, 0x0A, 0x62, 0xFF]).ToString());

    [Fact]
    public void DecodingStopsAtTheFirstInvalidSequenceAsAStrictDecoderDoes() =>
        Gen.Select(_fragments.Array[0, 12], Gen.Int[1, 5]).Sample(
            static (fragments, segmentSize) =>
            {
                byte[] bytes = [.. fragments.SelectMany(static fragment => fragment)];
                var (items, error) = Decode(bytes, segmentSize);

                try
                {
                    var text = Decoded(bytes, out var firstByte);

                    return error is null && items.SequenceEqual(Expected(text, firstByte));
                }
                catch (DecoderFallbackException exception)
                {
                    var text = Decoded(bytes[..exception.Index], out var firstByte);
                    var invalid = new SourceSpan(SourceId.Unnamed, exception.Index, exception.Index + exception.BytesUnknown!.Length, text.Length, text.Length);

                    return items.SequenceEqual(Expected(text, firstByte)) && error is { Layer: "decoder" } && error.Span == invalid;
                }
            },
            iter: 2000
        );

    [Fact]
    public void EachCharacterSpansItsBytesAndItsPlaceInTheDecodedText() =>
        Assert.Equal<(char, StreamSlot)>(
            [
                ('a', Slot(0, 1, 0, 1)),
                ('é', Slot(1, 3, 1, 2)),
                ('€', Slot(3, 6, 2, 3)),
                ('\uD83D', Slot(6, 10, 3, 5, group: 0)),
                ('\uDE00', Slot(6, 10, 3, 5, group: 1)),
                ('b', Slot(10, 11, 5, 6)),
            ],
            LayerRun.Items(Layer.Utf8(), Encoding.UTF8.GetBytes("aé€\U0001F600b"))
        );

    [Fact]
    public void EveryCharacterOfValidUtf8HasTheSpanOfItsBytesAndOfItsPlaceInTheText() =>
        Gen.Select(_runes.Array[0, 30], Gen.Bool, Gen.Int[1, 5]).Sample(
            static (runes, byteOrderMark, segmentSize) =>
            {
                var encoded = Encoding.UTF8.GetBytes(string.Concat(runes.Select(static rune => rune.ToString())));

                var bytes = byteOrderMark
                    ? [.. _byteOrderMark, .. encoded]
                    : encoded;

                var text = Decoded(bytes, out var firstByte);

                return Decode(bytes, segmentSize) is { Error: null } decoded && decoded.Items.SequenceEqual(Expected(text, firstByte));
            },
            iter: 1000
        );

    [Theory]
    [InlineData(new byte[] { 0x61, 0x0A, 0x62, 0xFF }, 3, 4, 3, "invalid UTF-8 byte FF")]
    [InlineData(new byte[] { 0x80 }, 0, 1, 0, "invalid UTF-8 byte 80")]
    [InlineData(new byte[] { 0xC3, 0xA9, 0xE2, 0x82, 0x78 }, 2, 4, 1, "invalid UTF-8 bytes E2 82")]
    [InlineData(new byte[] { 0x61, 0xE2, 0x82 }, 1, 3, 1, "invalid UTF-8 bytes E2 82")]
    [InlineData(new byte[] { 0xC0, 0x80 }, 0, 1, 0, "invalid UTF-8 byte C0")]
    [InlineData(new byte[] { 0x61, 0xED, 0xA0, 0x80 }, 1, 2, 1, "invalid UTF-8 byte ED")]
    [InlineData(new byte[] { 0xF4, 0x90, 0x80, 0x80 }, 0, 1, 0, "invalid UTF-8 byte F4")]
    [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 0xF0, 0x9F, 0x98 }, 3, 6, 0, "invalid UTF-8 bytes F0 9F 98")]
    public void InvalidBytesEndTheParseWithADecoderErrorOverThem(byte[] bytes, long start, long end, long chars, string message) =>
        Assert.Equal(
            new ParseError(new SourceSpan(SourceId.Unnamed, start, end, chars, chars), "decoder", new CustomMessage(message), [], []),
            LayerRun.Error(Layer.Utf8(), bytes)
        );

    [Fact]
    public void LayersAboveKeepBothUnits() =>
        Assert.Equal(
            [new SourceSpan(SourceId.Unnamed, 3, 9, 0, 5), new SourceSpan(SourceId.Unnamed, 10, 16, 6, 11)],
            LayerRun.Items(
                Layer.Utf8().Then(Layer.Tokens(Letter.AtLeastOnce().Text(), skip: Whitespace.SkipMany())),
                [.. _byteOrderMark, .. Encoding.UTF8.GetBytes("héllo wörld")]
            ).Select(static entry => entry.Slot.Span)
        );
}
