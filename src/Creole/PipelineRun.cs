namespace Creole;

/// <summary>
/// One run of a pipeline's layers: what every layer in the chain is opened with, and the mode channels its layers read.
/// </summary>
/// <remarks>
/// <para>
/// A pipeline makes one for each run, and a second run, with a failure log, makes another.
/// Every layer of the chain is opened with the same one.
/// </para>
/// <para>
/// Layers open from the bottom of the chain up, and a <c>Layer.Modal</c> adds its channel with <see cref="Read{TMode}"/> as it opens.
/// So the writer <see cref="Writer"/> makes for a layer as it opens holds exactly the channels read below that layer.
/// </para>
/// </remarks>
/// <param name="options">The settings for the parse, whose depth limit applies to each step.</param>
/// <param name="recordFailures">Whether each layer keeps a failure log, so that a failed step reports what it expected.</param>
internal sealed class PipelineRun(ParseOptions options, bool recordFailures)
{
    private ModeLink[] _read = [];

    /// <summary>
    /// The settings for the parse, whose depth limit applies to each step.
    /// </summary>
    public ParseOptions Options { get; } = options;

    /// <summary>
    /// Whether each layer keeps a failure log, so that a failed step reports what it expected.
    /// </summary>
    public bool RecordFailures { get; } = recordFailures;

    /// <summary>
    /// Adds <paramref name="channel"/> to the channels read so far, for the layers that open after this one, and returns its link.
    /// </summary>
    public ModeLink<TMode> Read<TMode>(ModeChannel<TMode> channel)
    {
        var link = new ModeLink<TMode>(channel);

        _read = [.. _read, link];

        return link;
    }

    /// <summary>
    /// The writer for the layer named <paramref name="layer"/>, which may change the channels read so far: those of the layers below it.
    /// </summary>
    public ModeWriter Writer(string layer) => new(layer, _read);
}
