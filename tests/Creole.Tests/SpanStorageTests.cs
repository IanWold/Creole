using System.Runtime.CompilerServices;
using CsCheck;

namespace Creole.Tests;

file sealed class SlotUpstream(StreamSlot[] slots) : IUpstream<int>
{
    private readonly StreamSlot[] _slots = slots;
    private int _next;

    public SourceSpan InputEnd =>
        _slots is [.., var last]
        ? new SourceSpan(last.Span.Source, last.Span.End, last.Span.End, last.Span.CharEnd, last.Span.CharEnd)
        : default;

    public SourceSpan InputStart =>
        _slots is [var first, ..]
        ? new SourceSpan(first.Span.Source, first.Span.Start, first.Span.Start, first.Span.CharStart, first.Span.CharStart)
        : default;

    public bool TryStep(StreamBuffer<int> buffer)
    {
        if (_next == _slots.Length)
        {
            return false;
        }

        buffer.Add(_next, _slots[_next]);
        _next++;
        return true;
    }
}

file sealed class StrideUpstream(long first, int stride, int count, long? firstChar) : IUpstream<char>
{
    private readonly int _count = count;
    private readonly long _first = first;
    private readonly long? _firstChar = firstChar;
    private int _next;
    private readonly int _stride = stride;

    public SourceSpan InputEnd => new(SourceId.Unnamed, _first + (long)_count * _stride, _first + (long)_count * _stride);

    public SourceSpan InputStart => new(SourceId.Unnamed, _first, _first);

    public bool TryStep(StreamBuffer<char> buffer)
    {
        if (_next == _count)
        {
            return false;
        }

        var start = _first + (long)_next * _stride;

        var span = _firstChar is { } firstChar
            ? new SourceSpan(SourceId.Unnamed, start, start + _stride - 1, firstChar + _next, firstChar + _next + 1)
            : new SourceSpan(SourceId.Unnamed, start, start + _stride - 1);

        buffer.Add('x', new StreamSlot(span, start, null, 0));
        _next++;
        return true;
    }
}

public class SpanStorageTests
{
    private static readonly SourceId _other = new SourceId(1);

    private static readonly Gen<SourceSpan> _spans = Gen.Select(
        Gen.OneOf(Gen.Long[0, 100], Gen.Long[(1L << 31) - 100, (1L << 31) + 100], Gen.Long[(1L << 32) - 100, (1L << 32) + 100], Gen.Long[0, 1L << 40]),
        Gen.OneOf(Gen.Long[0, 10], Gen.Long[int.MaxValue - 2L, int.MaxValue + 2L]),
        Gen.Int[0, 4],
        Gen.Int[0, 4],
        static (start, length, decoding, source) => decoding switch
        {
            0 => new SourceSpan(source == 0 ? _other : SourceId.Unnamed, start, start + length, start / 2, start / 2 + length / 2),
            1 => new SourceSpan(source == 0 ? _other : SourceId.Unnamed, start, start + length, start + 3, start + 3 + length),
            _ => new SourceSpan(source == 0 ? _other : SourceId.Unnamed, start, start + length),
        }
    );

    private static readonly Gen<StreamSlot> _slots = Gen.Select(
        _spans,
        Gen.Long[0, 1L << 40],
        Gen.Bool,
        Gen.Int[0, 3],
        static (span, stepStart, stateful, group) => new StreamSlot(span, stepStart, stateful ? new object() : null, group)
    );

    private static StreamBuffer<int> Filled(StreamSlot[] slots, int segmentSize)
    {
        var buffer = new StreamBuffer<int>(new SlotUpstream(slots), segmentSize);
        var cursor = new Cursor<int>(buffer);

        while (cursor.TryRead(out _)) { }

        return buffer;
    }

    private static bool ReadsBack(StreamBuffer<int> buffer, StreamSlot[] slots) =>
        Enumerable.Range((int)buffer.Start, slots.Length - (int)buffer.Start)
            .All(i => buffer.SlotAt(i) == slots[i] && buffer.SpanOf(i, i + 1) == slots[i].Span);

    private static StreamSlot Slot(SourceSpan span) => new(span, span.Start, null, 0);

    [Fact]
    public void ABufferedItemsSpanTakesTwo32BitOffsetsAndItsSlot32Bytes()
    {
        Assert.Equal(8, Unsafe.SizeOf<StreamBuffer<char>.PackedSpan>());
        Assert.Equal(32, Unsafe.SizeOf<StreamBuffer<char>.PackedSlot>());
    }

    [Fact]
    public void EverySlotReadsBackAsItWasAddedBeforeAndAfterARelease() =>
        Gen.Select(_slots.Array[0, 30], Gen.Int[1, 5], Gen.Int[0, 31]).Sample((slots, segmentSize, horizon) =>
        {
            var buffer = Filled(slots, segmentSize);
            var before = ReadsBack(buffer, slots);

            buffer.Release(horizon, failures: null);

            return before && ReadsBack(buffer, slots);
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReadingSpansPast4GBAllocatesNothingPerItem(bool decoded)
    {
        const int length = 1 << 20;
        const long first = 5L << 30;
        const long firstChar = first / 3;

        var buffer = new StreamBuffer<char>(
            new StrideUpstream(
                first,
                stride: 3,
                count: 8 * length,
                decoded
                    ? firstChar
                    : null
            ),
            segmentSize: 1024
        );
        var position = 0L;

        void Read()
        {
            var cursor = new Cursor<char>(buffer);
            cursor.Reset(position);

            for (var i = 0; i < length && cursor.TryRead(out _); i++)
            {
                if (cursor.Position % 1024 == 0)
                {
                    buffer.Release(cursor.Position, failures: null);
                }
            }

            position = cursor.Position;
        }

        Read();

        var allocated = Allocation.Fewest(Read, attempts: 5);
        var last = first + (position - 1) * 3;
        var lastChar = firstChar + position - 1;

        var expected = decoded
            ? new SourceSpan(SourceId.Unnamed, last, last + 2, lastChar, lastChar + 1)
            : new SourceSpan(SourceId.Unnamed, last, last + 2);

        Assert.True(position >= 2 * length, $"Read only {position} items.");
        Assert.True(allocated < 4096, $"{allocated} bytes allocated over {length} items.");
        Assert.Equal(expected, buffer.SpanOf(position - 1, position));
        Assert.Equal(decoded, buffer.StoresCharOffsets(position - 1));
    }

    [Fact]
    public void SpansThatDoNotFitInTwo32BitNumbersReadBackExactly()
    {
        const long base3GB = 3L << 30;

        StreamSlot[] slots =
        [
            Slot(new SourceSpan(SourceId.Unnamed, base3GB, base3GB + 1)),
            Slot(new SourceSpan(SourceId.Unnamed, 0, 1)),
            Slot(new SourceSpan(SourceId.Unnamed, 2 * base3GB, 2 * base3GB + 1)),
            Slot(new SourceSpan(SourceId.Unnamed, base3GB, 2 * base3GB)),
            Slot(new SourceSpan(_other, base3GB + 1, base3GB + 2)),
            Slot(new SourceSpan(SourceId.Unnamed, base3GB + 10, base3GB + 14, 4, 6)),
            Slot(new SourceSpan(SourceId.Unnamed, base3GB + 14, base3GB + 15, base3GB, base3GB + 1)),
            Slot(new SourceSpan(SourceId.Unnamed, base3GB + 15, base3GB + 16, 7, 7 + (1L << 31))),
            Slot(new SourceSpan(SourceId.Unnamed, base3GB + 16, base3GB + 18, 8, 9)),
            Slot(new SourceSpan(SourceId.Unnamed, base3GB + 20, base3GB + 21)),
        ];

        var buffer = Filled(slots, segmentSize: 16);

        Assert.Equal(slots, Enumerable.Range(0, slots.Length).Select(i => buffer.SlotAt(i)));
        Assert.Equal(new SourceSpan(SourceId.Unnamed, base3GB, base3GB + 21), buffer.SpanOf(0, slots.Length));
        Assert.Equal(new SourceSpan(SourceId.Unnamed, base3GB + 10, base3GB + 18, 4, 9), buffer.SpanOf(5, 9));
    }
}
