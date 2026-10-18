# INT-01: Cursor and in-memory input

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

The allocation-free cursor every engine reads through.

## Scope

- `Cursor<TIn>` ref struct with a fast path for reads inside the current segment.
- In-memory sources over `string`, `ReadOnlyMemory<char>` and arrays of `T`.
- Consumption is read from the cursor (moved or not), per the outcome model.
- Shape the cursor so LAY-01's `StreamBuffer<T>` can back it later without API change.

## Acceptance criteria

- [ ] Tests show no allocation per read, for in-memory and segmented input.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [INT-02 Plan lowering and cache](../03-interpreter/INT-02-plan-lowering-and-cache.md)
- [LAY-01 StreamBuffer and pulling cursor](../06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md)

## Whitepaper references

- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
- [§ Results](../../../whitepaper.md#results)

## Progress notes

- 2026-10-18 (Build): Started. Plan a `Cursor<TIn>` ref struct over a current segment (span plus base offset) with a refill hook left for LAY-01, plus in-memory sources over `string`, `ReadOnlyMemory<char>` and `T[]`; prove zero allocation per read with BenchmarkDotNet's MemoryDiagnoser.
- 2026-10-18 (Build): Increment 1 of 3 done. Added public `Cursor<T>` ref struct (`Position`, `IsAtEnd`, `TryPeek`, `TryRead`, `Advance(int)`, `Available`; internal `Reset`) and internal `ICursorSource<T>` for LAY-01. In-memory input goes through the internal `Cursor<T>(ReadOnlySpan<T>)` constructor, which takes `string`, `T[]` and `ReadOnlyMemory<T>.Span` directly; a separate factory class was redundant. The cursor holds a span, not a `ReadOnlyMemory<T>`; `Slice` adds an internal memory field when it is built, with no public change. 19 cursor tests, one a CsCheck property over random segmentations pass. Next: allocation proof (test and `CursorBenchmarks`).
- 2026-10-18 (Build): Increment 2 of 3 done. Added `CursorAllocationTests`, which assert zero bytes from `GC.GetAllocatedBytesForCurrentThread` over 2^20 reads in four patterns: `TryRead` over a string, `TryPeek` plus `Advance(1)` over an `int[]`, `Available` scans across segments, and `TryRead` across segments followed by `Reset`. Moved the test `SegmentedSource<T>` to its own file. Acceptance criterion changed with the maintainer from "Micro-benchmarks show no allocation per read" to "Tests show no allocation per read, for in-memory and segmented input": § Benchmark plan covers only whole-grammar comparisons, the tests are exact and run on every `dotnet test`, and a `--job short` run of cursor micro-benchmarks gave sub-nanosecond timings with error above the mean (allocation was zero). The micro-benchmarks were removed; LAY-01 can add one if it needs to compare the fast path before and after the stream buffer. Next: `docs/engines/cursor.md`.
- 2026-10-18 (Build): Increment 3 of 3 done. Added `docs/engines/cursor.md` (positions, the consumption rule, members, bulk scans with `Available`) and its docs index entry. Its identifier-scanner sample is tested in `CursorTests` (a read across segments, and a failure that does not move). The page documents only what exists: no public constructor, and no custom scanners yet (CORE-12). All increments done; next: `/complete`.
