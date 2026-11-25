namespace Creole;

/// <summary>
/// One run of a pipeline's layers: what every layer in the chain is opened with.
/// </summary>
/// <remarks>
/// A pipeline makes one for each run, and a second run, with a failure log, makes another.
/// Every layer of the chain is opened with the same one.
/// </remarks>
/// <param name="options">The settings for the parse, whose depth limit applies to each step.</param>
/// <param name="recordFailures">Whether each layer keeps a failure log, so that a failed step reports what it expected.</param>
internal sealed class PipelineRun(ParseOptions options, bool recordFailures)
{
    /// <summary>
    /// The settings for the parse, whose depth limit applies to each step.
    /// </summary>
    public ParseOptions Options { get; } = options;

    /// <summary>
    /// Whether each layer keeps a failure log, so that a failed step reports what it expected.
    /// </summary>
    public bool RecordFailures { get; } = recordFailures;
}
