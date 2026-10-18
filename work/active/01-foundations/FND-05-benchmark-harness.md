# FND-05: Benchmark harness

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Not started |

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

- [ ] Competitor baselines run with one command and produce comparable reports.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [FND-06 Fix numeric gate thresholds](../01-foundations/FND-06-fix-numeric-gate-thresholds.md)
- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)

## Whitepaper references

- [§ Benchmark plan](../../../whitepaper.md#benchmark-plan)
