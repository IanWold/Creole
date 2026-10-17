# ERR-03: Diagnostic rendering

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 0 — Core |
| Status | Not started |

## Summary

Render diagnostics against the original text in the format shown in the whitepaper.

## Scope

- `error[layer]: message`, `--> source:line:col`, gutter, carets and notes.
- Columns in Unicode scalar values; grapheme-aware caret alignment.
- Ordering and grouping of many diagnostics per file.

## Acceptance criteria

- [ ] Snapshot tests cover tabs, wide characters, combining marks and CRLF line endings.

## Blocked by

- [ERR-01 Error and result types](../04-errors-and-recovery/ERR-01-error-and-result-types.md)
- [SPAN-01 SourceSpan, SourceId and LineMap](../05-source-spans/SPAN-01-sourcespan-sourceid-and-linemap.md)

## Unblocks

- [STRM-03 Streaming diagnostics](../08-streaming/STRM-03-streaming-diagnostics.md)
- [TOOL-01 Creole.Testing assertions](../12-grammar-author-tooling/TOOL-01-creoletesting-assertions.md)

## Whitepaper references

- [§ The error model](../../../whitepaper.md#the-error-model)
- [§ Source spans and provenance](../../../whitepaper.md#source-spans-and-provenance)
