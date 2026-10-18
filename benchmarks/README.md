# Benchmarks

`Creole.Benchmarks` is the BenchmarkDotNet harness behind every phase gate (§ [Benchmark plan](../whitepaper.md#benchmark-plan)). It holds the shared inputs and the competitor baselines. Creole's own engines join each benchmark class as they are built ([BENCH-02](../work/active/13-benchmarks/BENCH-02-full-benchmark-suite.md)).

## Running

| Task | Command |
| --- | --- |
| Everything, default job | `dotnet run -c Release --project benchmarks/Creole.Benchmarks` |
| One class | `dotnet run -c Release --project benchmarks/Creole.Benchmarks -- --filter '*Json*'` |
| Quick, less precise | add `--job short` |
| Check that it runs | add `--job dry` (one cold iteration; the numbers mean nothing) |
| Compare two runs | `python3 scripts/bench-compare.py <old-run> <new-run>` |

With no arguments the program runs every benchmark. Any arguments go to BenchmarkDotNet unchanged.

Each run writes to a new folder under the current directory, `BenchmarkDotNet.Artifacts/runs/<UTC yyyyMMdd-HHmmss>-<commit>/`, with full JSON, GitHub Markdown, CSV and HTML reports per class. The folder is git-ignored. `bench-compare.py` matches benchmarks by full name and prints mean time and allocation with new/old ratios. It warns when the two runs used different hardware or runtimes.

The on-demand [Benchmarks workflow](../.github/workflows/benchmarks.yml) runs the harness on a GitHub runner and uploads the run folder. Shared runners are noisy, so use it to check that the harness runs and to spot large regressions, not to set thresholds.

## What is measured

| Class | Plan row | Baselines | Inputs | Reported |
| --- | --- | --- | --- | --- |
| `JsonBenchmarks` | JSON documents, small to large | System.Text.Json (ratio baseline, the ceiling), Parlot, Parlot compiled, Pidgin, Superpower, Sprache | `small` (1.3 KB), `medium` (104 KB), `large` (1.03 MB) | Mean per document, MB/s of UTF-8 input, allocation |
| `ArithmeticBenchmarks` | Arithmetic expressions | Hand-written recursive descent (ratio baseline), Parlot, Parlot compiled, Pidgin with `ExpressionParser`, Pidgin with hand-built folds | 1,000 expressions | Mean per expression, allocation |

Every run uses the memory diagnoser.

## Keeping the baselines comparable

- **Same result.** Every JSON baseline builds the same `JsonValue` tree, and every arithmetic baseline evaluates to the same `double`. `tests/Creole.Benchmarks.Tests` checks each one against the reference on every input, so a baseline that skips work fails the build.
- **Same input.** Inputs are generated from fixed `System.Random` seeds, and their SHA-256 hashes are pinned in the tests. Changing a generator changes every number, so it fails a test until the hashes are updated on purpose. A seeded `Random` is not guaranteed to give the same sequence on every .NET version; if an upgrade changes it, the same test fails, and results from before and after are no longer directly comparable.
- **Idiomatic grammars.** Each baseline uses its library's own tools where they exist: Parlot's `Terms`, `LeftAssociative` and `Unary`; Pidgin's `ExpressionParser`; Superpower's tokenizer followed by token-list parsers. Numbers are parsed with `double.Parse` where the library has no float parser of its own.

Known differences:

| Baseline | Difference |
| --- | --- |
| System.Text.Json | Reads UTF-8 bytes; the combinator baselines read a `string`. |
| Parlot | Its string literal rejects the JSON `\/` escape, so the inputs leave that escape out. `Separated` fails on an empty list, so the grammar wraps it in `ZeroOrOne`. |
| Pidgin | Arithmetic has two Pidgin baselines. `Pidgin` uses `ExpressionParser`, the idiomatic choice; `PidginFolds` folds `Many` by hand and is faster. Set "near Pidgin speed" against the faster one. |
| Parlot | Parlot 1.5.12, the latest stable release, has no build-time generator; only `Compile()` is measured. Add a generated variant when a stable release ships one. |

## Adding a benchmark

Put the inputs in `Inputs/`, generated from a fixed `Random` seed, and pin its hash in `BaselineAgreementTests`. Put each library's grammar in a folder per input language (`Json/`, `Arithmetic/`), add an agreement test for it, then add a `[Benchmark]` method. To report MB/s for a new class, give `ThroughputColumn` its input size.
