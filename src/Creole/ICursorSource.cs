namespace Creole;

/// <summary>
/// Supplies a <see cref="Cursor{T}"/> with the segments of an input that is not held as one span, such as a layer boundary's stream buffer.
/// </summary>
internal interface ICursorSource<T>
{
    /// <summary>
    /// Lets the source discard what lies below <paramref name="horizon"/>, which nothing will read again, after <paramref name="failures"/> has copied what its errors need.
    /// </summary>
    /// <param name="horizon">The minimum of the cursor and its floor.</param>
    /// <param name="failures">The failure log of the layer that reads the source, or null if it keeps none.</param>
    void Release(long horizon, FailureLog<T>? failures);

    /// <summary>
    /// Gets the segment holding the item at <paramref name="position"/>, pulling more input if needed.
    /// </summary>
    /// <param name="position">An absolute position in the input.</param>
    /// <param name="segment">The segment, which holds the item at <paramref name="position"/>.</param>
    /// <param name="segmentStart">The absolute position of the segment's first item.</param>
    /// <returns><see langword="false"/> if no item exists at <paramref name="position"/>.</returns>
    bool TryGetSegment(long position, out ReadOnlyMemory<T> segment, out long segmentStart);
}
