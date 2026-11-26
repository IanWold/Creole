namespace Creole.MemoryGate;

/// <summary>
/// What one run of the memory gate measured, written as one line of JSON.
/// </summary>
/// <param name="Input">The input's name.</param>
/// <param name="Length">The fewest characters the run asked for.</param>
/// <param name="Characters">The characters it parsed.</param>
/// <param name="Records">The records it parsed.</param>
/// <param name="Samples">How many times it sampled the retained heap.</param>
/// <param name="BaselineBytes">The retained heap after the warm-up run, before this one.</param>
/// <param name="PeakBytes">The largest retained heap sampled during this run.</param>
/// <param name="Seconds">How long the run took, sampling included.</param>
public sealed record GateRun(string Input, long Length, long Characters, long Records, int Samples, long BaselineBytes, long PeakBytes, double Seconds)
{
    /// <summary>
    /// The peak less the baseline: what the parse itself retained at most.
    /// </summary>
    public long RetainedBytes => PeakBytes - BaselineBytes;
}
