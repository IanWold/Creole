# LAY-10: Measure and reduce what a buffered item costs

| | |
| --- | --- |
| Epic | [Layers, buffers and pipelines](../../README.md#lay--layers-buffers-and-pipelines) |
| Phase | Phase 3 — Staging |
| Status | Not started |

## Summary

Every buffered item carries a 32-byte slot, so holding a record whole costs tens of bytes per input byte. Measure where the bytes go at each boundary, and compute what can be computed instead of storing it.

## Scope

- **What was measured.** [STRM-02](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md) parsed a 4 MiB JSON record with `ParseManyAsync`, using the `json-lines-async-large` input of the memory harness. The heap grew by 117 to 121 MB. That is over 28 bytes per input byte, and it is measured above a baseline that already holds the warm-up's pooled segments (STRM-02's progress note of 2026-11-27, increment 4).
  - A held record is kept whole in every layer: the root's bytes, the decoded characters and the tokens.
  - Each item has a 32-byte `PackedSlot` beside it ([SPAN-02](../05-source-spans/SPAN-02-out-of-band-span-storage.md)'s Outcome). It holds a packed span (8 bytes), `stepStart` (8), `stateBefore` (8) and `groupIndex` (4).
  - A segment whose character offsets differ from its byte offsets adds 8 bytes per item.
- **Measure first.** Find the bytes retained per item at each boundary, over `json-lines-async-large` and Example B. Use a run that does not hide the warm-up's pooled segments, such as a cold run or one that counts what the pools hold.
- **Candidates, each to be measured:**
  - A root over bytes or characters needs no stored slot. Item *i* has the span \[*i*, *i* + 1), its chunk's start as its step start, no state, and its index in the chunk as its group index.
  - A stateless layer stores `stateBefore` for every item, though it is always null.
  - `groupIndex` is 0 for every item of a step that emits one, and `Layer.Utf8`'s step start equals its span's start.
  - Each field could be kept in its own array per segment, and an array kept only for a layer that needs it.
- **The step fields must stay exact.** Mode invalidation, cascades, restarts and the failure log's copies all read them, so any field left unstored must be computed exactly.
- **A decision record chooses the layout.** [§ The stream buffer](../../../whitepaper.md#the-stream-buffer) says each slot stores five fields, so a layout that computes some of them changes the design it describes.

## Acceptance criteria

- [ ] The bytes retained per item at each boundary are measured over `json-lines-async-large` and Example B, before and after the change, and recorded in the card.
- [ ] A decision record chooses the slot layout with those measurements as its evidence, and § The stream buffer matches it once the record is accepted.
- [ ] All tests pass, including `SpanStorageTests`, `RestartTests` and the mode invalidation tests. The Phase 0 speed gate and the Phase 1 memory gate still pass.

## Blocked by

- [STRM-02 Restart protocol and ParseManyAsync](../08-streaming/STRM-02-restart-protocol-and-parsemanyasync.md)

## Unblocks

- [REL-05 Ship 1.0](../14-release/REL-05-ship-10.md)

## Whitepaper references

- [§ The stream buffer](../../../whitepaper.md#the-stream-buffer)
- [§ Guarantees](../../../whitepaper.md#guarantees)
- [§ Source spans and provenance](../../../whitepaper.md#source-spans-and-provenance)
