# ERR-03: Diagnostic rendering

| | |
| --- | --- |
| Epic | [Errors, diagnostics and recovery](../../README.md#err--errors-diagnostics-and-recovery) |
| Phase | Phase 0 — Core |
| Status | Done |

## Summary

Render diagnostics against the original text in the format shown in the whitepaper.

## Scope

- `error[layer]: message`, `--> source:line:col`, gutter, carets and notes.
- Columns in Unicode scalar values; grapheme-aware caret alignment.
- Ordering and grouping of many diagnostics per file.

## Acceptance criteria

- [x] Snapshot tests cover tabs, wide characters, combining marks and CRLF line endings.

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
- 2026-10-19 (Build): Increment 2 done (refactor, no behaviour change): `ParseError.ToString()` now builds its text from two internal helpers, `DescribeUnexpected()` and `DescribeExpected()`, so the renderer can share the wording. The existing `ToString`/`Render()` tests pin the output; full suite 280 pass. Next: increment 3, `SourceText`, `Diagnostics.Render(params ReadOnlySpan<SourceText>)`, `LineMap.GetLineStart` and docs.
- 2026-10-19 (Build): Increment 3 done. Public API: `SourceText` (sealed class, not the planned record, so its cached `LineMap` stays out of equality) and `Diagnostics.Render(params ReadOnlySpan<SourceText>)`. The internal `DiagnosticRenderer` writes the headline, location, gutter, carets and notes. `LineMap.GetLineStart` is internal. `Render` throws `ArgumentException` for a missing, duplicate or null text and for a span past its text. `DiagnosticRenderingTests` (19) cover the whitepaper sample exactly, tabs, CJK, emoji, combining marks, CRLF and CR, empty spans at the end of a line and the end of input, multi-line spans, a two-digit gutter, two files plus `Unnamed`, custom messages, control characters and the error cases, plus a CsCheck shape property. Mutations (wide as 1, tab as 1, no CR strip) each failed their tests. The property at first missed the CR one, because sanitizing hides raw CRs, so it now also rejects the CR and LF Control Pictures. Full suite 299 pass; AOT smoke clean. Docs: docs/concepts/errors.md § Rendering against the source, a pointer in spans.md, the docs index. Follow-ups, not done: bidi controls and grammar-written `Display` strings are not sanitized; multi-line spans show only their first line. Next: maintainer review, then `/complete ERR-03`.
- 2026-10-19 (Build): `/complete` at 7952d27, after merging main (CORE-03). The criterion is met: `TabsAreExpandedToStopsOfFour`, `WideCharactersTakeTwoColumns`, `EmojiTakeTwoColumnsWhateverTheirLength`, `CombiningMarksTakeNoColumnButCountInTheLocation` and `CrlfLineEndingsAreNotShown` each pass, and so do all 65 rendering and width tests. Full suite: 398 pass, 0 fail, 1 skipped (CORE-03's `Case` test, which waits on CORE-05). AOT smoke clean.

## Outcome

Delivered:

- `Diagnostics.Render(params ReadOnlySpan<SourceText>)` in [Diagnostics.cs](../../../src/Creole/Errors/Diagnostics.cs) writes the format in [§ The error model](../../../whitepaper.md#the-error-model): `error[layer]:` headline, ` --> name:line:col`, gutter, source line, carets with a label, and `= note:` lines. Errors come in span order, grouped by source, with a blank line between them. It throws `ArgumentException` for a missing, duplicate or null text and for a span past the end of its text. The one-line `Render()` is unchanged.
- [SourceText](../../../src/Creole/SourceText.cs): a source's text, id and name, with its `LineMap` built on first use. It is a sealed class, so the cached map stays out of equality.
- Internal: [DiagnosticRenderer.cs](../../../src/Creole/Errors/DiagnosticRenderer.cs); [DisplayWidth.cs](../../../src/Creole/Errors/DisplayWidth.cs), which places carets by grapheme cluster (`StringInfo`), counts W/F, regional indicators and U+FE0F as two columns and Mn, Me or Cf first as zero, expands tabs to stops of 4, and shows C0 and DEL as Control Pictures and C1 and lone surrogates as U+FFFD; `LineMap.GetLineStart`; and `ParseError.DescribeUnexpected()`/`DescribeExpected()`, which the one-line form shares.
- [EastAsianWidth.g.cs](../../../src/Creole/Errors/EastAsianWidth.g.cs), generated from Unicode 16.0.0 (the runtime's version) by [scripts/gen-east-asian-width.py](../../../scripts/gen-east-asian-width.py). .NET has no East Asian Width API.
- Tests: [DiagnosticRenderingTests.cs](../../../tests/Creole.Tests/DiagnosticRenderingTests.cs), with expected strings written in the tests and a CsCheck shape property, and [DisplayWidthTests.cs](../../../tests/Creole.Tests/DisplayWidthTests.cs). No snapshot library: Verify's licence rules it out ([TOOL-01](../12-grammar-author-tooling/TOOL-01-creoletesting-assertions.md)). Mutations (wide as 1, tab as 1, no CR strip) each failed their tests.
- Docs: [docs/concepts/errors.md](../../../docs/concepts/errors.md) § Rendering against the source (format, `SourceText`, the alignment table and what it does not handle), and a pointer in [docs/concepts/spans.md](../../../docs/concepts/spans.md).

No design deviations. Calls the whitepaper left open: the headline lists what was expected and the carets are labelled with what was found; a span over several lines is shown on its first line; tab stops are 4.

Not covered, documented in docs/concepts/errors.md: East Asian Ambiguous width in CJK terminals, terminal-specific emoji widths, Indic conjuncts (RC 2 does not apply GB9c), and right-to-left lines.

Follow-ups, no new cards:

- [LAY-05](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md) decides how Example A's `result.Diagnostics.Render()` gets the source text (scope line added).
- Bidi controls and grammar-written strings (layer, `Display`, messages, notes) are printed as given. Only the source line is cleaned.
- [STRM-03](../08-streaming/STRM-03-streaming-diagnostics.md) can render from captured lines by giving `DiagnosticRenderer` a line in place of a `SourceText`.
- Regenerate the width table with the script when the runtime's Unicode version changes; `EastAsianWidthTableMatchesTheRuntimeUnicodeVersion` pins it.
