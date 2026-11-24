# SPAN-06: Byte sources and the decoding layer

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

Decoding is just another layer, and spans carry both byte and character offsets.

## Scope

- A `Layer<byte, char>` decoder, UTF-8 at minimum, with diagnostics for invalid sequences.
- Spans carry byte offsets and character offsets when a decoding layer exists; text-rooted pipelines skip the character fields.
- Every decoded character covers at least one byte, so real input never gets a zero-width span, which would make it synthetic ([ADR-0034](../../docs/decisions/0034-a-synthetic-item-is-one-whose-span-is-zero-width.md) point 6). Test it.
- Store the four offsets densely in the stream buffer. Until then, a span whose character offsets differ from its root offsets is kept whole beside its segment ([SPAN-02](SPAN-02-out-of-band-span-storage.md)).

## Acceptance criteria

- [ ] Diagnostics on a UTF-8 byte source report correct byte offsets and correct line and column.

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
