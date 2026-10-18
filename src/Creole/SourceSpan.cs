namespace Creole;

/// <summary>
/// A range of the original source, from <see cref="Start"/> (inclusive) to <see cref="End"/> (exclusive).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Start"/> and <see cref="End"/> are offsets in the root layer's units: characters when the source is text, bytes when it is bytes.
/// <see cref="CharStart"/> and <see cref="CharEnd"/> are offsets in the decoded text, used for lines, columns and display.
/// When the source is text the two pairs are equal.
/// </para>
/// <para>
/// Offsets are <see cref="long"/> because a streamed source can be longer than 2 GB.
/// A zero-width span (<see cref="Start"/> equal to <see cref="End"/>) marks a position, such as where an inserted item belongs.
/// </para>
/// </remarks>
public readonly record struct SourceSpan
{
    /// <summary>
    /// Creates a span over a text source, whose character offsets equal its root offsets.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is negative, or <paramref name="end"/> is less than <paramref name="start"/>.</exception>
    public SourceSpan(SourceId source, long start, long end) : this(source, start, end, start, end) { }

    /// <summary>
    /// Creates a span over a byte source, giving both byte offsets and character offsets in the decoded text.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A start offset is negative, or an end offset is less than its start.</exception>
    public SourceSpan(SourceId source, long start, long end, long charStart, long charEnd)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfLessThan(end, start);
        ArgumentOutOfRangeException.ThrowIfNegative(charStart);
        ArgumentOutOfRangeException.ThrowIfLessThan(charEnd, charStart);

        Source = source;
        Start = start;
        End = end;
        CharStart = charStart;
        CharEnd = charEnd;
    }

    /// <summary>
    /// The source the span points into.
    /// </summary>
    public SourceId Source { get; }

    /// <summary>
    /// The offset of the first unit in the span, in root-layer units.
    /// </summary>
    public long Start { get; }

    /// <summary>
    /// The offset just past the last unit in the span, in root-layer units.
    /// </summary>
    public long End { get; }

    /// <summary>
    /// The offset of the first character in the span, in the decoded text. Equal to <see cref="Start"/> for a text source.
    /// </summary>
    public long CharStart { get; }

    /// <summary>
    /// The offset just past the last character in the span, in the decoded text. Equal to <see cref="End"/> for a text source.
    /// </summary>
    public long CharEnd { get; }

    /// <summary>
    /// The number of root-layer units the span covers.
    /// </summary>
    public long Length => End - Start;

    /// <summary>
    /// Deconstructs the span into its source and root-layer offsets.
    /// </summary>
    public void Deconstruct(out SourceId source, out long start, out long end)
    {
        source = Source;
        start = Start;
        end = End;
    }
}
