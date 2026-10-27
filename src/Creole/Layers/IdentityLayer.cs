namespace Creole;

/// <summary>
/// Passes its items through unchanged. Backs <c>Layer.Identity</c>.
/// </summary>
/// <remarks>
/// <c>Then</c> returns the other layer when either side is this one, so it runs only on its own.
/// </remarks>
internal sealed class IdentityLayer<T> : Layer<T, T>
{
    private static readonly SelectLayer<T, T> _copy = new(static item => item);

    public static IdentityLayer<T> Instance { get; } = new();

    internal override IUpstream<T> Open(StreamBuffer<T> input, ParseOptions options, bool recordFailures) =>
        _copy.Open(input, options, recordFailures);
}
