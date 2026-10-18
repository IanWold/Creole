# BENCH-01: Phase 0 speed gate

| | |
| --- | --- |
| Epic | [Benchmarks](../../README.md#bench--benchmarks) |
| Phase | Phase 0 — Core |
| Status | Not started |
| Gate | Phase 0 gate |

## Summary

The Phase 0 gate: core tests green, and the interpreter at least as fast as Pidgin and within 1.5× of Parlot.

## Scope

- Character-level JSON and arithmetic grammars on the interpreter.
- Compare against the FND-05 Pidgin (`PidginFolds` for arithmetic) and stable Parlot baselines in the same run; median of three runs on the gate machine ([ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md)).

## Acceptance criteria

- [ ] The Phase 0 speed thresholds in [ADR-0003](../../docs/decisions/0003-numeric-gate-thresholds.md) are met (time: geometric mean ≤ 1.0× Pidgin, no benchmark above 1.25× Pidgin, geometric mean ≤ 1.5× Parlot; allocations ≤ Pidgin's on every benchmark), or the gap is understood and recorded.

## Blocked by

- [FND-06 Fix numeric gate thresholds](../01-foundations/FND-06-fix-numeric-gate-thresholds.md)
- [INT-05 Interpreter lowering rewrites](../03-interpreter/INT-05-interpreter-lowering-rewrites.md)
- [INT-06 DFA compiler for regular subtrees](../03-interpreter/INT-06-dfa-compiler-for-regular-subtrees.md)
- [INT-08 Single-layer Parse API](../03-interpreter/INT-08-single-layer-parse-api.md)
- [ERR-02 Furthest-failure tracking and labels](../04-errors-and-recovery/ERR-02-furthest-failure-tracking-and-labels.md)

## Unblocks

Nothing directly.

## Whitepaper references

- [§ Performance design and benchmark plan](../../../whitepaper.md#performance-design-and-benchmark-plan)
- [§ Roadmap](../../../whitepaper.md#roadmap)
