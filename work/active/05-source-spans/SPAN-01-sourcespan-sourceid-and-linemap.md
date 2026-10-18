# SPAN-01: SourceSpan, SourceId and LineMap

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

The public span type and on-demand line and column lookup.

## Scope

- `SourceSpan(SourceId, long Start, long End)` with `CharStart` and `CharEnd`.
- `SourceId` registry carrying source names.
- `LineMap` built once per source; binary search over line starts; columns in Unicode scalar values.

## Acceptance criteria

- [x] Line and column lookups are correct for LF, CRLF and CR inputs and for offsets past 2 GB.

## Blocked by

- [FND-02 Repository and solution scaffolding](../01-foundations/FND-02-repository-and-solution-scaffolding.md)

## Unblocks

- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)
- [ERR-03 Diagnostic rendering](../04-errors-and-recovery/ERR-03-diagnostic-rendering.md)
- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)

## Whitepaper references

- [§ Source spans and provenance](../../../whitepaper.md#source-spans-and-provenance)
- [§ Span rules](../../../whitepaper.md#span-rules)

## Progress notes

- 2026-10-18 (Build): Started. Build directly from § Source spans and provenance; settle the SourceId registry shape, CharStart/CharEnd defaults and how LineMap counts scalar-value columns in the /build plan.
- 2026-10-18 (Build): Increment 1 done. Added `SourceId`, `SourceRegistry` (per instance, thread-safe; `default` id is Unnamed) and `SourceSpan` (two constructors instead of `init` char offsets, so every span is validated), with tests and docs/concepts/spans.md. Build clean; 55 tests pass. Next: increment 2, `LineMap` with its builder, `LinePosition` (1-based), the CsCheck model test and the test past 2 GB.
- 2026-10-18 (Build): Increment 2 done. Added `LineMap` (line starts and surrogate-pair starts as `long[]`, two binary searches per lookup), `LineMap.Builder` for chunked input and `LinePosition` (1-based), with docs in docs/concepts/spans.md. Tests: examples for LF, CRLF, CR and mixed endings; a CsCheck model test under random chunking; a 2.2 GB chunked test (about 210 ms). 83 tests pass. Next: `/complete`.

## Outcome

Delivered:

- `SourceId`, `SourceRegistry` and `SourceSpan` in [src/Creole/](../../../src/Creole/). The registry is per instance and thread-safe; `default(SourceId)` is `SourceId.Unnamed`.
- `LineMap`, `LineMap.Builder` and `LinePosition`. The map keeps line starts and surrogate-pair starts as `long[]` and never the text; lookups are 1-based and count columns in Unicode scalar values. Line breaks are LF, CRLF and CR only.
- Tests: [SourceSpanTests.cs](../../../tests/Creole.Tests/SourceSpanTests.cs) and [LineMapTests.cs](../../../tests/Creole.Tests/LineMapTests.cs), including a CsCheck model test under random chunking and a 2.2 GB chunked test (about 210 ms).
- Docs: [docs/concepts/spans.md](../../../docs/concepts/spans.md).

API detail settled here, not a design change: `SourceSpan` takes character offsets through a five-argument constructor instead of the sketch's `init` properties, so every span is validated and `with` cannot leave the two offset pairs inconsistent.

Not in scope: the streaming line counter that captures positions as diagnostics are raised belongs to the root layer (§ Streaming); snippet rendering and grapheme-aware carets belong to ERR-03.
