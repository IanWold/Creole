namespace Creole.Tests;

internal class SegmentedSource<T>(T[] items, int segmentSize) : ICursorSource<T>
{
    public bool TryGetSegment(long position, out ReadOnlyMemory<T> segment, out long segmentStart)
    {
        if (position < 0 || position >= items.Length)
        {
            segment = default;
            segmentStart = 0;
            return false;
        }

        segmentStart = position / segmentSize * segmentSize;
        segment = items.AsMemory((int)segmentStart, (int)Math.Min(segmentSize, items.Length - segmentStart));
        return true;
    }
}
