# FUS-01: Fusion feasibility measurement

| | |
| --- | --- |
| Epic | [Layer fusion (1.x)](../../README.md#fus--layer-fusion-1x) |
| Phase | Phase 4 — 1.x releases |
| Size | M |
| Status | Not started |

## Summary

Fusion ships only if it measurably beats unfused generated code on .NET.

## Scope

- Hand-write a fused JSON parser and compare it to the generated unfused one.
- Go or no-go decision against the FND-06 threshold.

## Acceptance criteria

- [ ] Decision recorded with numbers.

## Blocked by

- [FND-06 Fix numeric gate thresholds](../01-foundations/FND-06-fix-numeric-gate-thresholds.md)
- [GEN-05 Generated DFAs, scans and jump tables](../11-source-generator/GEN-05-generated-dfas-scans-and-jump-tables.md)
- [BENCH-02 Full benchmark suite](../13-benchmarks/BENCH-02-full-benchmark-suite.md)

## Unblocks

- [FUS-02 Fused code emission](../16-layer-fusion/FUS-02-fused-code-emission.md)

## Whitepaper references

- [§ Layer fusion](../../../whitepaper.md#layer-fusion)
- [§ Risks](../../../whitepaper.md#risks)
