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

## Acceptance criteria

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
