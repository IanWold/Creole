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
    /// Runs one step, adding its outputs to <paramref name="buffer"/> with <see cref="StreamBuffer{T}.Add"/>.
    /// </summary>
    /// <returns><see langword="false"/> if the input is exhausted, so no step ran.</returns>
    bool TryStep(StreamBuffer<T> buffer);
}
