# BENCH-04: Revisit string handling in the JSON benchmark grammar

| | |
| --- | --- |
| Epic | [Benchmarks](../../README.md#bench--benchmarks) |
| Phase | Phase 1 — Layers |
| Status | Not started |

## Summary

The Phase 0 gate's JSON grammar builds each string from chunks joined by `string.Concat`, as the Pidgin baseline does. Slicing the whole string and unescaping it in a `Select`, as Creole's docs recommend over collecting, took 0.80–0.95× the time in [BENCH-01](BENCH-01-phase-0-speed-gate.md). Phase 0 kept the grammar it was gated with. Decide which form the JSON benchmarks use from here on.

## Scope

- Measure the JSON grammar's strings built from chunks joined by `string.Concat` (as built) and sliced whole then unescaped in a `Select`, on small, medium and large JSON, against Pidgin and Parlot. Parlot uses its built-in string literal.
- Choose the form the JSON benchmarks use, weighing idiomatic Creole ([docs/engines/interpreter.md](../../../docs/engines/interpreter.md): take text with `Slice()`, not by collecting characters), each baseline's use of its own library's tools ([benchmarks/README.md](../../../benchmarks/README.md)), and comparability with the Phase 0 gate's results.
- If the measurements argue for a built-in string-literal combinator, record it as an idea. The whitepaper plans none.

## Acceptance criteria

- [ ] Both string forms are measured on small, medium and large JSON, median of three default-job runs, with their ratios to Pidgin and Parlot recorded on this card.
- [ ] The JSON benchmark grammar uses the chosen form, its baseline agreement test passes, and benchmarks/README.md's known differences say how Creole's strings are built and why.
- [ ] If the choice changes the Phase 0 gate's ratios, both sets are recorded here, so later gates compare like with like.

## Blocked by

- [BENCH-01 Phase 0 speed gate](BENCH-01-phase-0-speed-gate.md)

## Unblocks

- [BENCH-02 Full benchmark suite](BENCH-02-full-benchmark-suite.md)

## Whitepaper references

- [§ Benchmark plan](../../../whitepaper.md#benchmark-plan)
- [§ Text capture](../../../whitepaper.md#text-capture)
