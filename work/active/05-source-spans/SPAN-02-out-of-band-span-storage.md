# SPAN-02: Out-of-band span storage

| | |
| --- | --- |
| Epic | [Source spans and provenance](../../README.md#span--source-spans-and-provenance) |
| Phase | Phase 1 — Layers |
| Status | Building |

## Summary

Spans travel beside items in the stream buffer, never inside user token types.

## Scope

- Store spans as 32-bit offsets relative to each segment's base; expose `long` publicly.
- A span that does not fit, because an offset is out of range, its source differs from its segment's or its character offsets differ from its root offsets, is kept whole beside its segment, so every span reads back exactly as added.
- No wrapper objects per item.
- Not here: the derived-item rule belongs to `Layer.Tokens` ([LAY-03](../06-layers-and-pipelines/LAY-03-layer-abstraction-and-emit.md)), the root-layer rule over streamed text to its sources ([LAY-05](../06-layers-and-pipelines/LAY-05-pipeline-api-and-sources.md)), and the dense four-offset form to byte roots ([SPAN-06](SPAN-06-byte-sources-and-the-decoding-layer.md)).

## Acceptance criteria

- [ ] A buffered item's span takes two 32-bit offsets (8 bytes) over a text root, and nothing is allocated per item.

## Blocked by

- [SPAN-01 SourceSpan, SourceId and LineMap](../05-source-spans/SPAN-01-sourcespan-sourceid-and-linemap.md)
- [LAY-01 StreamBuffer and pulling cursor](../06-layers-and-pipelines/LAY-01-streambuffer-and-pulling-cursor.md)

## Unblocks

- [SPAN-03 Synthetic items](../05-source-spans/SPAN-03-synthetic-items.md)
- [SPAN-04 Span combinators](../05-source-spans/SPAN-04-span-combinators.md)
- [SPAN-05 Trivia attach mode](../05-source-spans/SPAN-05-trivia-attach-mode.md)
- [SPAN-06 Byte sources and the decoding layer](../05-source-spans/SPAN-06-byte-sources-and-the-decoding-layer.md)
- [SPAN-07 Embedded languages](../05-source-spans/SPAN-07-embedded-languages.md)

## Whitepaper references

- [§ Span rules](../../../whitepaper.md#span-rules)
- [§ Source spans and provenance](../../../whitepaper.md#source-spans-and-provenance)

## Progress notes

- 2026-10-27 (Build): Started. The whitepaper fixes the format: per segment a `long` base, per item two 32-bit offsets (four over a byte root), widened to `SourceSpan` when read. Points for the `/build` plan: what happens when an offset does not fit in 32 bits; whether the root buffer stores spans at all, since rule 1 derives them from the position; whether the four-offset form is built here or in SPAN-06; where the derived-item rule lives before LAY-03 builds `Layer.Tokens`; and a number for criterion 2, since the whitepaper budgets only the span fields (slots are 64 bytes per item today). Next: `/build`.
- 2026-10-27 (Build): Increment 1 done. Agreed with the maintainer: criterion 1 and the derived-item rule moved to LAY-03, the root-layer rule over streamed text to LAY-05, and the dense four-offset form to SPAN-06; criterion 2 reworded with a number. `StreamBuffer<T>` now stores each span as a `PackedSpan` (a 32-bit offset from its segment's base, the start of the segment's first span, and a 32-bit length) inside a 32-byte `PackedSlot`, down from 64. A span whose offset or length does not fit, whose source differs from its segment's, or whose character offsets differ from its root offsets is kept whole in a list beside its segment, so every span reads back exactly. `Add`, `SlotAt` and `SpanOf` keep their signatures. `SpanStorageTests` (4): the slot and span sizes; a CsCheck model test of random slots across segment sizes 1–5, before and after a release; spans that do not fit; no allocation per item over offsets past 4 GB. A mutation sweep (each packing condition dropped, always wide, a 64-bit length) fails at least one test; sending negative offsets to the wide list survives, which is correct and only costs memory. No `docs/` change, since nothing public changed. Full suite: 1,552 passed; AOT smoke publish and run OK. The speed gate was not re-run: its benchmarks parse in-memory input and never touch the buffer. The criterion appears met. Next: the maintainer opens the PR and runs `/complete`.
