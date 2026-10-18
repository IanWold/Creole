namespace Creole.Tests;

public class CursorAllocationTests
{
    private const int Length = 1 << 20;

    private static readonly string _text = new('x', Length);

    private static readonly int[] _items = [.. Enumerable.Range(0, Length)];

    private static readonly SegmentedSource<char> _segments = new(_text.ToCharArray(), 4096);

    [Fact]
    public void ReadingAStringAllocatesNothing() =>
        AssertNoAllocation(static () =>
        {
            var cursor = new Cursor<char>(_text);
            var count = 0;
            while (cursor.TryRead(out _))
            {
                count++;
            }

            return count;
        });

    [Fact]
    public void PeekingAndAdvancingOverAnArrayAllocatesNothing() =>
        AssertNoAllocation(static () =>
        {
            var cursor = new Cursor<int>(_items);
            var count = 0;
            while (cursor.TryPeek(out _))
            {
                cursor.Advance(1);
                count++;
            }

            return count;
        });

    [Fact]
    public void ScanningAvailableAllocatesNothing() =>
        AssertNoAllocation(static () =>
        {
            var cursor = new Cursor<char>(_segments);
            var count = 0;
            while (cursor.Available is { IsEmpty: false } available)
            {
                count += available.Length;
                cursor.Advance(available.Length);
            }

            return count;
        });

    [Fact]
    public void ReadingAcrossSegmentsAllocatesNothing() =>
        AssertNoAllocation(static () =>
        {
            var cursor = new Cursor<char>(_segments);
            var count = 0;
            while (cursor.TryRead(out _))
            {
                count++;
            }

            cursor.Reset(0);
            return count;
        });

    private static void AssertNoAllocation(Func<int> read)
    {
        Assert.Equal(Length, read());

        var before = GC.GetAllocatedBytesForCurrentThread();
        var count = read();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(Length, count);
        Assert.True(allocated < Length, $"{allocated} bytes allocated over {Length} reads.");
    }
}
