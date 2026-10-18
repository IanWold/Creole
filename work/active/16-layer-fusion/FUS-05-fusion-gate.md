# FUS-05: Fusion gate

| | |
| --- | --- |
| Epic | [Layer fusion (1.x)](../../README.md#fus--layer-fusion-1x) |
| Phase | Phase 4 — 1.x releases |
| Status | Not started |
| Gate | Fusion gate |

## Summary

Fused engines join the differential tests and the benchmark gate.

## Scope

- Add fused engines to GEN-08's differential tests.
- Re-run benchmarks against the threshold.

## Acceptance criteria

- [ ] Agreement holds, and fused code is at least 1.2× faster than the best unfused generated code (geometric mean over JSON, arithmetic and Example B), with no benchmark slower ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)).

## Blocked by

- [GEN-08 Differential engine-agreement tests](../11-source-generator/GEN-08-differential-engine-agreement-tests.md)
- [FUS-03 Fusion with modes and custom scanners](../16-layer-fusion/FUS-03-fusion-with-modes-and-custom-scanners.md)
- [FUS-04 Fuse through stateless item layers](../16-layer-fusion/FUS-04-fuse-through-stateless-item-layers.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Layer fusion](../../../whitepaper.md#layer-fusion)
