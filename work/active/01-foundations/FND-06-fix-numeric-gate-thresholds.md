# FND-06: Fix numeric gate thresholds

| | |
| --- | --- |
| Epic | [Foundations](../../README.md#fnd--foundations) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

The roadmap's gates ("near Pidgin speed", "measured win") become numbers once the harness has baselines.

## Scope

- Measure Pidgin and Parlot baselines on the FND-05 corpora.
- Propose a threshold for each gate: Phase 0 speed, Phase 1 bounded memory, fusion win, resumable-async win, incremental re-parse cost.
- Update the Roadmap table in the whitepaper.

## Acceptance criteria

- [ ] Every gate in the whitepaper's Roadmap has a numeric, measurable threshold.

## Blocked by

- [FND-05 Benchmark harness](../01-foundations/FND-05-benchmark-harness.md)

## Unblocks

- [BENCH-01 Phase 0 speed gate](../13-benchmarks/BENCH-01-phase-0-speed-gate.md)
- [INC-07 Incremental re-parsing gate](../15-incremental-reparsing/INC-07-incremental-re-parsing-gate.md)
- [FUS-01 Fusion feasibility measurement](../16-layer-fusion/FUS-01-fusion-feasibility-measurement.md)
- [RES-01 Resumable async measurement](../17-resumable-async/RES-01-resumable-async-measurement.md)

## Whitepaper references

- [§ Roadmap](../../../whitepaper.md#roadmap)
