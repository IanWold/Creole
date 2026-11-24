namespace Creole.Tests;

internal class SegmentedSource<T>(T[] items, int segmentSize) : ICursorSource<T>
{
    public SourceSpan InputEnd => new(SourceId.Unnamed, items.Length, items.Length);

    public SourceSpan InputStart => new(SourceId.Unnamed, 0, 0);

    public void DropSpan() { }

    public void HoldSpan(long position) { }

    public void Release(long horizon, FailureLog<T>? failures) { }

    public SourceSpan SpanOf(long start, long end) =>
        new(SourceId.Unnamed, start, end);

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
