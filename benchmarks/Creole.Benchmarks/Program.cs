using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Creole.Benchmarks.Harness;

BenchmarkSwitcher
    .FromAssembly(typeof(ThroughputColumn).Assembly)
    .Run(
        args.Contains("--filter")
            ? args
            : [.. args, "--filter", "*"],
        DefaultConfig.Instance
            .AddDiagnoser(MemoryDiagnoser.Default)
            .AddColumn(new ThroughputColumn())
            .AddExporter(JsonExporter.Full, MarkdownExporter.GitHub, CsvExporter.Default)
            .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Trend))
            .WithOrderer(new DefaultOrderer(SummaryOrderPolicy.Declared))
            .WithArtifactsPath(Path.Combine("BenchmarkDotNet.Artifacts", "runs", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)))
    );
