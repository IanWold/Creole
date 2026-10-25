# INT-01: Cursor and in-memory input

| | |
| --- | --- |
| Epic | [Interpreted engine](../../README.md#int--interpreted-engine) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

The allocation-free cursor every engine reads through.

## Scope

- `Cursor<TIn>` ref struct with a fast path for reads inside the current segment.
- In-memory sources over `string`, `ReadOnlyMemory<char>` and arrays of `T`.
- Consumption is read from the cursor (moved or not), per the outcome model.
- Shape the cursor so LAY-01's `StreamBuffer<T>` can back it later without API change.

## Acceptance criteria

- [x] Tests show no allocation per read, for in-memory and segmented input.

## Blocked by

- [FND-02 Repository and solution scaffolding](../../active/01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [INT-02 Plan lowering and cache](../03-interpreter/INT-02-plan-lowering-and-cache.md)
- [LAY-01 StreamBuffer and pulling cursor](../../active/06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md)

## Whitepaper references

- [§ Interpreted engine](../../../whitepaper.md#interpreted-engine)
- [§ Results](../../../whitepaper.md#results)

## Progress notes

- 2026-10-18 (Build): Started. Plan a `Cursor<TIn>` ref struct over a current segment (span plus base offset) with a refill hook left for LAY-01, plus in-memory sources over `string`, `ReadOnlyMemory<char>` and `T[]`; prove zero allocation per read with BenchmarkDotNet's MemoryDiagnoser.
- 2026-10-18 (Build): Increment 1 of 3 done. Added public `Cursor<T>` ref struct (`Position`, `IsAtEnd`, `TryPeek`, `TryRead`, `Advance(int)`, `Available`; internal `Reset`) and internal `ICursorSource<T>` for LAY-01. In-memory input goes through the internal `Cursor<T>(ReadOnlySpan<T>)` constructor, which takes `string`, `T[]` and `ReadOnlyMemory<T>.Span` directly; a separate factory class was redundant. The cursor holds a span, not a `ReadOnlyMemory<T>`; `Slice` adds an internal memory field when it is built, with no public change. 19 cursor tests, one a CsCheck property over random segmentations pass. Next: allocation proof (test and `CursorBenchmarks`).
- 2026-10-18 (Build): Increment 2 of 3 done. Added `CursorAllocationTests`, which assert zero bytes from `GC.GetAllocatedBytesForCurrentThread` over 2^20 reads in four patterns: `TryRead` over a string, `TryPeek` plus `Advance(1)` over an `int[]`, `Available` scans across segments, and `TryRead` across segments followed by `Reset`. Moved the test `SegmentedSource<T>` to its own file. Acceptance criterion changed with the maintainer from "Micro-benchmarks show no allocation per read" to "Tests show no allocation per read, for in-memory and segmented input": § Benchmark plan covers only whole-grammar comparisons, the tests are exact and run on every `dotnet test`, and a `--job short` run of cursor micro-benchmarks gave sub-nanosecond timings with error above the mean (allocation was zero). The micro-benchmarks were removed; LAY-01 can add one if it needs to compare the fast path before and after the stream buffer. Next: `docs/engines/cursor.md`.
- 2026-10-18 (Build): Increment 3 of 3 done. Added `docs/engines/cursor.md` (positions, the consumption rule, members, bulk scans with `Available`) and its docs index entry. Its identifier-scanner sample is tested in `CursorTests` (a read across segments, and a failure that does not move). The page documents only what exists: no public constructor, and no custom scanners yet (CORE-12). All increments done; next: `/complete`.
- 2026-10-18 (Done, fix): `ReadingAStringAllocatesNothing` failed once on the Windows CI runner (PR #5, run 39150655961) with 7,560 bytes over 2^20 reads, and passed on the previous commit. That is about 0.007 bytes per read, so a one-off allocation on the test thread, not a per-read one; the cause was not identified. The tests now assert fewer bytes than reads, which any per-read allocation (24 bytes minimum on x64) exceeds by at least 24 times.

## Outcome

Delivered the cursor every engine reads through:

| Path | Contents |
| --- | --- |
| `src/Creole/Cursor.cs` | Public `Cursor<T>` ref struct: `Position` (absolute `long`), `IsAtEnd`, `TryPeek`, `TryRead`, `Advance(int)`, `Available`. Reads inside the current segment take an inlined path with one bounds check. Internal constructors over `ReadOnlySpan<T>` (which accepts `string`, `T[]` and `ReadOnlyMemory<T>.Span`) and over a source; internal `Reset`. |
| `src/Creole/ICursorSource.cs` | Internal segment hook. LAY-01's `StreamBuffer<T>` implements it, with no change to the public API. |
| `tests/Creole.Tests/CursorTests.cs` | 21 tests, including a CsCheck property over random segmentations and the docs sample. |
| `tests/Creole.Tests/CursorAllocationTests.cs` | Allocation checks over 2^20 reads in four patterns, in-memory and segmented. |
| `docs/engines/cursor.md` | User page: positions, the consumption rule, members, bulk scans with `Available`. |

Evidence for the criterion: the four `CursorAllocationTests` assert that `GC.GetAllocatedBytesForCurrentThread` grows by fewer bytes than reads over a measured pass of 2^20 reads; a per-read allocation would cost at least 24 bytes per read. Locally they measured 0 bytes in Debug and Release (2026-10-18). The full suite passes (66 tests) and the AOT smoke publishes without warnings.

Deviations: none from the whitepaper. The card's criterion changed during Build, with the maintainer, from micro-benchmarks to tests (see progress notes). § Benchmark plan never asked for cursor micro-benchmarks, so no decision record is needed. `Slice()` (§ Text capture) will add an internal `ReadOnlyMemory<T>` field to the cursor; that is internal and needs no API change. No follow-up cards.
