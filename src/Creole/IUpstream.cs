namespace Creole;

/// <summary>
/// Produces the items of a layer boundary into the <see cref="StreamBuffer{T}"/> that holds them, one step at a time.
/// </summary>
/// <remarks>
/// A step is one run of the producing layer's item parser, or one chunk of the source at the root layer.
/// It may add any number of items, including none.
/// Disposing an upstream disposes the input it reads, so the buffers below it return their segments.
/// </remarks>
internal interface IUpstream<T> : IDisposable
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
    /// Does nothing, for an upstream that reads from nothing it must dispose.
    /// </summary>
    void IDisposable.Dispose() { }

    /// <summary>
    /// Forgets the furthest failure of this layer and of every layer below it, as each item of a <c>ParseMany</c> starts.
    /// </summary>
    /// <remarks>
    /// The default does nothing, for an upstream that keeps no failure log and reads from nothing that does.
    /// </remarks>
    void ForgetFailures() { }

    /// <summary>
    /// Runs one step, adding its outputs to <paramref name="buffer"/> with <see cref="StreamBuffer{T}.Add"/>.
    /// </summary>
    /// <returns><see langword="false"/> if the input is exhausted, so no step ran.</returns>
    bool TryStep(StreamBuffer<T> buffer);
}
