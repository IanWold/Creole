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

Without `--filter` the program runs every benchmark. Other arguments go to BenchmarkDotNet unchanged.

Each run writes to a new folder under the current directory, `BenchmarkDotNet.Artifacts/runs/<UTC yyyyMMdd-HHmmss>/`, with full JSON, GitHub Markdown, CSV and HTML reports per class. The folder is git-ignored. `bench-compare.py` matches benchmarks by full name and prints mean time and allocation with new/old ratios. It warns when the two runs used different hardware or runtimes.

## Gate runs

Phase gates compare Creole with its baselines as ratios ([ADR-0003](../work/docs/decisions/0003-numeric-gate-thresholds.md); thresholds in § [Roadmap](../whitepaper.md#roadmap)). A gate run:

- runs on the maintainer's laptop, on AC power, with no other heavy load;
- uses the default job, with Creole and its baselines in the same invocation;
- repeats the full run three times and takes each benchmark's median ratio, then the geometric mean over the gate's benchmarks;
- records the Parlot version.

Single runs on this machine vary by up to 1.3× per benchmark, so one run decides nothing. Allocations do not vary and can be compared directly. Hosted CI runners only catch regressions of 10% or more and do not decide gates.

## What is measured

| Class | Plan row | Baselines | Inputs | Reported |
| --- | --- | --- | --- | --- |
| `JsonBenchmarks` | JSON documents, small to large | System.Text.Json (ratio baseline, the ceiling), Parlot, Pidgin, Superpower, Sprache | `small` (1.3 KB), `medium` (104 KB), `large` (1.03 MB) | Mean per document, MB/s of UTF-8 input, allocation |
| `ArithmeticBenchmarks` | Arithmetic expressions | Hand-written recursive descent (ratio baseline), Parlot, Pidgin with `ExpressionParser`, Pidgin with hand-built folds | 1,000 expressions | Mean per expression, allocation |
| `ErrorBenchmarks` | None: [ERR-02](../work/active/04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)'s evidence that recording failures allocates nothing | The token JSON parser with no failure log (ratio baseline) | `JsonTokens`, and a copy with one comma past the middle made a colon | Mean per input and allocation, with and without a log, for the plain grammar and one with `Named` and `Context`; building the reported error alone (`BuildError`) |
| `LoweringBenchmarks` | None: [INT-05](../work/active/03-interpreter/INT-05-interpreter-lowering-rewrites.md)'s and [INT-06](../work/active/03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)'s evidence for each lowering rewrite | The same grammar lowered without the rewrite (ratio baseline, one per category) | `CSharp` (100 KB of generated C#-like text, for the 138-rule `Longest` lexer in `LongestLexers`), `Digits` (100 KB of digits), `Json` (the medium JSON document, 104 KB, also tokenized by the 12-rule `Longest` lexer) and `JsonTokens` (it lexed to kinds by a hand-written lexer), `Strings` (100 KB of quoted strings, long runs), `Words` (100 KB of words and spaces, short runs) | Mean per input, allocation; the `DfaLower…` categories time lowering itself |

Every run uses the memory diagnoser.

`LoweringBenchmarks` decides no gate. It runs interpreter plans directly, through `InternalsVisibleTo`, until INT-08 adds the public `Parse`. Each category compares one rewrite on and off, with every other rewrite on in both. A rewrite stays if the median of three runs is at least 5% faster on its target and no other category is more than 5% slower.

`FourKeysSkipped` and `FourKeysLast` measure dispatch at its threshold of four keyed alternatives ([INT-05](../work/active/03-interpreter/INT-05-interpreter-lowering-rewrites.md)). In the first, the keyed alternatives come first and never match, which is dispatch's best case. In the second, they come after the alternatives that match, so dispatch skips nothing and only adds its cost. The second is slower by design; re-measure both after changing `DispatchPlan`.

## Keeping the baselines comparable

- **Same result.** Every JSON baseline builds the same `JsonValue` tree, and every arithmetic baseline evaluates to the same `double`. `tests/Creole.Benchmarks.Tests` checks each one against the reference on every input, so a baseline that skips work fails the build.
- **Same input.** Inputs are generated from fixed `System.Random` seeds, and their SHA-256 hashes are pinned in the tests. Changing a generator changes every number, so it fails a test until the hashes are updated on purpose. A seeded `Random` is not guaranteed to give the same sequence on every .NET version; if an upgrade changes it, the same test fails, and results from before and after are no longer directly comparable.
- **Idiomatic grammars.** Each baseline uses its library's own tools where they exist: Parlot's `Terms`, `LeftAssociative` and `Unary`; Pidgin's `ExpressionParser`; Superpower's tokenizer followed by token-list parsers. Numbers are parsed with `double.Parse` where the library has no float parser of its own.

Known differences:

| Baseline | Difference |
| --- | --- |
| System.Text.Json | Reads UTF-8 bytes; the combinator baselines read a `string`. |
| Parlot | Its string literal rejects the JSON `\/` escape, so the inputs leave that escape out. `Separated` fails on an empty list unless given a minimum of zero, so the grammar passes `min: 0`. |
| Pidgin | Arithmetic has two Pidgin baselines. `Pidgin` uses `ExpressionParser`, the idiomatic choice; `PidginFolds` folds `Many` by hand and is faster. The Phase 0 gate uses the faster one. |
| Parlot | The harness pins Parlot 2.0.0 and measures its interpreted (Fluent) parsers, which the Phase 0 gate uses ([ADR-0003](../work/docs/decisions/0003-numeric-gate-thresholds.md)). 2.0.0 was the fastest stable release measured: faster than 1.5.12 on medium and large JSON (0.86–0.88×) and level elsewhere, and level with 1.6.0 within noise ([research note](../work/docs/research/2026-10-24-parlot-2-0-baselines-and-generator.md)). Parlot 2.0 removes `Compile()`. Its generated parsers, which the Phase 3 gate uses, join with [BENCH-03](../work/active/13-benchmarks/BENCH-03-phase-3-speed-gate.md). |

## Adding a benchmark

Put the inputs in `Inputs/`, generated from a fixed `Random` seed, and pin its hash in `BaselineAgreementTests`. Put each library's grammar in a folder per input language (`Json/`, `Arithmetic/`), add an agreement test for it, then add a `[Benchmark]` method. To report MB/s for a new class, give `ThroughputColumn` its input size.
