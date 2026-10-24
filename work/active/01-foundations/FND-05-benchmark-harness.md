# FND-05: Benchmark harness

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Every phase gate depends on measurements, so the harness and competitor baselines come first.

## Scope

- BenchmarkDotNet project with the memory diagnoser.
- Reference projects for Pidgin, Parlot, Superpower, Sprache, and System.Text.Json as a ceiling.
- Shared input corpora: small, medium and large JSON; arithmetic expression sets.
- JSON and arithmetic grammars written in Pidgin and Parlot as baselines.
- Results exported per run so later runs can be compared.
- On-demand CI benchmark job that runs the harness (moved here from [FND-03](FND-03-continuous-integration.md)).

## Acceptance criteria

- [x] Competitor baselines run with one command and produce comparable reports.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [FND-06 Fix numeric gate thresholds](../01-foundations/FND-06-fix-numeric-gate-thresholds.md)
- [FND-10 Move the Parlot baselines to 2.0.0](../01-foundations/FND-10-move-the-parlot-baselines-to-200.md)
- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)

## Whitepaper references

- [§ Benchmark plan](../../../whitepaper.md#benchmark-plan)

## Artifacts

- Docs: [Benchmarks README](../../../benchmarks/README.md)

## Progress notes

- 2026-10-17 (Building): Harness built in `benchmarks/Creole.Benchmarks` with BenchmarkDotNet 0.16.0-preview.2, because 0.15.8 throws "GetRuntimeVersion not implemented" on .NET 11. It uses the memory diagnoser, adds an MB/s column, and writes JSON, Markdown and CSV reports to a new `BenchmarkDotNet.Artifacts/runs/<UTC time>-<commit>/` folder each run. `scripts/bench-compare.py` compares two runs. JSON baselines: System.Text.Json (the ceiling), Parlot interpreted and compiled, Pidgin, Superpower, Sprache. Arithmetic baselines: hand-written recursive descent, Parlot interpreted and compiled, Pidgin. Each library has a folder per language rather than its own project. Corpora are generated from SplitMix64 seeds: JSON 1.3 KB, 103 KB and 1.03 MB, and 1,000 expressions. `tests/Creole.Benchmarks.Tests` pins the corpus hashes and checks that every baseline matches the reference result on every corpus. Added the on-demand `Benchmarks` workflow (`workflow_dispatch`). Parlot findings: its string literal rejects `\/`, so the corpus leaves that escape out; `Separated` fails on empty lists despite its doc comment; 1.5.12, the latest stable release, ships no build-time generator. Next: `/complete FND-05`; FND-06 measures on a fixed machine.
- 2026-10-18 (Building): The full suite ran with the one command (`dotnet run -c Release --project benchmarks/Creole.Benchmarks`, default job, exit 0) on an i9-13900H with .NET 11 RC 2. Indicative results on a laptop, not gate numbers: JSON large, System.Text.Json 180 MB/s, Parlot 122, Pidgin 21, Superpower 4.8, Sprache 2.0; arithmetic per expression, hand-written 86 ns, Parlot 308 ns, Pidgin `ExpressionParser` 3.4 µs. `ExpressionParser` turned out to be the expensive part, so a second Pidgin baseline with hand-built folds over `Many` was added (1.2 µs) to keep "near Pidgin speed" from being set against a slow configuration. Next: `/complete FND-05`.
- 2026-10-18 (Building): Simplified at the maintainer's request: corpora now come from seeded `System.Random` instead of a custom SplitMix64 generator; the pinned hashes catch any change in `Random`'s sequence across .NET versions. Corpus sizes are unchanged (1.3 KB, 104 KB, 1.03 MB), and new hashes are pinned. Also removed an unused interface, helper and script option. Next: `/complete FND-05`.
- 2026-10-18 (Building): Renamed corpora to inputs in code and docs, at the maintainer's request: `Corpora/` is now `Inputs/`, `JsonCorpus` and `ArithmeticCorpus` are `JsonInputs` and `ArithmeticInputs`, and the JSON benchmark parameter is `Input`. Next: `/complete FND-05`.
- 2026-10-18 (Building): Fixes from the critic's verification:
  - `--job short` or `--job dry` without `--filter` used to show BenchmarkDotNet's prompt and run nothing. The program now adds `--filter *` whenever no filter is given.
  - The README described a commit suffix on run folders that the maintainer's inlined config no longer adds; it now matches the code.
  - `bench-compare.py` now shows "from 0" when a zero-allocation benchmark starts allocating.
  - The README now says Parlot's generator exists only in preview packages.

## Outcome

- Code: `benchmarks/Creole.Benchmarks` and `tests/Creole.Benchmarks.Tests`. Docs: [benchmarks/README.md](../../../benchmarks/README.md). Also `scripts/bench-compare.py` and the on-demand workflow `.github/workflows/benchmarks.yml`.
- Evidence:
  - `dotnet run -c Release --project benchmarks/Creole.Benchmarks` ran all baselines with the default job and exited 0 (numbers in the progress notes).
  - After the final fixes, two `--job dry` runs without a filter each executed 23 benchmarks and wrote per-run JSON, CSV and Markdown reports. `bench-compare.py` matched the two runs benchmark by benchmark.
  - The agreement tests (each baseline gives the reference result on every input) passed locally and in CI on three OSes.
  - The critic verified the criterion independently.
- Deviations:
  - Baselines are folders in one project, not one project per library.
  - BenchmarkDotNet is 0.16.0-preview.2, because 0.15.8 does not support .NET 11.
  - The corpus omits the JSON `\/` escape, which Parlot's string literal rejects.
- Not measured: Parlot's source generator, which ships only as previews. FND-06 now has a scope item to decide whether the gates need it. Superpower and Sprache have JSON baselines only, as the whitepaper's plan specifies.
- The on-demand Benchmarks workflow has not run yet, because `workflow_dispatch` only works once the workflow is on the default branch.

