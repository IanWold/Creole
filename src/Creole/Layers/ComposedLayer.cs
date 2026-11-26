namespace Creole;

/// <summary>
/// Runs <see cref="First"/>, then <see cref="Second"/> over its items. Backs <c>Then</c>.
/// </summary>
/// <remarks>
/// A run holds <see cref="First"/>'s items in a buffer, which <see cref="Second"/> reads and may rewind over without <see cref="First"/> running again.
/// </remarks>
internal sealed class ComposedLayer<TIn, TMiddle, TOut>(Layer<TIn, TMiddle> first, Layer<TMiddle, TOut> second) : Layer<TIn, TOut>
{
    internal override bool DecodesUtf8 => First.DecodesUtf8;

    internal override IEnumerable<(object Channel, string Layer)> Readers => First.Readers.Concat(Second.Readers);

    public Layer<TIn, TMiddle> First { get; } = first;

    public Layer<TMiddle, TOut> Second { get; } = second;

    internal override IUpstream<TOut> Open(ICursorSource<TIn> input, PipelineRun run) =>
        Second.Open(new StreamBuffer<TMiddle>(First.Open(input, run)), run);
}
