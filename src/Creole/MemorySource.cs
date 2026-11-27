namespace Creole;

/// <summary>
/// The root of a pipeline over input held whole in memory: one segment, which is the caller's memory and is never released.
/// </summary>
/// <remarks>
/// Item <c>i</c> has the span <c>[i, i + 1)</c> in <see cref="SourceId.Unnamed"/>.
/// A cursor over it returns slices as views of the caller's memory, not copies.
/// </remarks>
/// <param name="input">The caller's input.</param>
internal class MemorySource<T>(ReadOnlyMemory<T> input) : ICursorSource<T>
{
    private readonly ReadOnlyMemory<T> _input = input;

    public SourceSpan InputEnd => new(SourceId.Unnamed, _input.Length, _input.Length);

    public SourceSpan InputStart => new(SourceId.Unnamed, 0, 0);

    public void DropSpan() { }

    public void HoldSpan(long position) { }

    public void Release(long horizon, FailureLog<T>? failures) { }

    public SourceSpan SpanOf(long start, long end) =>
        new(SourceId.Unnamed, start, end);

    public bool TryGetSegment(long position, long horizon, out ReadOnlyMemory<T> segment, out long segmentStart)
    {
        segment = _input;
        segmentStart = 0;

        return position >= 0 && position < _input.Length;
    }
}
