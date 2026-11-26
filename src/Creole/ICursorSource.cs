namespace Creole;

/// <summary>
/// Supplies a <see cref="Cursor{T}"/> with the segments of an input that is not held as one span, such as a layer boundary's stream buffer.
/// </summary>
internal interface ICursorSource<T>
{
    /// <summary>
    /// Stops holding the position that the matching <see cref="HoldSpan"/> held.
    /// </summary>
    void DropSpan();

    /// <summary>
    /// Holds <paramref name="position"/>, where a span combinator's range starts, until the matching <see cref="DropSpan"/>.
    /// </summary>
    /// <remarks>
    /// The combinator reads the span there after the cursor has moved on.
    /// A source that releases the item first copies its span, whether or not the reading layer keeps a failure log.
    /// </remarks>
    void HoldSpan(long position);

    /// <summary>
    /// Lets the source discard what lies below <paramref name="horizon"/>, which nothing will read again, after <paramref name="failures"/> has copied what its errors need.
    /// </summary>
    /// <param name="horizon">The minimum of the cursor and its floor.</param>
    /// <param name="failures">The failure log of the layer that reads the source, or null if it keeps none.</param>
    void Release(long horizon, FailureLog<T>? failures);

    /// <summary>
    /// The source span of the items from <paramref name="start"/> up to <paramref name="end"/>: from the start of the first to the end of the last.
    /// </summary>
    /// <remarks>
    /// An empty range gives a zero-width span at the start of the item at <paramref name="start"/>, or at the end of the last item at the end of the input.
    /// The item at <paramref name="start"/> may have been released if <see cref="HoldSpan"/> holds it.
    /// </remarks>
    SourceSpan SpanOf(long start, long end);

    /// <summary>
    /// Gets the segment holding the item at <paramref name="position"/>, pulling more input if needed.
    /// </summary>
    /// <param name="position">An absolute position in the input.</param>
    /// <param name="segment">The segment, which holds the item at <paramref name="position"/>.</param>
    /// <param name="segmentStart">The absolute position of the segment's first item.</param>
    /// <returns><see langword="false"/> if no item exists at <paramref name="position"/>.</returns>
    bool TryGetSegment(long position, out ReadOnlyMemory<T> segment, out long segmentStart);
}
