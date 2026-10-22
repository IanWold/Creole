using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using static Creole.CharGrammar;

namespace Creole.Benchmarks;

/// <summary>
/// Each interpreter lowering rewrite on a grammar it targets, lowered with the rewrite and without it.
/// The rewrite stays if it is at least 5% faster on its target and no other target is more than 5% slower.
/// </summary>
/// <remarks>
/// Each category compares one rewrite: the plan without it is the baseline, and every other rewrite is on in both.
/// Plans are run directly, without the public <c>Parse</c> API.
/// </remarks>
[BenchmarkCategory("Lowering")]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class LoweringBenchmarks
{
    private const string ScanLongRuns = "ScanLongRuns";
    private const string ScanShortRuns = "ScanShortRuns";

    private static readonly Parser<char, Unit> _quotedStrings =
        Char('"').Then(NoneOf('"', '\\').SkipMany()).Before(Char('"')).SkipMany();

    private static readonly Parser<char, Unit> _words =
        Letter.SkipAtLeastOnce().Before(Whitespace.SkipAtLeastOnce()).SkipMany();

    private readonly Plan<char, Unit> _quotedStringsWithoutScan = Lower(_quotedStrings, LoweringOptions.All & ~LoweringOptions.Scan);
    private readonly Plan<char, Unit> _quotedStringsWithScan = Lower(_quotedStrings, LoweringOptions.All);
    private readonly string _stringInput = LoweringInputs.Strings;
    private readonly string _wordInput = LoweringInputs.Words;
    private readonly Plan<char, Unit> _wordsWithoutScan = Lower(_words, LoweringOptions.All & ~LoweringOptions.Scan);
    private readonly Plan<char, Unit> _wordsWithScan = Lower(_words, LoweringOptions.All);

    private static Plan<char, T> Lower<T>(Parser<char, T> parser, LoweringOptions options) =>
        new PlanBuilder(options).Lower(parser);

    private static long Run<T>(Plan<char, T> plan, string input)
    {
        var cursor = new Cursor<char>(input);
        T result = default!;

        return plan.TryParse(ref cursor, ref result) ? cursor.Position : -1;
    }

    [Benchmark(Baseline = true), BenchmarkCategory(ScanLongRuns)]
    public long LongRunsWithoutScan() =>
        Run(_quotedStringsWithoutScan, _stringInput);

    [Benchmark, BenchmarkCategory(ScanLongRuns)]
    public long LongRunsWithScan() =>
        Run(_quotedStringsWithScan, _stringInput);

    [Benchmark(Baseline = true), BenchmarkCategory(ScanShortRuns)]
    public long ShortRunsWithoutScan() =>
        Run(_wordsWithoutScan, _wordInput);

    [Benchmark, BenchmarkCategory(ScanShortRuns)]
    public long ShortRunsWithScan() =>
        Run(_wordsWithScan, _wordInput);
}
