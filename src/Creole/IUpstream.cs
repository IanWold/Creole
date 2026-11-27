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
    /// Whether the input has run out of what it has received so far but has not ended, so a step could not tell what to produce.
    /// </summary>
    /// <remarks>
    /// It is read once <see cref="TryStep"/> has returned <see langword="false"/>, and tells a starved step from the end of the input.
    /// A layer gives its input's, so starvation at the root reaches every layer above it.
    /// The default is false, for an upstream whose input always ends where it has run out.
    /// </remarks>
    bool Starved => false;

    /// <summary>
    /// Takes the restart bookmark: notes where this upstream stands, and has the input it reads take its own, as an item of <c>ParseManyAsync</c> starts.
    /// </summary>
    /// <remarks>
    /// A layer notes its position in its input, its state, its mode log and how many diagnostics it has raised.
    /// The default does nothing, for a root, which is never rewound.
    /// </remarks>
    void Bookmark() { }

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
    /// Goes back to the restart bookmark, as a starved item restarts: undoes the mode writes since, withdraws the diagnostics raised since, forgets every failure, and puts back the position and state, then has the input it reads go back to its own.
    /// If a mode change sent the upstream back before its bookmark since, it goes back to that earlier step instead.
    /// </summary>
    /// <remarks>
    /// The default does nothing and returns <see langword="false"/>, for a root, which keeps every item it has added because it cannot produce them again.
    /// </remarks>
    /// <returns>Whether the upstream went back, so that the buffer must discard the items added since the bookmark.</returns>
    bool Restart() => false;

    /// <summary>
    /// Goes back to the step that produced the item beside <paramref name="slot"/>, so that the next step produces that item again, if this upstream reads <paramref name="channel"/>, whose mode the layer above changed.
    /// </summary>
    /// <remarks>
    /// The default does nothing and returns <see langword="false"/>, for an upstream that reads no mode.
    /// </remarks>
    /// <returns>Whether the upstream went back, so that the buffer must discard the item and every item after it.</returns>
    bool TryGoBack(StreamSlot slot, ModeLink channel) => false;

    /// <summary>
    /// Runs one step, adding its outputs to <paramref name="buffer"/> with <see cref="StreamBuffer{T}.Add"/>.
    /// </summary>
    /// <remarks>
    /// A step whose input starves adds nothing and returns <see langword="false"/>, with <see cref="Starved"/> set.
    /// </remarks>
    /// <returns><see langword="false"/> if the input is exhausted, or has starved, so no step ran.</returns>
    bool TryStep(StreamBuffer<T> buffer);
}
