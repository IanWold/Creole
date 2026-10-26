namespace Creole.Tests;

/// <summary>
/// Measures what code allocates on the current thread.
/// </summary>
internal static class Allocation
{
    /// <summary>
    /// The fewest bytes <paramref name="run"/> allocates on the current thread, over up to <paramref name="attempts"/> runs, stopping at the first that allocates nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Code that allocates does so on every run. While other tests run in parallel, the runtime now and then allocates a few kilobytes once on the test thread, which a single run would count.
    /// </para>
    /// <para>
    /// Each garbage collection during a run also adds a few hundred bytes to the count, whatever the run does, and collections are frequent while other tests run.
    /// To measure code that runs for long, measure short runs over more attempts, so that some run sees no collection.
    /// </para>
    /// </remarks>
    public static long Fewest(Action run, int attempts = 3)
    {
        var fewest = long.MaxValue;

        for (var attempt = 0; attempt < attempts && fewest != 0; attempt++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();

            run();
            fewest = Math.Min(fewest, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        return fewest;
    }
}
