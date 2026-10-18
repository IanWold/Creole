using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Creole.Benchmarks.Harness;

internal sealed class ThroughputColumn : IColumn
{
    public string Id => nameof(ThroughputColumn);

    public string ColumnName => "MB/s";

    public bool AlwaysShow => true;

    public ColumnCategory Category => ColumnCategory.Custom;

    public int PriorityInCategory => 0;

    public bool IsNumeric => true;

    public UnitType UnitType => UnitType.Dimensionless;

    public string Legend => "Throughput in 10^6 bytes of UTF-8 input per second";

    public bool IsAvailable(Summary summary) => summary.BenchmarksCases.Any(c => InputBytes(c) is not null);

    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) =>
        GetValue(summary, benchmarkCase, SummaryStyle.Default);

    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var mean = summary[benchmarkCase]?.ResultStatistics?.Mean;
        var bytes = InputBytes(benchmarkCase);

        if (mean is not > 0 || bytes is null)
        {
            return "-";
        }

        var megabytesPerSecond = bytes.Value / (mean.Value / 1e9) / 1e6;
        return megabytesPerSecond.ToString("N1", CultureInfo.InvariantCulture);
    }

    private static long? InputBytes(BenchmarkCase benchmarkCase) =>
        benchmarkCase.Descriptor.Type == typeof(JsonBenchmarks)
        ? JsonBenchmarks.InputBytes(benchmarkCase)
        : null;
}
