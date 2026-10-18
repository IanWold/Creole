namespace Creole;

/// <summary>
/// Supplies a <see cref="Cursor{T}"/> with the segments of an input that is not held as one span, such as a layer boundary's stream buffer.
/// </summary>
internal interface ICursorSource<T>
{
    /// <summary>
    /// Gets the segment holding the item at <paramref name="position"/>, pulling more input if needed.
    /// </summary>
    /// <param name="position">An absolute position in the input.</param>
    /// <param name="segment">The segment, which holds the item at <paramref name="position"/>.</param>
    /// <param name="segmentStart">The absolute position of the segment's first item.</param>
    /// <returns><see langword="false"/> if no item exists at <paramref name="position"/>.</returns>
    bool TryGetSegment(long position, out ReadOnlyMemory<T> segment, out long segmentStart);
}
