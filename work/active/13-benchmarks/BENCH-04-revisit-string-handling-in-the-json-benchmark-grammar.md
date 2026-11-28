# BENCH-04: Revisit string handling in the JSON benchmark grammar

| | |
| --- | --- |
| Epic | [Benchmarks](../../README.md#bench--benchmarks) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

The Phase 0 gate's JSON grammar builds each string from chunks joined by `string.Concat`, as the Pidgin baseline does. Slicing the whole string and unescaping it in a `Select`, as Creole's docs recommend over collecting, took 0.80–0.95× the time in [BENCH-01](BENCH-01-phase-0-speed-gate.md). Phase 0 kept the grammar it was gated with. Decide which form the JSON benchmarks use from here on.

## Scope

- Measure the JSON grammar's strings built from chunks joined by `string.Concat` (as built) and sliced whole then unescaped in a `Select`, on small, medium and large JSON, against Pidgin and Parlot. Parlot uses its built-in string literal.
- Choose the form the JSON benchmarks use, weighing idiomatic Creole ([docs/engines/interpreter.md](../../../docs/engines/interpreter.md): take text with `Slice()`, not by collecting characters), each baseline's use of its own library's tools ([benchmarks/README.md](../../../benchmarks/README.md)), and comparability with the Phase 0 gate's results.
- If the measurements argue for a built-in string-literal combinator, record it as an idea. The whitepaper plans none.

## Acceptance criteria

- [x] Both string forms are measured on small, medium and large JSON, median of three default-job runs, with their ratios to Pidgin and Parlot recorded on this card.
- [x] The JSON benchmark grammar uses the chosen form, its baseline agreement test passes, and benchmarks/README.md's known differences say how Creole's strings are built and why.
- [x] If the choice changes the Phase 0 gate's ratios, both sets are recorded here, so later gates compare like with like.

## Blocked by

- [BENCH-01 Phase 0 speed gate](BENCH-01-phase-0-speed-gate.md)

## Unblocks

- [BENCH-02 Full benchmark suite](BENCH-02-full-benchmark-suite.md)

## Whitepaper references

- [§ Benchmark plan](../../../whitepaper.md#benchmark-plan)
- [§ Text capture](../../../whitepaper.md#text-capture)

## Progress notes

- 2026-11-27 (Building): Started. Build, not Research: the card names both string forms, the measurement protocol and the criteria, and the choice is benchmark method recorded here and in benchmarks/README.md, so no decision record. Approach: add the sliced form beside the chunked one in `CreoleJson`, run three default-job runs of both against Pidgin and Parlot, then choose. Next: plan with `/build`.
- 2026-11-27 (Building): Plan presented, awaiting approval. Increment 1: a copy of `CreoleJson` with sliced strings (`CreoleSlicedJson`), timed as `CreoleSliced` in the `Gate` category so both forms run in the same invocations, with agreement tests; then three default-job gate runs on the committed tree. Increment 2: `CreoleJson` takes the chosen form, the other is deleted, and benchmarks/README.md's known differences are updated. A sliced Pidgin variant is left out: Creole is at 0.24× Pidgin, so it could not change a gate outcome. Next: the maintainer approves or revises the plan.
- 2026-11-27 (Building): Increment 1 built, not yet measured. `CreoleSlicedJson` is `CreoleJson` with its strings sliced whole and unescaped in a `Select`; its escape check builds no value. `JsonBenchmarks.CreoleSliced` carries the `Gate` category. `CreoleSliced` joins the System.Text.Json agreement test on all three inputs, and `CreoleJsonUnescapesEveryEscape` checks both forms on every escape, a surrogate pair and a string longer than the stack buffer; mapping `\b` wrongly fails it. Benchmark tests: 49 of 49 pass. benchmarks/README.md's gate paragraph names `CreoleSliced`. Next: the maintainer commits, then three default-job gate runs on the clean tree.
- 2026-11-27 (Building): Increment 1 measured at 28f8a6b, clean tree: three default-job gate runs (20261128-003352, -003915, -004410), Parlot 2.4.0, Pidgin 3.5.1. Medians, chunked then sliced: JSON small 0.20× and 0.18× Pidgin, 1.92× and 1.69× Parlot; medium 0.23× and 0.20× Pidgin, 1.81× and 1.57× Parlot; large 0.30× and 0.21× Pidgin, 2.13× and 1.55× Parlot. The sliced form takes 0.89×, 0.87× and 0.73× the chunked form's time (each run within 0.02 of the median) and allocates 0.52× as much: 6.7 KB, 492 KB and 4.86 MB against 12.9 KB, 946 KB and 9.35 MB. Arithmetic has no strings: 0.24× Pidgin, 1.19× Parlot. Gate geometric means: chunked 0.24× Pidgin and 1.72× Parlot, which fails 1.5×; sliced 0.21× and 1.49×. The chunked figure matches ERR-04's and STRM-01's 1.72–1.74×: the gate already failed because the harness builds against a newer Parlot than the 2.0.0 that BENCH-01 measured (1.37×), and `run.json` reports 2.4.0 as `2.0.0-preview` (STRM-01's finding). `bench-gate.py` gave the chunked ratios; a scratch script over the same runs gave the sliced ones. Fixed a test defect: `CreoleJsonUnescapesEveryEscape` held é, 中 and 😀 as literal characters, not `\u` escapes, so it tested no `\u` escape, surrogate pair or uppercase hex digit. It now holds `\u00e9\u4E2D\uD83D\uDE00`, and dropping `Range('A', 'F')` from the sliced grammar fails it. Benchmark tests: 49 of 49 pass. Next: the maintainer chooses the form; increment 2 builds it.
- 2026-11-27 (Building): Increment 2 done: the maintainer chose the sliced form. `CreoleJson` now slices each string whole and unescapes it in a `Select`; `CreoleSlicedJson` and the `CreoleSliced` benchmark are gone. `CreoleJsonUnescapesEveryEscape` is a fact on `CreoleJson`, and dropping `Range('A', 'F')` from it still fails the test. Benchmark tests: 45 of 45 pass, including the System.Text.Json agreement test on all three inputs; the Release build has 0 warnings. benchmarks/README.md's known differences say how Creole's strings are built, why, and that the Phase 0 gate was measured with the chunked form; its gate paragraph no longer names `CreoleSliced`. The gate's ratios change with the choice: increment 1's note records both sets from the same runs (chunked 1.72×, sliced 1.49× Parlot 2.4.0), and BENCH-01 records the chunked form against Parlot 2.0.0 (1.37×). No idea recorded for a built-in string literal: after slicing, JSON is 1.55–1.69× Parlot against arithmetic's 1.19×, but these runs do not show how much of that gap is strings. Every acceptance criterion now appears met. Next: the maintainer commits, opens the PR and runs `/complete`.
