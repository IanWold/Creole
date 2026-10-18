# BENCH-02: Full benchmark suite

| | |
| --- | --- |
| Epic | [Benchmarks](../../README.md#bench--benchmarks) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

The published benchmark suite across every engine.

## Scope

- JSON small to large, arithmetic expressions, indentation-sensitive language, large JSON-lines stream, inputs with many errors.
- Every Creole engine (interpreted, generated) against the competitors in the plan.
- Each benchmark grammar written side by side in Sprache for the migration guide.

## Acceptance criteria

- [ ] Results published with the 1.0 docs.

## Blocked by

- [FND-05 Benchmark harness](../01-foundations/FND-05-benchmark-harness.md)
- [ERR-07 Lexer recovery and cascade suppression](../04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md)
- [STRM-02 Restart protocol and ParseManyAsync](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)
- [GEN-05 Generated DFAs, scans and jump tables](../11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md)
- [GEN-06 Generated layers, modes, recovery and streaming](../11-source-generator/GEN-06-generated-layers-modes-recovery-and-streaming.md)

## Unblocks

- [BENCH-03 Phase 3 speed gate](BENCH-03-phase-3-speed-gate.md)
- [REL-03 Migration guides](../14-release/REL-03-migration-guides.md)
- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)
- [FUS-01 Fusion feasibility measurement](../16-layer-fusion/FUS-01-fusion-feasibility-measurement.md)
- [RES-01 Resumable async measurement](../17-resumable-async/RES-01-resumable-async-measurement.md)
- [GLL-04 General parsing measurements](../18-general-parsing-spike/GLL-04-general-parsing-measurements.md)

## Whitepaper references

- [§ Benchmark plan](../../../whitepaper.md#benchmark-plan)
