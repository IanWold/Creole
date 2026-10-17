# BENCH-01: Phase 0 speed gate

| | |
| --- | --- |
| Epic | [Benchmarks](../../README.md#bench--benchmarks) |
| Phase | Phase 0 — Core |
| Status | Not started |
| Gate | Phase 0 gate |

## Summary

The Phase 0 gate: core tests green and speed near Pidgin.

## Scope

- Character-level JSON and arithmetic grammars on the interpreter.
- Compare against Pidgin and Parlot baselines from FND-05.

## Acceptance criteria

- [ ] The Phase 0 threshold from FND-06 is met, or the gap is understood and recorded.

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
