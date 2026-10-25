namespace Creole.Tests;

/// <summary>
/// Measures what code allocates on the current thread.
/// </summary>
internal static class Allocation
{
    /// <summary>
    /// The fewest bytes <paramref name="run"/> allocates on the current thread, over up to three runs, stopping at the first that allocates nothing.
    /// </summary>
    /// <remarks>
    /// Code that allocates does so on every run. While other tests run in parallel, the runtime now and then allocates a few kilobytes once on the test thread, which a single run would count.
    /// </remarks>
    public static long Fewest(Action run)
    {
        var fewest = long.MaxValue;

        for (var attempt = 0; attempt < 3 && fewest != 0; attempt++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();

            run();
            fewest = Math.Min(fewest, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        return fewest;
    }
}
