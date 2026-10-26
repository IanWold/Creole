# BENCH-03: Phase 3 speed gate

| | |
| --- | --- |
| Epic | [Benchmarks](../../README.md#bench--benchmarks) |
| Phase | Phase 3 — Staging |
| Status | Not started |
| Gate | Phase 3 gate |

## Summary

The Phase 3 speed gate: the generated engine is as fast as Parlot.

## Scope

- Run the Phase 0 benchmarks (character-level JSON small, medium and large, and arithmetic) on the generated engine.
- Compare against stable Parlot in the same run, median of three runs on the gate machine. `Parlot.SourceGenerator` is stable from 2.0.0, so add Parlot's generated JSON and arithmetic parsers to the harness (left out of [FND-10](../01-foundations/FND-10-move-the-parlot-baselines-to-200.md)), checked by `tests/Creole.Benchmarks.Tests` like the other baselines, compare against them, and record the Parlot version.
- Before the gate runs, draft a record with `/decide` that supersedes [ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md). It restates the thresholds and how speed gates are measured, but drops the consequence that hosted runners catch regressions of 10% or more: no CI workflow runs the benchmarks ([BENCH-01](BENCH-01-phase-0-speed-gate.md), 2026-10-26). It also records the gate procedure BENCH-01 built: each run's `run.json` with its commit and baseline versions, the `Gate` benchmark category, and `scripts/bench-gate.py`.
- `scripts/bench-gate.py` checks the Phase 3 threshold as well as Phase 0's, and gives a verdict only for runs that follow the protocol. Today it prints "pass", with warnings, for runs with uncommitted changes, another job or no `run.json`, and it does not compare Pidgin versions or arguments ([BENCH-01](BENCH-01-phase-0-speed-gate.md) Outcome).

## Acceptance criteria

- [ ] A record superseding ADR-0003 is drafted, and the gate is measured under the accepted one.
- [ ] `bench-gate.py` checks the Phase 3 threshold and refuses a verdict on a set of runs that breaks the protocol; a run set made for the purpose shows it refusing.
- [ ] Generated engine time is at most 1.0× Parlot's, as a geometric mean over the Phase 0 benchmarks ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)), or the gap is understood and recorded.

## Blocked by

- [FND-06 Fix numeric gate thresholds](../01-foundations/FND-06-fix-numeric-gate-thresholds.md)
- [FND-10 Move the Parlot baselines to 2.0.0](../01-foundations/FND-10-move-the-parlot-baselines-to-200.md)
- [BENCH-02 Full benchmark suite](BENCH-02-full-benchmark-suite.md)

## Unblocks

- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)

## Whitepaper references

- [§ Performance design and benchmark plan](../../../whitepaper.md#performance-design-and-benchmark-plan)
- [§ Roadmap](../../../whitepaper.md#roadmap)

## Artifacts

- [Research: Parlot 2.0 baselines and generator](../../docs/research/2026-10-24-parlot-2-0-baselines-and-generator.md) `Parlot.SourceGenerator` 2.0.0 is stable; how its generator works, what generated parsers support, and how the harness can carry them

## Progress notes

- 2026-10-24 (Not started): FND-10 research found `Parlot.SourceGenerator` 2.0.0 stable. Generated parsers need `.parlot.cs` factories and a `partial` entry point, and have no Parlot runtime dependency (note, findings 13–19). Next: unchanged; waits on BENCH-02.
- 2026-10-26 (Not started): At the maintainer's request, scope gains a record superseding ADR-0003. Its consequence about hosted runners is stale, and accepted records cannot be edited. Next: unchanged; waits on BENCH-02.
- 2026-10-26 (Not started): Scope gains a stricter `bench-gate.py`, from BENCH-01's completion review. Next: unchanged; waits on BENCH-02.
