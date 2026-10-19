# ERR-03: Diagnostic rendering

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 0 — Core |
| Status | Building |

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

## Progress notes

- 2026-10-19 (Build): Started. The format is settled by § The error model; build a renderer over `Diagnostics` with grapheme clusters from `StringInfo` and an East Asian Width table generated from the UCD. Next: `/build` plan for approval.
- 2026-10-19 (Build): Plan approved: increment 1, display width; increment 2, a refactor that shares the expected and unexpected wording from `ParseError.ToString()`; increment 3, `SourceText`, `Diagnostics.Render(params ReadOnlySpan<SourceText>)` and docs. Checked on RC 2: `StringInfo` gives UAX #29 clusters, but GB9c is not applied (क्ष splits), and .NET has no East Asian Width API. The runtime's Unicode data is 16.0.0 (runtime-assets 13b701d). Added a scope line to LAY-05 about where `Render()` gets the source text.
- 2026-10-19 (Build): Increment 1 done: `scripts/gen-east-asian-width.py` generates `src/Creole/Errors/EastAsianWidth.g.cs` from Unicode 16.0.0 (122 W/F ranges, binary search over a constant span). Internal `DisplayWidth` measures clusters (W/F, regional indicators or U+FE0F give 2; Mn, Me or Cf first gives 0; otherwise 1), expands tabs to stops of 4, shows C0 and DEL as Control Pictures and C1 and lone surrogates as U+FFFD, and places carets with whole-cluster snapping. `DisplayWidthTests` 46 pass; full suite 280 pass; AOT smoke publishes clean. Next: increment 2, the refactor.
