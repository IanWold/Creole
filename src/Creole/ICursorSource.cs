namespace Creole;

/// <summary>
/// Supplies a <see cref="Cursor{T}"/> with the segments of an input that is not held as one span, such as a layer boundary's stream buffer.
/// </summary>
/// <remarks>
/// Disposing a source returns what it rents to its pools and disposes what it reads from, so a parse that disposes its last buffer returns every buffer below it.
/// Nothing may read a source after it is disposed.
/// </remarks>
internal interface ICursorSource<T> : IDisposable
{
    /// <summary>
    /// A zero-width span where the input of the layer that produced these items ends in the source, after any input that layer skipped after its last item.
    /// </summary>
    /// <remarks>
    /// It is known once a cursor has found the end of the items.
    /// </remarks>
    SourceSpan InputEnd { get; }

    /// <summary>
    /// A zero-width span where the input of the layer that produced these items starts in the source, before any input that layer skipped before its first item.
    /// </summary>
    SourceSpan InputStart { get; }

    /// <summary>
    /// Does nothing, for a source that rents nothing and reads from nothing it must dispose.
    /// </summary>
    void IDisposable.Dispose() { }

    /// <summary>
    /// Stops holding the position that the matching <see cref="HoldSpan"/> held.
    /// </summary>
    void DropSpan();

    /// <summary>
    /// Forgets the furthest failure of every layer that produced these items, as each item of a <c>ParseMany</c> starts.
    /// </summary>
    /// <remarks>
    /// The default does nothing, for a source that no layer produces.
    /// </remarks>
    void ForgetFailures() { }

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
