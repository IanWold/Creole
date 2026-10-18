# SPAN-01: SourceSpan, SourceId and LineMap

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 0 — Core |
| Status | Building |

## Summary

The public span type and on-demand line and column lookup.

## Scope

- `SourceSpan(SourceId, long Start, long End)` with `CharStart` and `CharEnd`.
- `SourceId` registry carrying source names.
- `LineMap` built once per source; binary search over line starts; columns in Unicode scalar values.

## Acceptance criteria

- [ ] Line and column lookups are correct for LF, CRLF and CR inputs and for offsets past 2 GB.

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
