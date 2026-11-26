# SPAN-06: Byte sources and the decoding layer

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Done |

## Summary

Decoding is just another layer, and spans carry both byte and character offsets.

## Scope

- A `Layer<byte, char>` decoder, UTF-8 at minimum, with diagnostics for invalid sequences.
- Spans carry byte offsets and character offsets when a decoding layer exists; text-rooted pipelines skip the character fields.
- Every decoded character covers at least one byte, so real input never gets a zero-width span, which would make it synthetic ([ADR-0034](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md) point 6). Test it.
- Store the four offsets densely in the stream buffer. Until then, a span whose character offsets differ from its root offsets is kept whole beside its segment ([SPAN-02](SPAN-02-out-of-band-span-storage.md)).

## Acceptance criteria

- [x] Diagnostics on a UTF-8 byte source report correct byte offsets and correct line and column.

## Blocked by

- [SPAN-02 Out-of-band span storage](../05-source-spans/SPAN-02-out-of-band-span-storage.md)
- [LAY-03 Layer abstraction and Emit](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)

## Unblocks

- [STRM-02 Restart protocol and ParseManyAsync](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)

## Whitepaper references

- [§ Source spans and provenance](../../../whitepaper.md#source-spans-and-provenance)
- [§ Sources](../../../whitepaper.md#sources)

## Artifacts

- [ADR-0034: A synthetic item is one whose span is zero-width, and no flag is stored](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md)

## Progress notes

- 2026-11-24 (Build): Started. The whitepaper fixes the design (decoding is a `Layer<byte, char>`; spans carry byte and character offsets; rendering already reads `CharStart` and `CharEnd`), and nothing it depends on is open. Points for the `/build` plan: what an invalid sequence does (fail the step as any layer does today, or replace with U+FFFD and continue, which needs ERR-07's layer recovery and a decision); whether a leading BOM is stripped; character offsets in UTF-16 code units; the decoder's public name and whether it is UTF-8 only; the decoder building its own errors' character offsets, since the byte root's spans have none; the dense four-offset layout beside each segment instead of the wide list; and how the criterion is tested before LAY-05 gives byte pipelines a public `Parse` and a source text to render against. Next: `/build`.
- 2026-11-24 (Build): Plan agreed with the maintainer. Calls: an invalid sequence fails the decoder's step, named `decoder`, with its byte span over the maximal invalid subpart and a zero-width character span where it would have decoded; replacing it with U+FFFD and continuing waits for ERR-07's layer recovery. A leading BOM is stripped and belongs to no item. Character offsets count UTF-16 code units, and a surrogate pair's two items share the step's whole span. The API is `Layer.Utf8()`, a `Layer<byte, char>` with no `name`, UTF-8 only, one scalar value per step. Tests run through a byte `LayerRun` and render against a `SourceText` of the decoded text until LAY-05 and STRM-03 supply it. Increments: 1 dense four-offset storage; 2 `Layer.Utf8()`; 3 the criterion's end-to-end tests and follow-up notes on LAY-05, STRM-03 and ERR-07.
- 2026-11-24 (Build): Increment 1 done. `StreamBuffer<T>` stores a span's character offsets densely: a segment rents a `PackedSpan[]` of character offsets, counted from a character base, when it stores its first span whose character offsets differ from its root offsets, and marks the spans before it as equal to their root offsets. A segment over text never rents one, so the 32-byte slot and the text path are unchanged. The wide list now holds only spans out of range in either unit or from another source. `SpanStorageTests` (5): the model test also generates shifted character offsets; the allocation test is a theory over text and decoded spans past 4 GB, and checks that only decoded segments store character offsets; the explicit test adds decoded spans that fit, whose character offset or length does not, and a text span after them. A mutation sweep (always rent, no backfill, character base at the root base, each range check dropped, the array not returned, no mark for spans equal to their root offsets, `Unpack` ignoring the array) fails at least one test each. Full suite: 1,664 passed, 0 warnings; AOT smoke publish and run OK. No `docs/` change, since nothing public changed. Next: increment 2, `Layer.Utf8()`.
- 2026-11-24 (Build): Increment 2 done. `Layer.Utf8()` returns the `Utf8Layer` singleton, a `Layer<byte, char>` in [src/Creole/Layers/Utf8Layer.cs](../../../src/Creole/Layers/Utf8Layer.cs). Each step reads one byte at a time and decodes one scalar value with `Rune.DecodeFromUtf8`, so it never reads past the byte that ends or breaks a sequence. A leading BOM is a step that produces nothing. Outputs take byte offsets and the source from their input's spans and count character offsets themselves; a surrogate pair's halves share the step's span, with group indices 0 and 1. An invalid or truncated sequence throws `LayerFailedException` with an error named `decoder`, `invalid UTF-8 byte FF` or `invalid UTF-8 bytes E2 82`, over the maximal subpart, zero-width in characters. Input is released as `Select` releases it. `LayerRun` gained overloads over any input type. Tests: [Utf8LayerTests.cs](../../../tests/Creole.Tests/Utf8LayerTests.cs) (14 cases): CsCheck properties for valid text (random scalar values with CR and LF, an optional BOM, segment sizes 1–5) and for random byte fragments against a strict `UTF8Encoding`, whose `DecoderFallbackException` gives the first invalid sequence's index and bytes; explicit cases for spans, BOMs, empty input, eight invalid sequences and a lexer above the decoder. `LayerRetentionTests` runs the decoder over released, overwritten segments of 1 and 2 bytes. A mutation sweep (BOM not skipped, U+FEFF skipped anywhere, the error over every byte read, a one-wide error, each surrogate half with its own span, group always 0, one character per step, the step start after the step, release at the step start, no release, one message form) fails at least one test each. Docs: [layers.md](../../../docs/concepts/layers.md) § Decoding bytes, [spans.md](../../../docs/concepts/spans.md) and the docs index. Full suite: 1,680 passed, 0 warnings; AOT smoke publish and run OK. Next: increment 3, the criterion's end-to-end tests and the follow-up notes.
- 2026-11-24 (Build): Increment 3 done. [ByteSourceDiagnosticsTests.cs](../../../tests/Creole.Tests/ByteSourceDiagnosticsTests.cs) (3) runs UTF-8 bytes with a BOM, CRLF line ends and characters of two, three and four bytes through `Layer.Utf8()`, a lexer and a parser reading the lexer's buffer with a failure log. It checks the error from each layer: its span in bytes and characters, and its rendered `line:column` against a `SourceText` of the decoded text. Decoder: bytes 19..20, characters 14..14, 2:4. Lexer: bytes 26..29, characters 20..21, 2:10. Parser: bytes 30..31, characters 24..25, 2:13, after an emoji that is two characters and one column. Each value was worked out by hand before the first run. Mutations fail them: the decoder giving text-style spans, the span hull dropping character offsets, and the buffer dropping stored character offsets. Also, `Utf8LayerTests` now writes U+FEFF as `﻿`, where increment 2's file had the invisible character. Follow-ups added to the Scope of LAY-05 (`Parse` over bytes in memory and the text its result holds), STRM-02 (a byte root that runs dry must end the attempt Starved, since the decoder treats a cut-short sequence as invalid), STRM-03 (count lines and columns in the decoded text over a byte root) and ERR-07 (whether the decoder recovers with U+FFFD). Full suite: 1,683 passed, 0 warnings. The criterion appears met. Next: the maintainer opens the PR and runs `/complete`.
- 2026-11-24 (Build): `/complete`. Re-verified on ac365f6: build with 0 warnings, 1,683 passed, AOT smoke publish and run OK. Done.
- 2026-11-24 (Build): Rebased onto `feature/span-05` (SPAN-03 to SPAN-05) at the maintainer's request. Conflicts were only in the work README, this card and the docs index. `IUpstream<T>` gained `InputStart` and `InputEnd`, so the rebased "Add UTF8 layer" commit gives the decoder both: its input's byte offsets, at character 0 and at the number of characters decoded, as [ADR-0036](../../docs/decisions/0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md) point 4 needs. Each rebased commit builds with 0 warnings. Integration on top: the scope item [ADR-0034](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md) point 6 added is tested (`EveryDecodedCharacterCoversAtLeastOneByteSoNoneIsSynthetic`); trivia over bytes is tested by a CsCheck round trip of the bytes and of the decoded text, an explicit case and a byte-order mark alone; `LayerRun.Run` and the parser `LayerRun.Error` take bytes, and `ByteSourceDiagnosticsTests` uses the latter; docs say where a byte-order mark's trivia goes. Making `InputEnd` keep its input's character offsets fails 3 tests. Making `InputStart` do the same survives, because every root starts at byte 0, so the two agree; a root that starts later, such as SPAN-07's, would tell them apart. Re-verified: build with 0 warnings, 1,763 passed, AOT smoke publish and run OK. Still Done.

## Outcome

- **Dense four-offset spans**, in [src/Creole/StreamBuffer.cs](../../../src/Creole/StreamBuffer.cs):
  - When a segment stores its first span whose character offsets differ from its root offsets, it rents a `PackedSpan[]` of character offsets, counted from a character base. It marks the spans before it as equal to their root offsets.
  - A segment over text never rents one, so slots stay 32 bytes and text-rooted pipelines skip the character fields. A decoded item costs 8 more bytes.
  - The wide list now holds only spans out of range in either unit or from another source. `Release` returns the array to the pool.
- **`Layer.Utf8()`**, a `Layer<byte, char>`, in [src/Creole/Layers/Utf8Layer.cs](../../../src/Creole/Layers/Utf8Layer.cs) and [Layer.cs](../../../src/Creole/Layer.cs):
  - Each step decodes one scalar value with `Rune.DecodeFromUtf8`, reading one byte at a time, so it never reads past the byte that ends or breaks a sequence.
  - A leading byte-order mark is skipped and belongs to no item; elsewhere it decodes as U+FEFF.
  - Byte offsets and the source come from the input's spans. Character offsets count UTF-16 code units from 0 after any byte-order mark. A surrogate pair's halves share the step's whole span (§ Span rules, rule 2), with group indices 0 and 1.
  - Where its input starts and ends (`InputStart`, `InputEnd`) keep the input's byte offsets, at character 0 and at the number of characters decoded, so trivia over bytes has both units ([ADR-0036](../../docs/decisions/0036-trivia-is-the-source-between-items-read-with-a-zero-width-parser.md) point 4). A leading byte-order mark is in the first item's leading trivia: three bytes, zero-width in characters.
  - Every decoded character covers at least one byte, so no real input is synthetic ([ADR-0034](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md) point 6).
  - Invalid bytes end the parse with an error named `decoder`, such as `invalid UTF-8 bytes E2 82`. Its byte span covers the maximal subpart a replacing decoder turns into one U+FFFD, and its character span is zero-width where those bytes stand.
  - It releases its input as `Select` does.
- **Tests** (29 across four classes):
  - [SpanStorageTests.cs](../../../tests/Creole.Tests/SpanStorageTests.cs) (5): the model test with shifted character offsets; no allocation per item for text or decoded spans past 4 GB, and only decoded segments store character offsets; explicit spans that fit and do not fit in either unit.
  - [Utf8LayerTests.cs](../../../tests/Creole.Tests/Utf8LayerTests.cs) (20 cases): CsCheck properties for valid text against `Rune` and for random bytes against a strict `UTF8Encoding`, whose exception gives the first invalid sequence; that every decoded character covers at least one byte; that trivia and tokens over bytes cover the bytes and the decoded text exactly once; explicit spans, byte-order marks, empty input, eight invalid sequences, a lexer above the decoder, and trivia with both units.
  - [LayerRetentionTests.cs](../../../tests/Creole.Tests/LayerRetentionTests.cs): the decoder over released, overwritten segments of 1 and 2 bytes.
  - [ByteSourceDiagnosticsTests.cs](../../../tests/Creole.Tests/ByteSourceDiagnosticsTests.cs) (3): the criterion, end to end.
  - [LayerRun.cs](../../../tests/Creole.Tests/Utilities/LayerRun.cs) runs a layer, and a parser over its items, over any input type.
  - Mutation sweeps in each increment: each mutation failed at least one test.
- **Evidence, 2026-11-24, on b2e6be3 (rebased onto `feature/span-05`) with the integration change:** `dotnet build` with 0 warnings; `dotnet test` with 1,763 passed and 0 failed; the AOT smoke publish with no warnings, and its run printing OK. Before the rebase, on ac365f6: 1,683 passed.

  | Criterion | Evidence |
  | --- | --- |
  | 1. Diagnostics on a UTF-8 byte source report correct byte offsets and correct line and column | `ByteSourceDiagnosticsTests`: UTF-8 bytes with a byte-order mark, CRLF line ends and characters of two, three and four bytes run through `Layer.Utf8()`, a lexer and a parser. Each layer's error has the span and rendered position worked out by hand: decoder at bytes 19..20, characters 14..14, `2:4`; lexer at bytes 26..29, characters 20..21, `2:10`; parser at bytes 30..31, characters 24..25, `2:13`, after an emoji of two characters and one column. The decoder giving text-style spans, the span hull dropping character offsets, or the buffer dropping stored ones each fails them. |
- **Calls made in the build plan**, none of which changes the whitepaper's design: invalid bytes end the parse rather than becoming U+FFFD; a leading byte-order mark is skipped; character offsets count UTF-16 code units; `Layer.Utf8()` takes no `name` and decodes UTF-8 only.
- **Deviations:** none.
- **Docs:** [layers.md](../../../docs/concepts/layers.md) § Decoding bytes, [spans.md](../../../docs/concepts/spans.md) (decoded spans, trivia over bytes, and looking up line and column by `CharStart`), and the [docs index](../../../docs/README.md). XML docs cover `Layer.Utf8` and the storage rule on `StreamBuffer<T>`.
- **Follow-ups**, recorded in each card's Scope:
  - [LAY-05](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md): whether a pipeline from bytes gets `Parse` over bytes in memory, and which text its result holds for rendering. Until then no public API runs `Layer.Utf8()`, and the tests render against a `SourceText` they build from the decoded text.
  - [STRM-02](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md): a byte root that runs out of buffered data must end the attempt Starved, since the decoder treats a sequence cut short by the end of its input as invalid.
  - [STRM-03](../08-streaming/STRM-03-streaming-diagnostics.md): over a byte root, count lines and columns in the decoded text.
  - [ERR-07](../04-errors-and-recovery/ERR-07-lexer-recovery-and-cascade-suppression.md): whether the decoder recovers by replacing invalid bytes with U+FFFD.
