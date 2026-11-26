namespace Creole;

/// <summary>
/// Produces the items of a layer boundary into the <see cref="StreamBuffer{T}"/> that holds them, one step at a time.
/// </summary>
/// <remarks>
/// A step is one run of the producing layer's item parser, or one chunk of the source at the root layer.
/// It may add any number of items, including none.
/// </remarks>
internal interface IUpstream<T>
{
    /// <summary>
    /// A zero-width span where the input this upstream reads ends in the source.
    /// </summary>
    /// <remarks>
    /// A layer gives its input's, and a root gives the end of its source, so input the layer skipped after its last item lies before it.
    /// It is read once <see cref="TryStep"/> has returned <see langword="false"/>.
    /// </remarks>
    SourceSpan InputEnd { get; }

    /// <summary>
    /// A zero-width span where the input this upstream reads starts in the source.
    /// </summary>
    /// <remarks>
    /// A layer gives its input's, and a root gives the start of its source, so input the layer skipped before its first item lies after it.
    /// </remarks>
    SourceSpan InputStart { get; }

    /// <summary>
    /// Runs one step, adding its outputs to <paramref name="buffer"/> with <see cref="StreamBuffer{T}.Add"/>.
    /// </summary>
    /// <returns><see langword="false"/> if the input is exhausted, so no step ran.</returns>
    bool TryStep(StreamBuffer<T> buffer);
}
