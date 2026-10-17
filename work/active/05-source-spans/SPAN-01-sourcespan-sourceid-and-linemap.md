# SPAN-01: SourceSpan, SourceId and LineMap

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 0 — Core |
| Status | Not started |

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
