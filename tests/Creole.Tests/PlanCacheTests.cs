using System.Runtime.CompilerServices;
using Creole.Nodes;
using Creole.Plans;

namespace Creole.Tests;

public class PlanCacheTests
{
    [Fact]
    public void RepeatedUseReturnsTheSamePlanWithoutLoweringAgain()
    {
        var grammar = new CountingNode<int>(new ReturnNode<char, int>(1));

        var first = PlanCache.Get(grammar);
        var second = PlanCache.Get(grammar);

        Assert.Same(first, second);
        Assert.Equal(1, grammar.LowerCount);
    }

    [Fact]
    public void ConcurrentFirstUseLowersEachGrammarOnceAndParsesCorrectly()
    {
        const int threadCount = 64;
        const int grammarCount = 100;
        const int rounds = 20;

        for (var round = 0; round < rounds; round++)
        {
            var grammars = Enumerable.Range(0, grammarCount)
                .Select(i => new CountingNode<int>(new ReturnNode<char, int>(i)))
                .ToArray();

            var plans = new Plan<char, int>[threadCount, grammarCount];
            var results = new int[threadCount, grammarCount];
            var parsed = new bool[threadCount, grammarCount];

            using var start = new Barrier(threadCount);

            var threads = Enumerable.Range(0, threadCount)
                .Select(t => new Thread(() =>
                {
                    start.SignalAndWait();

                    for (var n = 0; n < grammarCount; n++)
                    {
                        var g = (n + t) % grammarCount;
                        var plan = PlanCache.Get(grammars[g]);
                        var cursor = new Cursor<char>("input");
                        var result = -1;

                        plans[t, g] = plan;
                        parsed[t, g] = plan.TryParse(ref cursor, ref result) && cursor.Position == 0;
                        results[t, g] = result;
                    }
                }))
                .ToArray();

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            for (var g = 0; g < grammarCount; g++)
            {
                Assert.Equal(1, grammars[g].LowerCount);

                for (var t = 0; t < threadCount; t++)
                {
                    Assert.Same(plans[0, g], plans[t, g]);
                    Assert.True(parsed[t, g]);
                    Assert.Equal(g, results[t, g]);
                }
            }
        }
    }

    [Fact]
    public void ACachedPlanDoesNotKeepItsGrammarAlive()
    {
        var grammar = CacheAndForget();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(grammar.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CacheAndForget()
    {
        var grammar = new ReturnNode<char, int>(1);
        PlanCache.Get(grammar);
        return new WeakReference(grammar);
    }
}
